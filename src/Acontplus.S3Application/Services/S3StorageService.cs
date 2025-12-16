using Acontplus.S3Application.Configuration;
using Acontplus.S3Application.Interfaces;
using Acontplus.S3Application.Models;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Collections.Concurrent;

namespace Acontplus.S3Application.Services;

/// <summary>
/// Provides a scalable, resilient implementation of <see cref="IS3StorageService"/> for AWS S3 storage operations.
/// Features: connection pooling, retry policies with exponential backoff, rate limiting, and proper resource management.
/// </summary>
public class S3StorageService : IS3StorageService, IDisposable
{
    private readonly ILogger<S3StorageService> _logger;
    private readonly S3StorageOptions _options;
    private readonly ConcurrentDictionary<string, IAmazonS3> _clientPool;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private readonly ConcurrentQueue<DateTime> _requestTimestamps;
    private readonly TimeSpan _rateLimitWindow;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="S3StorageService"/> with configuration and logging.
    /// </summary>
    /// <param name="options">S3 storage configuration options.</param>
    /// <param name="logger">Logger instance for diagnostics.</param>
    public S3StorageService(IOptions<S3StorageOptions> options, ILogger<S3StorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new S3StorageOptions();

        _clientPool = new ConcurrentDictionary<string, IAmazonS3>();
        _requestTimestamps = new ConcurrentQueue<DateTime>();
        _rateLimitWindow = TimeSpan.FromSeconds(1);
        _rateLimitSemaphore = new SemaphoreSlim(_options.MaxRequestsPerSecond, _options.MaxRequestsPerSecond);

        _retryPolicy = CreateRetryPolicy();

        _logger.LogInformation("S3StorageService initialized with MaxRequestsPerSecond: {MaxRequestsPerSecond}, Timeout: {Timeout}s",
            _options.MaxRequestsPerSecond, _options.TimeoutSeconds);
    }

    /// <summary>
    /// Gets or creates a pooled S3 client for the specified credentials and region.
    /// </summary>
    private IAmazonS3 GetOrCreateClient(AwsCredentials? credentials, string region)
    {
        var clientKey = $"{credentials?.Key}:{region}";

        return _clientPool.GetOrAdd(clientKey, key =>
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region),
                MaxErrorRetry = 0, // We handle retries via Polly
                Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
                UseHttp = false,
                ForcePathStyle = _options.ForcePathStyle
            };

            IAmazonS3 client = credentials != null
                ? new AmazonS3Client(new BasicAWSCredentials(credentials.Key, credentials.Secret), config)
                : new AmazonS3Client(config); // Uses default credential chain (IAM roles, etc.)

