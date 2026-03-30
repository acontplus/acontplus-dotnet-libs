# Persistence Layer Resilience Configuration Guide

## Overview

The ADO.NET repositories in `Acontplus.Persistence.SqlServer` and `Acontplus.Persistence.PostgreSQL` now support **dynamic resilience configuration** via `appsettings.json`, allowing you to adjust retry policies, circuit breakers, and timeouts without code changes.

## 🎯 Key Features

- ✅ **Dynamic Configuration**: Configure retry policies via `appsettings.json`
- ✅ **Sensible Defaults**: Works out-of-the-box without configuration
- ✅ **Multi-Layered Resilience**: Complements connection string retry settings
- ✅ **Environment-Specific**: Different settings for Development, Staging, Production
- ✅ **Backward Compatible**: No breaking changes to existing code

## 📋 Configuration Structure

### Configuration Section

Add this to your `appsettings.json`:

```json
{
  "Persistence": {
    "Resilience": {
      "RetryPolicy": {
        "Enabled": true,
        "MaxRetries": 3,
        "BaseDelaySeconds": 2,
        "ExponentialBackoff": true,
        "MaxDelaySeconds": 30
      },
      "CircuitBreaker": {
        "Enabled": true,
        "ExceptionsAllowedBeforeBreaking": 5,
        "DurationOfBreakSeconds": 30,
        "SamplingDurationSeconds": 60,
        "MinimumThroughput": 10
      },
      "Timeout": {
        "Enabled": true,
        "DefaultCommandTimeoutSeconds": 30,
        "ComplexQueryTimeoutSeconds": 60,
        "BulkOperationTimeoutSeconds": 300,
        "LongRunningQueryTimeoutSeconds": 600
      }
    }
  }
}
```

## 🔧 Configuration Options

### RetryPolicy Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable retry policy |
| `MaxRetries` | int | `3` | Maximum retry attempts |
| `BaseDelaySeconds` | int | `2` | Base delay for exponential backoff (2s, 4s, 8s) |
| `ExponentialBackoff` | bool | `true` | Use exponential backoff vs fixed delay |
| `MaxDelaySeconds` | int | `30` | Maximum delay cap for exponential backoff |

### CircuitBreaker Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable circuit breaker |
| `ExceptionsAllowedBeforeBreaking` | int | `5` | Failures before circuit opens |
| `DurationOfBreakSeconds` | int | `30` | How long circuit stays open |
| `SamplingDurationSeconds` | int | `60` | Time window for failure tracking |
| `MinimumThroughput` | int | `10` | Minimum requests before circuit can open |

### Timeout Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable timeout policies |
| `DefaultCommandTimeoutSeconds` | int | `30` | Default timeout for queries |
| `ComplexQueryTimeoutSeconds` | int | `60` | Timeout for complex queries |
| `BulkOperationTimeoutSeconds` | int | `300` | Timeout for bulk inserts (5 min) |
| `LongRunningQueryTimeoutSeconds` | int | `600` | Timeout for reports (10 min) |

## 🏗️ Multi-Layered Resilience Strategy

### Layer 1: Connection String Retry (Network Level)
```
"ConnectRetryCount=3;ConnectRetryInterval=10"
```
- ✅ Handles **connection establishment failures**
- ✅ Fast (10s intervals)
- ❌ Does NOT retry query execution failures

### Layer 2: Polly Retry (Application Level)
```json
{
  "RetryPolicy": {
    "MaxRetries": 3,
    "BaseDelaySeconds": 2,
    "ExponentialBackoff": true
  }
}
```
- ✅ Handles **both connection AND query failures**
- ✅ Smart (exponential backoff: 2s, 4s, 8s)
- ✅ Retry deadlocks, timeouts, transient errors

### Layer 3: Circuit Breaker (Protection Layer)
```json
{
  "CircuitBreaker": {
    "ExceptionsAllowedBeforeBreaking": 5,
    "DurationOfBreakSeconds": 30
  }
}
```
- ✅ Prevents **cascading failures**
- ✅ Protects database from overload
- ✅ Automatic recovery testing

### Combined Benefit

**Without Polly**: Connection retry only (30s max) → **FAILURE**
```
[10:00:00] Query starts
[10:00:10] Connection retry 1
[10:00:20] Connection retry 2
[10:00:30] Connection retry 3
[10:00:30] FAILED - Deadlock not retried
```

**With Polly**: Connection + Polly retry (16s) → **SUCCESS**
```
[10:00:00] Query starts
[10:00:03] Deadlock detected
[10:00:05] Polly retry 1 (after 2s)
[10:00:06] SUCCEEDED
```

## 📊 Environment-Specific Examples

### Development (Lenient)
```json
{
  "Persistence": {
    "Resilience": {
      "RetryPolicy": {
        "Enabled": true,
        "MaxRetries": 2,
        "BaseDelaySeconds": 1,
        "ExponentialBackoff": false
      },
      "CircuitBreaker": {
        "Enabled": false
      }
    }
  }
}
```

### Production (Strict)
```json
{
  "Persistence": {
    "Resilience": {
      "RetryPolicy": {
        "Enabled": true,
        "MaxRetries": 3,
        "BaseDelaySeconds": 2,
        "ExponentialBackoff": true,
        "MaxDelaySeconds": 30
      },
      "CircuitBreaker": {
        "Enabled": true,
        "ExceptionsAllowedBeforeBreaking": 5,
        "DurationOfBreakSeconds": 30
      }
    }
  }
}
```

