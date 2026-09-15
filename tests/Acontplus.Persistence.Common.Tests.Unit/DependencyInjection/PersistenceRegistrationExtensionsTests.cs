using Acontplus.Core.Abstractions.Persistence;
using Acontplus.Persistence.Common.DependencyInjection;
using Acontplus.Persistence.Common.Repositories;
using Acontplus.Persistence.Common.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Acontplus.Persistence.Common.Tests.Unit.DependencyInjection;

public class PersistenceRegistrationExtensionsTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext() : base(new DbContextOptions<TestDbContext>())
        {
        }
    }

    private sealed class TestAdoRepository : BaseAdoRepository
    {
        public TestAdoRepository(
            Microsoft.Extensions.Configuration.IConfiguration config,
            Microsoft.Extensions.Logging.ILogger<TestAdoRepository> logger,
            Microsoft.Extensions.Options.IOptions<Acontplus.Persistence.Common.Configuration.PersistenceResilienceOptions> options)
            : base(config, logger, options)
        {
        }

        protected override string ProviderName => "Test";
        protected override System.Data.Common.DbConnection CreateConnection(string connectionString) => throw new NotImplementedException();
        protected override bool IsTransientException(Exception exception) => false;
        protected override bool IsProviderException(Exception exception) => false;
        protected override void HandleProviderException(Exception exception, string operationName) { }
        protected override System.Data.Common.DbDataAdapter CreateDataAdapter(System.Data.Common.DbCommand command) => throw new NotImplementedException();
        protected override string SanitizeIdentifier(string identifier) => identifier;
        protected override string BuildPagedSql(string sql, Acontplus.Core.Dtos.Requests.PaginationRequest pagination) => sql;
        protected override void AddTableNamesOutputParameter(System.Data.Common.DbCommand command, Acontplus.Core.Dtos.Requests.CommandOptionsDto options) { }
        protected override void AddTotalCountParameter(System.Data.Common.DbCommand command) { }
        protected override int ReadTotalCountParameter(System.Data.Common.DbCommand command) => 0;
        public override Task<int> BulkInsertAsync(System.Data.DataTable dataTable, string tableName, Dictionary<string, string>? columnMappings = null, int batchSize = 10000, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    [Fact]
    public void RegisterPersistenceCore_WithoutServiceKey_RegistersStandardScopedServices()
    {
        var services = new ServiceCollection();

        services.RegisterPersistenceCore<TestDbContext, TestAdoRepository>();

        var adoDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAdoRepository));
        var uowDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUnitOfWork));
        var dbDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(DbContext));

        Assert.NotNull(adoDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, adoDescriptor.Lifetime);
        Assert.Equal(typeof(TestAdoRepository), adoDescriptor.ImplementationType);

        Assert.NotNull(uowDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, uowDescriptor.Lifetime);
        Assert.Equal(typeof(UnitOfWork<TestDbContext>), uowDescriptor.ImplementationType);

        Assert.NotNull(dbDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, dbDescriptor.Lifetime);
    }

    [Fact]
    public void RegisterPersistenceCore_WithServiceKey_RegistersKeyedScopedServices()
    {
        var services = new ServiceCollection();
        const string key = "tenant_1";

        services.RegisterPersistenceCore<TestDbContext, TestAdoRepository>(key);

        var adoDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAdoRepository) && Equals(s.ServiceKey, key));
        var uowDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUnitOfWork) && Equals(s.ServiceKey, key));
        var dbDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(DbContext) && Equals(s.ServiceKey, key));

        Assert.NotNull(adoDescriptor);
        Assert.True(adoDescriptor.IsKeyedService);
        Assert.Equal(ServiceLifetime.Scoped, adoDescriptor.Lifetime);

        Assert.NotNull(uowDescriptor);
        Assert.True(uowDescriptor.IsKeyedService);

        Assert.NotNull(dbDescriptor);
        Assert.True(dbDescriptor.IsKeyedService);
    }
}
