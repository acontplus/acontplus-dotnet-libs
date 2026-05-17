namespace Acontplus.Core.Abstractions.Persistence;

/// <summary>
/// Defines a specification pattern for querying entities with criteria, includes, ordering, and pagination.
/// </summary>
/// <typeparam name="T">The type of entity to query.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the filter criteria expression for the query.
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }
    
    /// <summary>
    /// Gets the list of navigation properties to include in the query.
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }
    
    /// <summary>
    /// Gets the list of navigation property names to include in the query as strings.
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }
    
    /// <summary>
    /// Gets the list of order by expressions for sorting the query results.
    /// </summary>
    IReadOnlyList<OrderByExpression<T>> OrderByExpressions { get; }
    
    /// <summary>
    /// Gets the pagination request containing page index and page size.
    /// </summary>
    PaginationRequest Pagination { get; }
    
    /// <summary>
    /// Gets a value indicating whether paging is enabled for this specification.
    /// </summary>
    bool IsPagingEnabled { get; }
    
    /// <summary>
    /// Gets a value indicating whether change tracking is enabled for this specification.
    /// </summary>
    bool IsTrackingEnabled { get; }
}

/// <summary>
/// Base implementation of the specification pattern for building queries with criteria, includes, ordering, and pagination.
/// </summary>
/// <typeparam name="T">The type of entity to query.</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
{
    /// <summary>
    /// Gets the filter criteria expression for the query.
    /// </summary>
    public Expression<Func<T, bool>> Criteria { get; protected set; } = null!;

    /// <summary>
    /// Gets the list of navigation properties to include in the query.
    /// </summary>
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();
    
    /// <summary>
    /// Gets the list of navigation property names to include in the query as strings.
    /// </summary>
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();
    
    /// <summary>
    /// Gets the list of order by expressions for sorting the query results.
    /// </summary>
    public IReadOnlyList<OrderByExpression<T>> OrderByExpressions => _orderByExpressions.AsReadOnly();
    
    /// <summary>
    /// Gets the pagination request containing page index and page size.
    /// </summary>
    public PaginationRequest Pagination { get; private set; } = new();
    
    /// <summary>
    /// Gets a value indicating whether paging is enabled for this specification.
    /// </summary>
    public bool IsPagingEnabled { get; private set; } = false;
    
    /// <summary>
    /// Gets a value indicating whether change tracking is enabled for this specification.
    /// </summary>
    public bool IsTrackingEnabled { get; private set; } = false;

    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<OrderByExpression<T>> _orderByExpressions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSpecification{T}"/> class.
    /// </summary>
    /// <param name="criteria">Optional filter criteria expression. If null, all entities will match.</param>
    protected BaseSpecification(Expression<Func<T, bool>>? criteria = null)
    {
        Criteria = criteria ?? (x => true);
    }

    /// <summary>
    /// Adds a navigation property to be included in the query.
    /// </summary>
    /// <param name="includeExpression">The expression representing the navigation property to include.</param>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        _includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a navigation property to be included in the query using a string path.
    /// </summary>
    /// <param name="includeString">The string path to the navigation property to include.</param>
    protected virtual void AddInclude(string includeString)
    {
        _includeStrings.Add(includeString);
    }

    /// <summary>
    /// Adds an order by expression to sort the query results.
    /// </summary>
    /// <param name="orderByExpression">The expression to use for sorting.</param>
    /// <param name="isDescending">True to sort in descending order; otherwise, false for ascending order.</param>
    protected virtual void AddOrderBy(Expression<Func<T, object>> orderByExpression, bool isDescending = false)
    {
        _orderByExpressions.Add(new OrderByExpression<T>(orderByExpression, isDescending));
    }

    /// <summary>
    /// Applies pagination to the query.
    /// </summary>
    /// <param name="pagination">The pagination request containing page index and page size.</param>
    protected virtual void ApplyPaging(PaginationRequest pagination)
    {
        Pagination = pagination;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Configures change tracking for the query.
    /// </summary>
    /// <param name="isTracking">True to enable change tracking; otherwise, false.</param>
    protected virtual void ApplyTracking(bool isTracking = true)
    {
        IsTrackingEnabled = isTracking;
    }
}

/// <summary>
/// Represents an order by expression with sort direction for query sorting.
/// </summary>
/// <typeparam name="T">The type of entity to sort.</typeparam>
public class OrderByExpression<T>
{
    /// <summary>
    /// Gets the expression to use for sorting.
    /// </summary>
    public Expression<Func<T, object>> Expression { get; }
    
    /// <summary>
    /// Gets a value indicating whether the sort order is descending.
    /// </summary>
    public bool IsDescending { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderByExpression{T}"/> class.
    /// </summary>
    /// <param name="expression">The expression to use for sorting.</param>
    /// <param name="isDescending">True to sort in descending order; otherwise, false for ascending order.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    public OrderByExpression(Expression<Func<T, object>> expression, bool isDescending = false)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression = expression;
        IsDescending = isDescending;
    }
}
