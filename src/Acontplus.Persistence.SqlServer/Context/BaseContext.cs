using Acontplus.Core.Domain.Common.Events;

namespace Acontplus.Persistence.SqlServer.Context;

/// <summary>
/// Options that control SQL Server-specific model builder behaviour.
/// </summary>
public class SqlServerModelBuilderOptions
{
    /// <summary>Gets or sets a value indicating whether decimal precision/scale conversion is applied. Defaults to <c>true</c>.</summary>
    public bool EnableDecimalConversion { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether string properties are mapped as non-Unicode. Defaults to <c>true</c>.</summary>
    public bool EnableNonUnicodeStrings { get; set; } = true;
}

/// <summary>
/// Base EF Core database context for SQL Server with support for domain events, timestamp auditing, and soft deletes.
/// </summary>
/// <remarks>
/// Audit identity fields are populated automatically by <see cref="Acontplus.Persistence.SqlServer.Interceptors.AuditSaveChangesInterceptor"/>.
/// </remarks>
public abstract class BaseContext : Common.Context.BaseContext
{
    private readonly SqlServerModelBuilderOptions _sqlServerOptions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    protected BaseContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseContext"/> class with a domain event dispatcher.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    /// <param name="eventDispatcher">The domain event dispatcher.</param>
    protected BaseContext(DbContextOptions options, IDomainEventDispatcher eventDispatcher)
        : base(options, eventDispatcher)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (Database.IsSqlServer())
        {
            ApplySqlServerConfigurations(modelBuilder);
        }
    }

    /// <summary>
    /// Applies SQL Server-specific model configurations such as decimal precision and non-Unicode string mappings.
    /// </summary>
    /// <param name="builder">The ModelBuilder instance.</param>
    protected virtual void ApplySqlServerConfigurations(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (_sqlServerOptions.EnableDecimalConversion)
        {
            foreach (var property in builder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }
        }

        if (_sqlServerOptions.EnableNonUnicodeStrings)
        {
            foreach (var property in builder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(string) && p.GetColumnType() == null))
            {
                property.SetIsUnicode(false);
            }
        }
    }
}
