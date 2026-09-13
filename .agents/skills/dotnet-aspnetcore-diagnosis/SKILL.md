---
name: dotnet-aspnetcore-diagnosis
description: >-
  Diagnose and resolve ASP.NET Core startup failures and runtime issues.
  Use when the application fails to start, DI registration errors occur,
  middleware ordering issues arise, or configuration binding problems appear.
  Covers dependency injection diagnostics, middleware pipeline, Kestrel
  configuration, and common startup patterns.
---

# ASP.NET Core Startup Diagnosis

## When to Use

Activate this skill when:

* `Demo.Api` or consumer ASP.NET Core APIs fail to start with DI or configuration errors.
* Middleware ordering causes unexpected behavior in `Acontplus.Infrastructure` pipeline.
* Service registration issues (`AddAcontplus...`) cause runtime exceptions.
* Configuration binding fails for options or connection strings.

## Common Startup Failures

### 1. DI Registration Errors

**Symptom**: `InvalidOperationException: Unable to resolve service for type 'IMyService'`

**Diagnosis**:

```bash
# Search for the service registration
grep -r "IMyService" src/ apps/src/ --include="*.cs"
```

**Common Causes**:
* Service not registered in DI container.
* Missing registration extension call (e.g. `services.AddAcontplus...()`).
* Interface/implementation mismatch (wrong namespace, wrong assembly).

**Fix**: Ensure the required package extension methods are called in `Program.cs`:
1. Call appropriate `AddAcontplusCore()`, `AddAcontplusPersistence()`, etc.
2. If custom service, register explicitly with correct lifetime (`AddScoped`, `AddSingleton`, `AddTransient`).

### 2. Configuration Binding Failures

**Symptom**: Options objects have default/null values at runtime.

**Diagnosis**:
```csharp
// Verify the configuration section exists
var section = builder.Configuration.GetSection("MySection");
Console.WriteLine($"Exists: {section.Exists()}");
Console.WriteLine($"Value: {section.Value}");
```

**Common Causes**:
* Missing section in `appsettings.json`.
* Property name mismatch (case sensitivity).
* Missing `builder.Services.Configure<MyOptions>(...)` call.

### 3. Middleware Ordering Issues

**Critical ordering for Acontplus APIs (`Demo.Api`)**:
```
1. UseApplicationMiddleware (from Acontplus.Infrastructure)
2. UseAuthentication
3. UseAuthorization
4. CORS / Antiforgery
5. Endpoint routing (MapGroup, MapEndpoints)
```

### 4. Database Connection Failures

**Symptom**: `NpgsqlException: Failed to connect to host` or `SqlException: Cannot open database`

**Diagnosis**:
```bash
# Verify connection string configuration
grep -r "ConnectionStrings" apps/src/Demo.Api/appsettings*.json
```

### 5. Migration Failures

```bash
# Apply migrations for Demo app
dotnet ef database update --project apps/src/Demo.Infrastructure --startup-project apps/src/Demo.Api
```

## Debugging Tips

### Enable Detailed Errors in Development

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

### Log DI Container Contents

```csharp
// In Program.cs after building the app
var services = app.Services.GetServices<IMyService>();
app.Logger.LogInformation("Registered {Count} IMyService implementations", services.Count());
```

### Validate Configuration at Startup

```csharp
builder.Services.AddOptions<MyOptions>()
    .BindConfiguration("MySection")
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fail fast if misconfigured
```
