using System.Data;
using System.Data.Common;
using Acontplus.Core.Dtos.Requests;
using Acontplus.Core.Enums;
using Acontplus.Persistence.Common.Configuration;
using Acontplus.Persistence.Common.Repositories;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Acontplus.Persistence.Common.Tests.Unit.Repositories;

public class BaseDapperRepositoryTests
{
    private sealed class TestableDapperRepository : BaseDapperRepository
    {
        public TestableDapperRepository(
            IConfiguration configuration,
            ILogger logger,
            IOptions<PersistenceResilienceOptions> resilienceOptions)
            : base(configuration, logger, resilienceOptions)
        {
        }

        protected override string ProviderName => "TestDapper";
        protected override DbConnection CreateConnection(string connectionString) => throw new NotImplementedException();
        protected override bool IsTransientException(Exception exception) => false;
        protected override string SanitizeIdentifier(string identifier) => $"[{identifier}]";
        protected override string BuildPagedSql(string sql, string orderByClause, PaginationRequest pagination, DynamicParameters parameters) =>
            $"{sql} {orderByClause} LIMIT {pagination.PageSize}";

        protected override (CommandDefinition Command, Func<List<T>, int> TotalCountExtractor) CreatePagedStoredProcedureCommand<T>(
            string storedProcedureName,
            PaginationRequest pagination,
            int? commandTimeout,
            CancellationToken cancellationToken)
        {
            var dynamicParams = BuildDynamicParameters(pagination);
            var command = new CommandDefinition(storedProcedureName, dynamicParams, CurrentTransaction, commandTimeout ?? DefaultTimeout, CommandType.StoredProcedure, cancellationToken: cancellationToken);
            return (command, items => items.Count);
        }

        protected override CommandDefinition CreateFilteredStoredProcedureCommand(
            string storedProcedureName,
            FilterRequest filter,
            int? commandTimeout,
            CancellationToken cancellationToken)
        {
            var dynamicParams = BuildDynamicParameters(filter);
            return new CommandDefinition(storedProcedureName, dynamicParams, CurrentTransaction, commandTimeout ?? DefaultTimeout, CommandType.StoredProcedure, cancellationToken: cancellationToken);
        }

        public string ExposeBuildOrderByClause(PaginationRequest pagination) => BuildOrderByClause(pagination);
        public string ExposeBuildOrderByClause(FilterRequest filter) => BuildOrderByClause(filter);
        public string ExposeGenerateCountQuery(string sql) => GenerateCountQuery(sql);
        public DynamicParameters ExposeBuildDynamicParameters(PaginationRequest pagination) => BuildDynamicParameters(pagination);
        public DynamicParameters ExposeBuildDynamicParameters(FilterRequest filter) => BuildDynamicParameters(filter);
    }

    private readonly TestableDapperRepository _sut;

    public BaseDapperRepositoryTests()
    {
        var configMock = new Mock<IConfiguration>();
        var loggerMock = new Mock<ILogger>();
        var optionsMock = new Mock<IOptions<PersistenceResilienceOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new PersistenceResilienceOptions());

        _sut = new TestableDapperRepository(configMock.Object, loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public void BuildOrderByClause_WithSortBy_ReturnsSanitizedOrderByClause()
    {
        var pagination = new PaginationRequest
        {
            SortBy = "UserName",
            SortDirection = SortDirection.Desc
        };

        var result = _sut.ExposeBuildOrderByClause(pagination);

        Assert.Equal("ORDER BY [UserName] DESC", result);
    }

    [Fact]
    public void BuildOrderByClause_WithoutSortBy_ReturnsDefaultOrderByClause()
    {
        var pagination = new PaginationRequest();

        var result = _sut.ExposeBuildOrderByClause(pagination);

        Assert.Equal("ORDER BY (SELECT NULL)", result);
    }

    [Fact]
    public void GenerateCountQuery_WrapsSqlInCountSelect()
    {
        const string sql = "SELECT Id, Name FROM Products WHERE Price > 10";

        var result = _sut.ExposeGenerateCountQuery(sql);

        Assert.Equal("SELECT COUNT(*) FROM (SELECT Id, Name FROM Products WHERE Price > 10) AS CountQuery", result);
    }

    [Fact]
    public void BuildDynamicParameters_WithFilters_PopulatesDynamicParameters()
    {
        var filter = new FilterRequest
        {
            Filters = new Dictionary<string, object>
            {
                ["Status"] = "Active",
                ["Level"] = 5
            }
        };

        var parameters = _sut.ExposeBuildDynamicParameters(filter);

        Assert.NotNull(parameters);
        Assert.Equal("Active", parameters.Get<string>("Status"));
        Assert.Equal(5, parameters.Get<int>("Level"));
    }

    [Fact]
    public void TransactionLifecycle_SetAndClear_OperatesCorrectly()
    {
        var transactionMock = new Mock<DbTransaction>();
        var connectionMock = new Mock<DbConnection>();

        _sut.SetTransaction(transactionMock.Object);
        _sut.SetConnection(connectionMock.Object);

        _sut.ClearTransaction();
    }
}
