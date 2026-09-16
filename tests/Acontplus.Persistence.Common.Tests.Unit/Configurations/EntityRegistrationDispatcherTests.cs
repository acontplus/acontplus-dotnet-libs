using Acontplus.Core.Domain.Common.Entities;
using Acontplus.Persistence.Common.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acontplus.Persistence.Common.Tests.Unit.Configurations;

public class EntityRegistrationDispatcherTests
{
    private sealed class SampleAuditableEntity : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class DummyAuditableConfig<T> : IEntityTypeConfiguration<T> where T : BaseEntity
    {
        public void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }

    private sealed class DummyDispatcher : EntityRegistrationDispatcher<DummyDispatcher>
    {
        static DummyDispatcher() =>
            InitializeProvider(new AuditableEntityRegistrationProvider(typeof(DummyAuditableConfig<>)));
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
    }

    [Fact]
    public void RegisterEntities_ValidEntity_RegistersSuccessfully()
    {
        var modelBuilder = new ModelBuilder();

        DummyDispatcher.RegisterEntities(modelBuilder, typeof(TestDbContext), typeof(SampleAuditableEntity));

        var entityType = modelBuilder.Model.FindEntityType(typeof(SampleAuditableEntity));
        Assert.NotNull(entityType);
    }

    [Fact]
    public void RegisterEntitiesWithSchemas_ValidSchema_AppliesSchema()
    {
        var modelBuilder = new ModelBuilder();

        DummyDispatcher.RegisterEntitiesWithSchemas(
            modelBuilder,
            typeof(TestDbContext),
            (typeof(SampleAuditableEntity), "custom_schema"));

        var entityType = modelBuilder.Model.FindEntityType(typeof(SampleAuditableEntity));
        Assert.NotNull(entityType);
        Assert.Equal("custom_schema", entityType.GetSchema());
    }

    [Fact]
    public void RegisterEntitiesWithNames_ValidName_AppliesSchemaAndTable()
    {
        var modelBuilder = new ModelBuilder();

        DummyDispatcher.RegisterEntitiesWithNames(
            modelBuilder,
            typeof(TestDbContext),
            (typeof(SampleAuditableEntity), "sales", "orders"));

        var entityType = modelBuilder.Model.FindEntityType(typeof(SampleAuditableEntity));
        Assert.NotNull(entityType);
        Assert.Equal("sales", entityType.GetSchema());
        Assert.Equal("orders", entityType.GetTableName());
    }
}
