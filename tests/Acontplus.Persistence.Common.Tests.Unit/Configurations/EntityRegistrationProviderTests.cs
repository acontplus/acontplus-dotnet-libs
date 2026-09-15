using Acontplus.Core.Domain.Common.Entities;
using Acontplus.Persistence.Common.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Acontplus.Persistence.Common.Tests.Unit.Configurations;

public class EntityRegistrationProviderTests
{
    private sealed class SampleAuditableEntity : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class SampleSimpleEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class DummyAuditableConfig<T> : IEntityTypeConfiguration<T> where T : BaseEntity
    {
        public void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(x => x.Id);
        }
    }

    private sealed class DummySimpleConfig<T> : IEntityTypeConfiguration<T> where T : class
    {
        public void Configure(EntityTypeBuilder<T> builder)
        {
        }
    }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
    }

    [Fact]
    public void AuditableProvider_Register_RegistersEntitiesSuccessfully()
    {
        var provider = new AuditableEntityRegistrationProvider(typeof(DummyAuditableConfig<>));
        var modelBuilder = new ModelBuilder();

        provider.Register(modelBuilder, typeof(TestDbContext), typeof(SampleAuditableEntity));

        var entityType = modelBuilder.Model.FindEntityType(typeof(SampleAuditableEntity));
        Assert.NotNull(entityType);
    }

    [Fact]
    public void AuditableProvider_RegisterWithSchemas_SetsSchema()
    {
        var provider = new AuditableEntityRegistrationProvider(typeof(DummyAuditableConfig<>));
        var modelBuilder = new ModelBuilder();

        provider.RegisterWithSchemas(
            modelBuilder,
            typeof(TestDbContext),
            (typeof(SampleAuditableEntity), "custom_schema"));

        var entityType = modelBuilder.Model.FindEntityType(typeof(SampleAuditableEntity));
        Assert.NotNull(entityType);
        Assert.Equal("custom_schema", entityType.GetSchema());
    }

    [Fact]
    public void AuditableProvider_RegisterWithNames_SetsSchemaAndTable()
    {
        var provider = new AuditableEntityRegistrationProvider(typeof(DummyAuditableConfig<>));
        var modelBuilder = new ModelBuilder();

        provider.RegisterWithNames(
            modelBuilder,
            typeof(TestDbContext),
            (typeof(SampleAuditableEntity), "sales", "tbl_orders"));

        var entityType = modelBuilder.Model.FindEntityType(typeof(SampleAuditableEntity));
        Assert.NotNull(entityType);
        Assert.Equal("sales", entityType.GetSchema());
        Assert.Equal("tbl_orders", entityType.GetTableName());
    }

    [Fact]
    public void SimpleProvider_Register_RegistersEntitiesSuccessfully()
    {
        var provider = new SimpleEntityRegistrationProvider(typeof(DummySimpleConfig<>));
        var modelBuilder = new ModelBuilder();

        provider.Register(modelBuilder, typeof(TestDbContext), typeof(SampleSimpleEntity));

        var entityType = modelBuilder.Model.FindEntityType(typeof(SampleSimpleEntity));
        Assert.NotNull(entityType);
    }
}
