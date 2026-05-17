namespace Acontplus.Core.Abstractions.Persistence;

/// <summary>
/// Generic repository interface for data access operations.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
public interface IRepository<TEntity>
    where TEntity : class
{
    #region Query Methods

    /// <summary>
    /// Gets an entity by its ID. Throws if not found.
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity</returns>
    Task<TEntity> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by its ID, or null if not found.
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity or null</returns>
    Task<TEntity?> GetByIdOrDefaultAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the predicate, or null if not found.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>The first matching entity or null</returns>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>All entities</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Finds all entities matching the predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>Matching entities</returns>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Finds a single entity matching the predicate, or null if not found.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>The single matching entity or null</returns>
    Task<TEntity?> FindSingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Finds a single entity matching the predicate. Throws if not found or multiple found.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>The single matching entity</returns>
    Task<TEntity> FindSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Finds entities matching the predicate as an async enumerable stream.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of matching entities</returns>
    IAsyncEnumerable<TEntity> FindAsyncEnumerable(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged result of all entities.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="orderBy">Optional ordering expression</param>
    /// <param name="orderByDescending">Whether to order descending</param>
    /// <returns>Paged result</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool orderByDescending = false);

    /// <summary>
    /// Gets a paged result of entities matching the predicate.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="orderBy">Optional ordering expression</param>
    /// <param name="orderByDescending">Whether to order descending</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>Paged result</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(
        PaginationRequest pagination,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool orderByDescending = false,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Gets a paged result of projected entities.
    /// </summary>
    /// <typeparam name="TProjection">The projection type</typeparam>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="projection">The projection expression</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="orderBy">Optional ordering expression</param>
    /// <param name="orderByDescending">Whether to order descending</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of projections</returns>
    Task<PagedResult<TProjection>> GetPagedProjectionAsync<TProjection>(
        PaginationRequest pagination,
        Expression<Func<TEntity, TProjection>> projection,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches, otherwise false</returns>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the optional predicate.
    /// </summary>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count</returns>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the optional predicate as a long.
    /// </summary>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count as long</returns>
    Task<long> LongCountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities by their IDs.
    /// </summary>
    /// <param name="ids">The entity IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>The entities with matching IDs</returns>
    Task<IReadOnlyList<TEntity>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Gets the maximum value of a property.
    /// </summary>
    /// <typeparam name="TProperty">The property type</typeparam>
    /// <param name="selector">The property selector</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The maximum value</returns>
    Task<TProperty?> GetMaxAsync<TProperty>(
        Expression<Func<TEntity, TProperty>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the minimum value of a property.
    /// </summary>
    /// <typeparam name="TProperty">The property type</typeparam>
    /// <param name="selector">The property selector</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The minimum value</returns>
    Task<TProperty?> GetMinAsync<TProperty>(
        Expression<Func<TEntity, TProperty>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the sum of a decimal property.
    /// </summary>
    /// <param name="selector">The property selector</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The sum</returns>
    Task<decimal> GetSumAsync(
        Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average of a decimal property.
    /// </summary>
    /// <param name="selector">The property selector</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The average</returns>
    Task<double> GetAverageAsync(
        Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Persistence Methods

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added entity</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository.
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated entity</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities.
    /// </summary>
    /// <param name="entities">The entities to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated entities</returns>
    Task<IEnumerable<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates specific properties of an entity.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="propertiesToUpdate">The properties to update</param>
    /// <returns>The updated entity</returns>
    Task<TEntity> UpdatePropertiesAsync(
        TEntity entity,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertiesToUpdate);

    /// <summary>
    /// Deletes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the entity was deleted, otherwise false</returns>
    Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities.
    /// </summary>
    /// <param name="entities">The entities to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes entities matching the predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of entities deleted</returns>
    Task<int> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Bulk deletes entities matching the predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of entities deleted</returns>
    Task<int> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates a specific property for entities matching the predicate.
    /// </summary>
    /// <typeparam name="TProperty">The property type</typeparam>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="propertyExpression">The property to update</param>
    /// <param name="newValue">The new value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of entities updated</returns>
    Task<int> BulkUpdateAsync<TProperty>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        TProperty newValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk inserts multiple entities.
    /// </summary>
    /// <param name="entities">The entities to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of entities inserted</returns>
    Task<int> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates entities matching the predicate using an update expression.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <param name="updateExpression">The update expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of entities updated</returns>
    Task<int> BulkUpdateAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TEntity>> updateExpression,
        CancellationToken cancellationToken = default);

    #endregion

    #region Specification Pattern

    /// <summary>
    /// Finds entities using a specification.
    /// </summary>
    /// <param name="specification">The specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching entities</returns>
    Task<IReadOnlyList<TEntity>> FindWithSpecificationAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching a specification, or default if not found.
    /// </summary>
    /// <param name="specification">The specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching entity or default</returns>
    Task<TEntity> GetFirstOrDefaultWithSpecificationAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged result using a specification.
    /// </summary>
    /// <param name="specification">The specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result</returns>
    Task<PagedResult<TEntity>> GetPagedWithSpecificationAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds projected entities using a specification.
    /// </summary>
    /// <typeparam name="TProjection">The projection type</typeparam>
    /// <param name="specification">The specification</param>
    /// <param name="projection">The projection expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Projected entities</returns>
    Task<IReadOnlyList<TProjection>> FindProjectionWithSpecificationAsync<TProjection>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TProjection>> projection,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching a specification.
    /// </summary>
    /// <param name="specification">The specification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count</returns>
    Task<int> CountWithSpecificationAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    #endregion

    #region Advanced Query Operations

    /// <summary>
    /// Gets a queryable for building complex queries with joins, custom projections, and advanced filtering.
    /// Use this for scenarios that can't be handled by the standard repository methods.
    /// </summary>
    /// <param name="tracking">Whether to enable change tracking</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>A queryable for building complex queries</returns>
    IQueryable<TEntity> GetQueryable(
        bool tracking = false,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Executes a custom query expression and returns the results.
    /// Use this for complex queries that require custom logic.
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="queryExpression">The custom query expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The query results</returns>
    Task<TResult> ExecuteQueryAsync<TResult>(
        Expression<Func<IQueryable<TEntity>, TResult>> queryExpression,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a custom query expression and returns the results as a list.
    /// Use this for complex queries that return collections.
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="queryExpression">The custom query expression</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The query results as a list</returns>
    Task<IReadOnlyList<TResult>> ExecuteQueryToListAsync<TResult>(
        Expression<Func<IQueryable<TEntity>, IQueryable<TResult>>> queryExpression,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a custom query with pagination and returns paged results.
    /// Use this for complex queries that need pagination.
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="queryExpression">The custom query expression</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged query results</returns>
    Task<PagedResult<TResult>> ExecutePagedQueryAsync<TResult>(
        Expression<Func<IQueryable<TEntity>, IQueryable<TResult>>> queryExpression,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with custom ordering.
    /// </summary>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="orderExpressions">Order expressions with direction</param>
    /// <returns>Ordered entities</returns>
    Task<IReadOnlyList<TEntity>> GetOrderedAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params (Expression<Func<TEntity, object>> KeySelector, bool Descending)[] orderExpressions);

    /// <summary>
    /// Executes an aggregate expression.
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="aggregateExpression">The aggregate expression</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate result</returns>
    Task<TResult> AggregateAsync<TResult>(
        Expression<Func<IQueryable<TEntity>, TResult>> aggregateExpression,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct values of a property.
    /// </summary>
    /// <typeparam name="TProperty">The property type</typeparam>
    /// <param name="propertySelector">The property selector</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Distinct property values</returns>
    Task<IReadOnlyList<TProperty>> GetDistinctAsync<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projected entities.
    /// </summary>
    /// <typeparam name="TProjection">The projection type</typeparam>
    /// <param name="projection">The projection expression</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>Projected entities</returns>
    Task<IReadOnlyList<TProjection>> GetProjectionAsync<TProjection>(
        Expression<Func<TEntity, TProjection>> projection,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    /// <summary>
    /// Gets the first projected entity or default.
    /// </summary>
    /// <typeparam name="TProjection">The projection type</typeparam>
    /// <param name="projection">The projection expression</param>
    /// <param name="predicate">Optional filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>The first projected entity or default</returns>
    Task<TProjection?> GetFirstProjectionOrDefaultAsync<TProjection>(
        Expression<Func<TEntity, TProjection>> projection,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties);

    #endregion

    #region Transaction Support

    /// <summary>
    /// Executes an operation within a transaction and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The operation result</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation within a transaction.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default);
    #endregion
}