            _logger.LogDebug("Created new S3 client for region: {Region}", region);
            return client;
        });
    }

    /// <summary>
    /// Creates retry policy for transient S3 errors with exponential backoff.
    /// </summary>
    private AsyncRetryPolicy CreateRetryPolicy()
    {
        var delays = new List<TimeSpan>();
        for (int i = 0; i < _options.MaxRetries; i++)
        {
            delays.Add(TimeSpan.FromMilliseconds(_options.RetryBaseDelayMs * Math.Pow(2, i)));
        }

        return Policy
            .Handle<AmazonS3Exception>(ex =>
                ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                ex.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
                ex.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                ex.ErrorCode == "RequestTimeout" ||
                ex.ErrorCode == "SlowDown" ||
                ex.ErrorCode == "ServiceUnavailable")
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .WaitAndRetryAsync(
                sleepDurations: delays,
                onRetryAsync: (exception, delay, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "S3 operation retry {RetryCount}/{MaxRetries} after {Delay}. Error: {ErrorType}",
                        retryCount, _options.MaxRetries, delay, exception.GetType().Name);
                    return Task.CompletedTask;
                });
    }

    /// <summary>
    /// Enforces rate limiting to prevent AWS throttling.
    /// </summary>
    private async Task EnforceRateLimitAsync(CancellationToken ct = default)
    {
        await _rateLimitSemaphore.WaitAsync(ct);

        try
        {
            CleanupOldTimestamps();

            if (_requestTimestamps.Count >= _options.MaxRequestsPerSecond)
            {
                if (_requestTimestamps.TryPeek(out var oldestTimestamp))
                {
                    var timeToWait = oldestTimestamp + _rateLimitWindow - DateTime.UtcNow;
                    if (timeToWait > TimeSpan.Zero)
                    {
                        _logger.LogDebug("Rate limit reached ({CurrentCount}/{MaxRate}), waiting {WaitTime}ms",
                            _requestTimestamps.Count, _options.MaxRequestsPerSecond, timeToWait.TotalMilliseconds);

                        await Task.Delay(timeToWait, ct);
                        CleanupOldTimestamps();
                    }
                }
            }

            _requestTimestamps.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// Removes timestamps older than the rate limit window.
    /// </summary>
    private void CleanupOldTimestamps()
    {
        var cutoff = DateTime.UtcNow - _rateLimitWindow;
        while (_requestTimestamps.TryPeek(out var timestamp) && timestamp < cutoff)
        {
            _requestTimestamps.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Uploads a new object to S3 asynchronously with retry policy and rate limiting.
    /// </summary>
    /// <param name="s3ObjectCustom">The S3 object to upload.</param>
    /// <returns>A response with status and metadata.</returns>
    public async Task<S3Response> UploadAsync(S3ObjectCustom s3ObjectCustom)
    {
        ArgumentNullException.ThrowIfNull(s3ObjectCustom);

        var response = new S3Response();

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await EnforceRateLimitAsync();

                var client = GetOrCreateClient(s3ObjectCustom.AwsCredentials, s3ObjectCustom.Region ?? _options.Region ?? "us-east-1");

                if (s3ObjectCustom.Content == null)
                    throw new InvalidOperationException("S3 object content cannot be null for upload");

                using var ms = new MemoryStream(s3ObjectCustom.Content);
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = ms,
                    Key = s3ObjectCustom.S3ObjectKey,
                    BucketName = s3ObjectCustom.BucketName,
                    CannedACL = S3CannedACL.NoACL,
                    ContentType = s3ObjectCustom.ContentType
                };

                using var transferUtility = new TransferUtility(client);
                await transferUtility.UploadAsync(uploadRequest);

                _logger.LogInformation("Successfully uploaded {Key} to bucket {Bucket}",
                    s3ObjectCustom.S3ObjectKey, s3ObjectCustom.BucketName);

                return new S3Response
                {
                    StatusCode = 201,
                    Message = $"El archivo {s3ObjectCustom.S3ObjectKey} se subió correctamente en Amazon S3"
                };
            });
        }
        catch (AmazonS3Exception s3Ex)
        {
            _logger.LogError(s3Ex, "S3 error uploading {Key}: {ErrorCode} - {Message}",
                s3ObjectCustom.S3ObjectKey, s3Ex.ErrorCode, s3Ex.Message);

            response.StatusCode = (int)s3Ex.StatusCode;
            response.Message = s3Ex.Message;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading {Key}",
                s3ObjectCustom.S3ObjectKey);

            response.StatusCode = 500;
            response.Message = ex.Message;
            return response;
        }
    }

    /// <summary>
    /// Updates an existing object in S3 asynchronously with retry policy and rate limiting.
    /// </summary>
    /// <param name="s3ObjectCustom">The S3 object to update.</param>
    /// <returns>A response with status and metadata.</returns>
    public async Task<S3Response> UpdateAsync(S3ObjectCustom s3ObjectCustom)
    {
        ArgumentNullException.ThrowIfNull(s3ObjectCustom);

        var response = new S3Response();

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await EnforceRateLimitAsync();

                var client = GetOrCreateClient(s3ObjectCustom.AwsCredentials, s3ObjectCustom.Region ?? _options.Region ?? "us-east-1");

                if (s3ObjectCustom.Content == null)
                    throw new InvalidOperationException("S3 object content cannot be null for update");

                using var ms = new MemoryStream(s3ObjectCustom.Content);
                var request = new PutObjectRequest
                {
                    BucketName = s3ObjectCustom.BucketName,
                    Key = s3ObjectCustom.S3ObjectKey,
                    InputStream = ms,
                    CannedACL = S3CannedACL.NoACL,
                    ContentType = s3ObjectCustom.ContentType
                };

                await client.PutObjectAsync(request);

                _logger.LogInformation("Successfully updated {Key} in bucket {Bucket}",
                    s3ObjectCustom.S3ObjectKey, s3ObjectCustom.BucketName);

                return new S3Response
                {
                    StatusCode = 200,
                    Message = $"El archivo {s3ObjectCustom.S3ObjectKey} se actualizó correctamente en Amazon S3"
                };
            });
        }
        catch (AmazonS3Exception s3Ex)
        {
            _logger.LogError(s3Ex, "S3 error updating {Key}: {ErrorCode} - {Message}",
                s3ObjectCustom.S3ObjectKey, s3Ex.ErrorCode, s3Ex.Message);

            response.StatusCode = (int)s3Ex.StatusCode;
            response.Message = s3Ex.Message;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating {Key}",
                s3ObjectCustom.S3ObjectKey);

            response.StatusCode = 500;
            response.Message = ex.Message;
            return response;
        }
    }

    /// <summary>
    /// Deletes an object from S3 asynchronously with retry policy and rate limiting.
    /// </summary>
    /// <param name="s3ObjectCustom">The S3 object to delete.</param>
    /// <returns>A response with status and metadata.</returns>
    public async Task<S3Response> DeleteAsync(S3ObjectCustom s3ObjectCustom)
    {
        ArgumentNullException.ThrowIfNull(s3ObjectCustom);

        var response = new S3Response();

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await EnforceRateLimitAsync();

                var client = GetOrCreateClient(s3ObjectCustom.AwsCredentials, s3ObjectCustom.Region ?? _options.Region ?? "us-east-1");

                var request = new DeleteObjectRequest
                {
                    BucketName = s3ObjectCustom.BucketName,
                    Key = s3ObjectCustom.S3ObjectKey
                };

                await client.DeleteObjectAsync(request);

                _logger.LogInformation("Successfully deleted {Key} from bucket {Bucket}",
                    s3ObjectCustom.S3ObjectKey, s3ObjectCustom.BucketName);

                return new S3Response
                {
                    StatusCode = 200,
                    Message = $"El archivo {s3ObjectCustom.S3ObjectKey} se eliminó correctamente de Amazon S3"
                };
            });
        }
        catch (AmazonS3Exception s3Ex)
        {
            _logger.LogError(s3Ex, "S3 error deleting {Key}: {ErrorCode} - {Message}",
                s3ObjectCustom.S3ObjectKey, s3Ex.ErrorCode, s3Ex.Message);

            response.StatusCode = (int)s3Ex.StatusCode;
            response.Message = s3Ex.Message;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting {Key}",
                s3ObjectCustom.S3ObjectKey);

            response.StatusCode = 500;
            response.Message = ex.Message;
            return response;
        }
    }

    /// <summary>
    /// Retrieves an object from S3 asynchronously with retry policy and rate limiting.
    /// </summary>
    /// <param name="s3ObjectCustom">The S3 object to retrieve.</param>
    /// <returns>A response with file content and metadata.</returns>
    public async Task<S3Response> GetObjectAsync(S3ObjectCustom s3ObjectCustom)
    {
        ArgumentNullException.ThrowIfNull(s3ObjectCustom);

        var response = new S3Response();

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await EnforceRateLimitAsync();

                var client = GetOrCreateClient(s3ObjectCustom.AwsCredentials, s3ObjectCustom.Region ?? _options.Region ?? "us-east-1");

                var request = new GetObjectRequest
                {
                    BucketName = s3ObjectCustom.BucketName,
                    Key = s3ObjectCustom.S3ObjectKey
                };

                using var s3Response = await client.GetObjectAsync(request);
                using var memoryStream = new MemoryStream();
                await s3Response.ResponseStream.CopyToAsync(memoryStream);

                _logger.LogInformation("Successfully retrieved {Key} from bucket {Bucket} ({Size} bytes)",
                    s3ObjectCustom.S3ObjectKey, s3ObjectCustom.BucketName, memoryStream.Length);

                return new S3Response
                {
                    StatusCode = 200,
                    Message = $"El archivo {s3ObjectCustom.S3ObjectKey} se obtuvo correctamente de Amazon S3",
                    Content = memoryStream.ToArray(),
                    ContentType = s3Response.Headers.ContentType,
                    FileName = Path.GetFileName(s3ObjectCustom.S3ObjectKey)
                };
            });
        }
        catch (AmazonS3Exception s3Ex)
        {
            _logger.LogError(s3Ex, "S3 error retrieving {Key}: {ErrorCode} - {Message}",
                s3ObjectCustom.S3ObjectKey, s3Ex.ErrorCode, s3Ex.Message);

            response.StatusCode = (int)s3Ex.StatusCode;
            response.Message = s3Ex.Message;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving {Key}",
                s3ObjectCustom.S3ObjectKey);

            response.StatusCode = 500;
            response.Message = ex.Message;
            return response;
        }
    }

    /// <summary>
    /// Checks if an object exists in S3 asynchronously with retry policy and rate limiting.
    /// </summary>
    /// <param name="s3ObjectCustom">The S3 object to check.</param>
    /// <returns>True if the object exists; otherwise, false.</returns>
    public async Task<bool> DoesObjectExistAsync(S3ObjectCustom s3ObjectCustom)
    {
        ArgumentNullException.ThrowIfNull(s3ObjectCustom);

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await EnforceRateLimitAsync();

                var client = GetOrCreateClient(s3ObjectCustom.AwsCredentials, s3ObjectCustom.Region ?? _options.Region ?? "us-east-1");

                var request = new GetObjectMetadataRequest
                {
                    BucketName = s3ObjectCustom.BucketName,
                    Key = s3ObjectCustom.S3ObjectKey
                };

                await client.GetObjectMetadataAsync(request);

                _logger.LogDebug("Object exists: {Key} in bucket {Bucket}",
                    s3ObjectCustom.S3ObjectKey, s3ObjectCustom.BucketName);

                return true;
            });
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Object not found: {Key} in bucket {Bucket}",
                s3ObjectCustom.S3ObjectKey, s3ObjectCustom.BucketName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking existence of {Key}",
                s3ObjectCustom.S3ObjectKey);
            return false;
        }
    }

    /// <summary>
    /// Generates a presigned URL for an S3 object asynchronously with retry policy and rate limiting.
    /// </summary>
    /// <param name="s3ObjectCustom">The S3 object for which to generate the URL.</param>
    /// <param name="expirationInMinutes">The expiration time in minutes for the URL.</param>
    /// <returns>A response containing the presigned URL.</returns>
    public async Task<S3Response> GetPresignedUrlAsync(S3ObjectCustom s3ObjectCustom, int expirationInMinutes = 60)
    {
        ArgumentNullException.ThrowIfNull(s3ObjectCustom);

        var response = new S3Response();

        try
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                await EnforceRateLimitAsync();

                var client = GetOrCreateClient(s3ObjectCustom.AwsCredentials, s3ObjectCustom.Region ?? _options.Region ?? "us-east-1");

                var request = new GetPreSignedUrlRequest
                {
                    BucketName = s3ObjectCustom.BucketName,
                    Key = s3ObjectCustom.S3ObjectKey,
                    Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes)
                };

                var presignedUrl = await Task.Run(() => client.GetPreSignedURL(request));

                _logger.LogInformation("Generated presigned URL for {Key} valid for {Minutes} minutes",
                    s3ObjectCustom.S3ObjectKey, expirationInMinutes);

                return new S3Response
                {
                    StatusCode = 200,
                    Message = $"URL prefirmada generada correctamente para {s3ObjectCustom.S3ObjectKey}",
                    FileName = presignedUrl
                };
            });
        }
        catch (AmazonS3Exception s3Ex)
        {
            _logger.LogError(s3Ex, "S3 error generating presigned URL for {Key}: {ErrorCode} - {Message}",
                s3ObjectCustom.S3ObjectKey, s3Ex.ErrorCode, s3Ex.Message);

            response.StatusCode = (int)s3Ex.StatusCode;
            response.Message = s3Ex.Message;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating presigned URL for {Key}",
                s3ObjectCustom.S3ObjectKey);

            response.StatusCode = 500;
            response.Message = ex.Message;
            return response;
        }
    }

    /// <summary>
    /// Disposes all pooled S3 clients and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing S3StorageService and {ClientCount} pooled clients", _clientPool.Count);

        foreach (var client in _clientPool.Values)
        {
            try
            {
                client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing S3 client");
            }
        }

        _clientPool.Clear();
        _rateLimitSemaphore?.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
