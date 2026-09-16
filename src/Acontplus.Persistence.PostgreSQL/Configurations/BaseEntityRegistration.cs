using Acontplus.Persistence.Common.Configurations;

namespace Acontplus.Persistence.PostgreSQL.Configurations;

/// <summary>
/// Provides methods for registering EF Core entity types with the PostgreSQL model builder.
/// </summary>
public sealed class BaseEntityRegistration : EntityRegistrationDispatcher<BaseEntityRegistration>
{
    private BaseEntityRegistration() { }

    static BaseEntityRegistration() =>
        InitializeProvider(new AuditableEntityRegistrationProvider(
            typeof(BaseEntityTypeConfiguration<>),
            "must be a concrete class inheriting from BaseEntity"));
}