### High-Volume Production
```json
{
  "Persistence": {
    "Resilience": {
      "RetryPolicy": {
        "Enabled": true,
        "MaxRetries": 5,
        "BaseDelaySeconds": 2,
        "ExponentialBackoff": true,
        "MaxDelaySeconds": 60
      },
      "CircuitBreaker": {
        "Enabled": true,
        "ExceptionsAllowedBeforeBreaking": 10,
        "DurationOfBreakSeconds": 60,
        "MinimumThroughput": 100
      }
    }
  }
}
```

## 🚀 Registration

### Option 1: Automatic (Recommended)
If using `AddInfrastructureServices()`, resilience is registered automatically:

```csharp
// Program.cs
builder.Services.AddInfrastructureServices(builder.Configuration);
```

### Option 2: Manual
Register explicitly for more control:

```csharp
// Program.cs
builder.Services.Configure<PersistenceResilienceOptions>(
    builder.Configuration.GetSection(PersistenceResilienceOptions.SectionName));

builder.Services.AddScoped<IAdoRepository, AdoRepository>();
```

## 📈 Performance Impact

### Successful Queries (No Retry)
- **Overhead**: <1μs (negligible)
- **Impact**: None

### Transient Failures (With Retry)
- **Without Polly**: 30s → FAILURE
- **With Polly**: 6s → SUCCESS
- **Benefit**: 80% faster + recovery

## 🔍 Monitoring & Logging

Retry attempts are automatically logged:

```
[10:00:03] [ADO Repository] Retry 1/3 after 2000ms for SQL Server operation
System.Data.SqlClient.SqlException: Deadlock victim
```

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Acontplus.Persistence.SqlServer": "Debug",
      "Acontplus.Persistence.PostgreSQL": "Debug"
    }
  }
}
```

## ⚙️ Advanced Scenarios

### Disable Retry for Specific Environment
```json
{
  "Persistence": {
    "Resilience": {
      "RetryPolicy": {
        "Enabled": false
      }
    }
  }
}
```

### Custom Retry for Bulk Operations
```json
{
  "Persistence": {
    "Resilience": {
      "RetryPolicy": {
        "MaxRetries": 5,
        "BaseDelaySeconds": 5,
        "MaxDelaySeconds": 120
      },
      "Timeout": {
        "BulkOperationTimeoutSeconds": 600
      }
    }
  }
}
```

### PostgreSQL-Specific Tuning
```json
{
  "Persistence": {
    "Resilience": {
      "RetryPolicy": {
        "MaxRetries": 3,
        "BaseDelaySeconds": 1,
        "ExponentialBackoff": true
      },
      "Timeout": {
        "DefaultCommandTimeoutSeconds": 45
      }
    }
  }
}
```

## 🎯 Best Practices

### ✅ DO
- Use **exponential backoff** for production
- Set `MaxDelaySeconds` to prevent excessive wait
- Enable **circuit breaker** for high-traffic APIs
- Configure **timeouts** based on query complexity
- Test configuration in staging before production

### ❌ DON'T
- Disable retry without understanding impact
- Set `MaxRetries` > 5 (excessive retries)
- Use fixed delay in production (inefficient)
- Set timeouts lower than connection string timeout
- Ignore retry logs (they indicate problems)

## 🔗 Related Configuration

### Connection String Resilience
```
Server=.;Database=MyDb;
Pooling=true;
Max Pool Size=300;
Min Pool Size=30;
Connection Timeout=45;
ConnectRetryCount=3;      ← Network-level retry
ConnectRetryInterval=10;
Command Timeout=90;       ← Must be >= DefaultCommandTimeoutSeconds
```

### Infrastructure Resilience (API Layer)
```json
{
  "Resilience": {
    "RateLimiting": { ... },
    "CircuitBreaker": { ... },
    "RetryPolicy": { ... }
  }
}
```

**Note**: `Persistence:Resilience` is **separate** from `Resilience` (Infrastructure layer).

## 📚 Migration Guide

### Before (Hardcoded)
```csharp
// Retry was hardcoded to 3 attempts with 2^n backoff
private static readonly AsyncRetryPolicy RetryPolicy = Policy
    .Handle<SqlException>(SqlServerExceptionHandler.IsTransientException)
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
```

### After (Configurable)
```csharp
// Retry now respects appsettings.json configuration
// Defaults remain the same if no configuration provided
private AsyncRetryPolicy RetryPolicy
{
    get
    {
        // Lazy-loaded from IOptions<PersistenceResilienceOptions>
        return _retryPolicy ??= CreateRetryPolicy();
    }
}
```

### No Code Changes Required! ✅
Existing code works without modification. Add configuration only when customization is needed.

## 🆘 Troubleshooting

### Problem: Retries not working
**Solution**: Check `Enabled = true` in `appsettings.json`

### Problem: Excessive retry delays
**Solution**: Reduce `MaxDelaySeconds` or disable `ExponentialBackoff`

### Problem: Timeout exceptions
**Solution**: Increase `DefaultCommandTimeoutSeconds` or use `CommandOptionsDto.CommandTimeout`

### Problem: Circuit breaker opening too often
**Solution**: Increase `ExceptionsAllowedBeforeBreaking` or `MinimumThroughput`

## 📖 Additional Resources

- [Polly Documentation](https://github.com/App-vNext/Polly)
- [SQL Server Transient Errors](https://learn.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues)
- [PostgreSQL Error Codes](https://www.postgresql.org/docs/current/errcodes-appendix.html)
