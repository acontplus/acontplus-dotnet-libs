using Acontplus.Core.Domain.Common.Entities;
using Acontplus.Persistence.Common.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Acontplus.Persistence.Common.Tests.Unit.Configurations;

public class EntityRegistrationHelperTests
{
    private sealed class TestAuditableEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestSimpleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private abstract class AbstractAuditableEntity : BaseEntity
    {
    }

    private sealed class EntityWithoutId
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void IsValidAuditableEntity_WhenConcreteSubclassOfBaseEntity_ReturnsTrue()
    {
        var isValid = EntityRegistrationHelper.IsValidAuditableEntity(typeof(TestAuditableEntity));
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidAuditableEntity_WhenAbstract_ReturnsFalse()
    {
        var isValid = EntityRegistrationHelper.IsValidAuditableEntity(typeof(AbstractAuditableEntity));
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidAuditableEntity_WhenNotBaseEntity_ReturnsFalse()
    {
        var isValid = EntityRegistrationHelper.IsValidAuditableEntity(typeof(TestSimpleEntity));
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidSimpleEntity_WhenConcreteClassWithId_ReturnsTrue()
    {
        var isValid = EntityRegistrationHelper.IsValidSimpleEntity(typeof(TestSimpleEntity));
        Assert.True(isValid);
    }

    [Fact]
    public void IsValidSimpleEntity_WhenSubclassOfBaseEntity_ReturnsFalse()
    {
        var isValid = EntityRegistrationHelper.IsValidSimpleEntity(typeof(TestAuditableEntity));
        Assert.False(isValid);
    }

    [Fact]
    public void IsValidSimpleEntity_WhenClassWithoutId_ReturnsFalse()
    {
        var isValid = EntityRegistrationHelper.IsValidSimpleEntity(typeof(EntityWithoutId));
        Assert.False(isValid);
    }

    [Fact]
    public void RegisterEntitiesWithSchemas_WhenCalled_InvokesDelegateWithMappedSchema()
    {
        var called = false;
        var builder = new ModelBuilder();

        EntityRegistrationHelper.RegisterEntitiesWithSchemas(
            (mb, dbCtx, nameMap, customCfg, types) =>
            {
                called = true;
                Assert.NotNull(nameMap);
                Assert.True(nameMap.ContainsKey(typeof(TestAuditableEntity)));
                Assert.Equal("custom_schema", nameMap[typeof(TestAuditableEntity)].schema);
            },
            builder,
            typeof(DbContext),
            (typeof(TestAuditableEntity), "custom_schema"));

        Assert.True(called);
    }

    [Fact]
    public void RegisterEntitiesWithNames_WhenCalled_InvokesDelegateWithMappedSchemaAndTable()
    {
        var called = false;
        var builder = new ModelBuilder();

        EntityRegistrationHelper.RegisterEntitiesWithNames(
            (mb, dbCtx, nameMap, customCfg, types) =>
            {
                called = true;
                Assert.NotNull(nameMap);
                Assert.True(nameMap.ContainsKey(typeof(TestSimpleEntity)));
                Assert.Equal("my_schema", nameMap[typeof(TestSimpleEntity)].schema);
                Assert.Equal("my_table", nameMap[typeof(TestSimpleEntity)].table);
            },
            builder,
            typeof(DbContext),
            (typeof(TestSimpleEntity), "my_schema", "my_table"));

        Assert.True(called);
    }
}
