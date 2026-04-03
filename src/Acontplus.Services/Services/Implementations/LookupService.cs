using System.Globalization;
using Acontplus.Core.Validation;

namespace Acontplus.Services.Services.Implementations;

/// <summary>
/// Service for managing and caching lookup data from database queries.
/// Works with both ADO.NET and Entity Framework Core through IUnitOfWork abstraction.
/// </summary>
public class LookupService : ILookupService
{
  private const string CacheKeyPrefix = "lookups";
  private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(30);

  private readonly IUnitOfWork _unitOfWork;
  private readonly ICacheService _cacheService;
  private readonly ILogger<LookupService> _logger;

  public LookupService(
      IUnitOfWork unitOfWork,
      ICacheService cacheService,
      ILogger<LookupService> logger)
  {
    _unitOfWork = unitOfWork;
    _cacheService = cacheService;
    _logger = logger;
  }

  public async Task<Result<IDictionary<string, IEnumerable<LookupItem>>, DomainError>> GetLookupsAsync(
      string storedProcedureName,
      FilterRequest filterRequest,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var cacheKey = BuildCacheKey(storedProcedureName, filterRequest);

      return await _cacheService.GetOrCreateAsync(
          cacheKey,
          async () => await FetchLookupsAsync(storedProcedureName, filterRequest, cancellationToken),
          DefaultCacheDuration,
          cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving lookups for {StoredProcedure}", storedProcedureName);
      var error = DomainError.Internal(
          "LOOKUPS_GET_ERROR",
          $"An error occurred while retrieving lookups: {ex.Message}");
      return Result<IDictionary<string, IEnumerable<LookupItem>>, DomainError>.Failure(error);
    }
  }

  public async Task<Result<IDictionary<string, IEnumerable<LookupItem>>, DomainError>> RefreshLookupsAsync(
      string storedProcedureName,
      FilterRequest filterRequest,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var cacheKey = BuildCacheKey(storedProcedureName, filterRequest);
      await _cacheService.RemoveAsync(cacheKey, cancellationToken);

      _logger.LogInformation("Cache removed for key: {CacheKey}", cacheKey);

      return await GetLookupsAsync(storedProcedureName, filterRequest, cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error refreshing lookups for {StoredProcedure}", storedProcedureName);
      var error = DomainError.Internal(
          "LOOKUPS_REFRESH_ERROR",
          $"An error occurred while refreshing lookups: {ex.Message}");
      return Result<IDictionary<string, IEnumerable<LookupItem>>, DomainError>.Failure(error);
    }
  }

  private async Task<Result<IDictionary<string, IEnumerable<LookupItem>>, DomainError>> FetchLookupsAsync(
      string storedProcedureName,
      FilterRequest filterRequest,
      CancellationToken cancellationToken)
  {
    var dataSet = await _unitOfWork.AdoRepository.GetFilteredDataSetAsync(
        storedProcedureName,
        filterRequest,
        options: new CommandOptionsDto { WithTableNames = false },
        cancellationToken: cancellationToken);

    if (DataValidation.DataSetIsNull(dataSet, removeEmptyDt: true))
    {
      return Result<IDictionary<string, IEnumerable<LookupItem>>, DomainError>.Failure(
          DomainError.NotFound("LOOKUPS_EMPTY", "The lookup query returned no data sets."));
    }

    var resultPayload = dataSet.Tables
        .Cast<DataTable>()
        .Where(dt => dt.Rows.Count > 0)
        .SelectMany(dt => dt.AsEnumerable())
        .GroupBy(row => row.Field<string>("TableName"))
        .ToDictionary(
            group => JsonNamingPolicy.CamelCase.ConvertName(string.IsNullOrWhiteSpace(group.Key) ? "default" : group.Key),
            group => (IEnumerable<LookupItem>)group.Select(row => new LookupItem(
                row.Field<int?>("Id"),
                row.Field<string?>("Code"),
                row.Field<string?>("Value"),
                row.Field<int?>("DisplayOrder"),
                row.Field<int?>("ParentId"),
                row.Field<bool?>("IsDefault"),
                row.Field<bool?>("IsActive"),
                row.Field<string?>("Description"),
                row.Field<string?>("Metadata")
            )).ToArray(),
            StringComparer.OrdinalIgnoreCase);

    _logger.LogDebug("Fetched {Count} lookup groups from {StoredProcedure}",
        resultPayload.Count, storedProcedureName);

    return Result<IDictionary<string, IEnumerable<LookupItem>>, DomainError>.Success(resultPayload);
  }

  private static string BuildCacheKey(string storedProcedureName, FilterRequest filterRequest)
  {
    var module = NormalizeSegment(ExtractFilterValue(filterRequest, "module"));
    var context = NormalizeSegment(ExtractFilterValue(filterRequest, "context"));
    var userRoleId = NormalizeSegment(ExtractFilterValue(filterRequest, "userRoleId"));
    var userId = NormalizeSegment(ExtractFilterValue(filterRequest, "userId"));
    var companyId = NormalizeSegment(ExtractFilterValue(filterRequest, "companyId"));
    var spName = NormalizeSegment(storedProcedureName);

    return $"{CacheKeyPrefix}:{spName}:{module}:{context}:{userRoleId}:{userId}:{companyId}";
  }

  private static string NormalizeSegment(string? value)
  {
    return string.IsNullOrWhiteSpace(value)
        ? "default"
        : value.Trim().ToLowerInvariant();
  }

  private static string ExtractFilterValue(FilterRequest filterRequest, string propertyName)
  {
    if (filterRequest.Filters == null || filterRequest.Filters.Count == 0)
    {
      return string.Empty;
    }

    if (!filterRequest.Filters.TryGetValue(propertyName, out var rawValue) || rawValue is null)
    {
      return string.Empty;
    }

    return rawValue switch
    {
      string str => str,
      IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
      JsonElement jsonElement => jsonElement.ValueKind switch
      {
        JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
        JsonValueKind.Number => jsonElement.GetRawText(),
        JsonValueKind.True => bool.TrueString,
        JsonValueKind.False => bool.FalseString,
        _ => jsonElement.ToString()
      },
      _ => rawValue.ToString() ?? string.Empty
    };
  }
}

