using System.Data;
using System.Data.Common;
using Acontplus.Core.Dtos.Requests;
using Acontplus.Core.Enums;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Acontplus.Persistence.Common.Tests.Unit.Repositories;

public class BaseAdoRepositoryTests
{
    private sealed class TestableAdoRepository : BaseAdoRepository
    {
        public TestableAdoRepository(
            IConfiguration configuration,
            ILogger logger,
            IOptions<PersistenceResilienceOptions> resilienceOptions)
            : base(configuration, logger, resilienceOptions)
        {
        }

        protected override string ProviderName => "TestEngine";
        protected override DbConnection CreateConnection(string connectionString) => throw new NotImplementedException();
        protected override bool IsTransientException(Exception exception) => false;
        protected override bool IsProviderException(Exception exception) => false;
        protected override void HandleProviderException(Exception exception, string operationName) { }
        protected override DbDataAdapter CreateDataAdapter(DbCommand command) => throw new NotImplementedException();
        protected override string SanitizeIdentifier(string identifier) => $"[{identifier}]";
        protected override string BuildPagedSql(string sql, PaginationRequest pagination) => sql;
        protected override void AddTableNamesOutputParameter(DbCommand command, CommandOptionsDto options) { }
        protected override void AddTotalCountParameter(DbCommand command) { }
        protected override int ReadTotalCountParameter(DbCommand command) => 0;
        public override Task<int> BulkInsertAsync(DataTable dataTable, string tableName, Dictionary<string, string>? columnMappings = null, int batchSize = 10000, CancellationToken cancellationToken = default) => Task.FromResult(0);

        public string ExposeAppendOrderByIfMissing(string sql, PaginationRequest pagination) =>
            AppendOrderByIfMissing(sql, pagination);
    }

    private readonly TestableAdoRepository _sut;

    public BaseAdoRepositoryTests()
    {
        var configMock = new Mock<IConfiguration>();
        var loggerMock = new Mock<ILogger>();
        var optionsMock = new Mock<IOptions<PersistenceResilienceOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new PersistenceResilienceOptions());

        _sut = new TestableAdoRepository(configMock.Object, loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public void AppendOrderByIfMissing_WhenNoOrderByAndSortByProvided_AppendsSanitizedOrderBy()
    {
        const string sql = "SELECT * FROM Users WHERE IsActive = 1";
        var pagination = new PaginationRequest
        {
            SortBy = "Username",
            SortDirection = SortDirection.Desc
        };

        var result = _sut.ExposeAppendOrderByIfMissing(sql, pagination);

        Assert.Equal("SELECT * FROM Users WHERE IsActive = 1 ORDER BY [Username] DESC", result);
    }

    [Fact]
    public void AppendOrderByIfMissing_WhenNoOrderByAndNoSortBy_AppendsDefaultOrderBy1()
    {
        const string sql = "SELECT * FROM Users";
        var pagination = new PaginationRequest();

        var result = _sut.ExposeAppendOrderByIfMissing(sql, pagination);

        Assert.Equal("SELECT * FROM Users ORDER BY 1 ASC", result);
    }

    [Fact]
    public void AppendOrderByIfMissing_WhenAlreadyHasOrderBy_ReturnsOriginalSql()
    {
        const string sql = "SELECT * FROM Users ORDER BY Id ASC";
        var pagination = new PaginationRequest
        {
            SortBy = "Username"
        };

        var result = _sut.ExposeAppendOrderByIfMissing(sql, pagination);

        Assert.Equal(sql, result);
    }
}
