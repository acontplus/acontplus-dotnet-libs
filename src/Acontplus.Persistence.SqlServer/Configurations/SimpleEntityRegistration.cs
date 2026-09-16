using Acontplus.Persistence.Common.Configurations;

namespace Acontplus.Persistence.SqlServer.Configurations;

/// <summary>
/// Provides methods for registering simple (non-auditable) EF Core entity types with the SQL Server model builder.
/// </summary>
public sealed class SimpleEntityRegistration : EntityRegistrationDispatcher<SimpleEntityRegistration>
{
    private SimpleEntityRegistration() { }

    static SimpleEntityRegistration() =>
        InitializeProvider(new SimpleEntityRegistrationProvider(
            typeof(SimpleEntityTypeConfiguration<>)));
}
