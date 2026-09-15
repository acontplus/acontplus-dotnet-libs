using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Resilience;
using Microsoft.Extensions.Logging;
using Moq;

namespace Acontplus.Persistence.Common.Tests.Unit.Resilience;

public class PersistenceResilienceHelperTests
{
    private readonly Mock<ILogger> _loggerMock = new();

    [Fact]
    public void CreateRetryPolicy_WhenNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PersistenceResilienceHelper.CreateRetryPolicy(null!, _ => true, _loggerMock.Object, "TestProvider"));
    }

    [Fact]
    public void CreateRetryPolicy_WhenNullTransientPredicate_ThrowsArgumentNullException()
    {
        var options = new PersistenceResilienceOptions();
        Assert.Throws<ArgumentNullException>(() =>
            PersistenceResilienceHelper.CreateRetryPolicy(options, null!, _loggerMock.Object, "TestProvider"));
    }

    [Fact]
    public void CreateRetryPolicy_WhenNullLogger_ThrowsArgumentNullException()
    {
        var options = new PersistenceResilienceOptions();
        Assert.Throws<ArgumentNullException>(() =>
            PersistenceResilienceHelper.CreateRetryPolicy(options, _ => true, null!, "TestProvider"));
    }

    [Fact]
    public async Task CreateRetryPolicy_WhenRetryDisabled_DoesNotRetry()
    {
        var options = new PersistenceResilienceOptions
        {
            RetryPolicy = new PersistenceResilienceOptions.RetryPolicyOptions
            {
                Enabled = false
            }
        };

        var policy = PersistenceResilienceHelper.CreateRetryPolicy(
            options,
            _ => true,
            _loggerMock.Object,
            "TestProvider");

        var executionCount = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                executionCount++;
                await Task.Yield();
                throw new InvalidOperationException("Failed");
            });
        });

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CreateRetryPolicy_WhenTransientExceptionOccurs_RetriesConfiguredTimes()
    {
        var options = new PersistenceResilienceOptions
        {
            RetryPolicy = new PersistenceResilienceOptions.RetryPolicyOptions
            {
                Enabled = true,
                MaxRetries = 2,
                BaseDelaySeconds = 0,
                ExponentialBackoff = false
            }
        };

        var policy = PersistenceResilienceHelper.CreateRetryPolicy(
            options,
            ex => ex is TimeoutException,
            _loggerMock.Object,
            "TestProvider");

        var executionCount = 0;
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                executionCount++;
                await Task.Yield();
                throw new TimeoutException("Database timeout");
            });
        });

        Assert.Equal(3, executionCount); // Initial attempt + 2 retries
    }
}
