// Assuming DiagnosticConfig is here


// Assuming DiagnosticConfig is here

namespace Acontplus.Persistence.Common.Repositories;

/// <summary>
/// A generic repository implementation for Entity Framework Core.
/// </summary>
/// <typeparam name="TEntity">The type of the entity, must be a reference type.</typeparam>
public class BaseRepository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// The underlying Entity Framework <see cref="DbContext"/>.
    /// </summary>
    protected readonly DbContext _context;

    /// <summary>
    /// The <see cref="DbSet{TEntity}"/> for the entity type.
    /// </summary>
    protected readonly DbSet<TEntity> _dbSet;

    private readonly string? _idPropertyName;

    /// <summary>
    /// The optional logger instance.
    /// </summary>
    protected readonly ILogger<BaseRepository<TEntity>>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{TEntity}"/> class.
    /// </summary>
    /// <param name="context">The database context instance.</param>
    /// <param name="logger">Optional logger instance.</param>
    public BaseRepository(DbContext context, ILogger<BaseRepository<TEntity>>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
        _logger = logger;
        _idPropertyName = GetIdPropertyName();
    }

    private static string? GetIdPropertyName()
    {
        // Look for common Id property names
        var type = typeof(TEntity);
        var idProperty = type.GetProperty("Id") ??
                         type.GetProperty("ID") ??
                         type.GetProperty($"{type.Name}Id");
        return idProperty?.Name;
    }

    #region Query Methods

    /// <inheritdoc />
    public virtual async Task<TEntity> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetByIdAsync)}");
        try
        {
            var entity = await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
            return entity ?? throw new InvalidOperationException($"Entity with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting entity of type {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
            throw new RepositoryException($"Error getting entity by ID {id}", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdOrDefaultAsync(int id, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetByIdOrDefaultAsync)}");
        try
        {
            return await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting entity of type {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
            throw new RepositoryException($"Error getting entity by ID {id}", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetByIdsAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(ids);
            var idList = ids.ToList();
            if (idList.Count == 0)
            {
                return Array.Empty<TEntity>();
            }

            var query = BuildQuery(includeProperties: includeProperties);
            if (_idPropertyName == null)
            {
                throw new InvalidOperationException(
                    $"Entity {typeof(TEntity).Name} does not have a recognizable Id property");
            }

            // Use dynamic property access
            return await query.Where(e => idList.Contains(EF.Property<int>(e, _idPropertyName)))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting entities of type {EntityType} by IDs", typeof(TEntity).Name);
            throw new RepositoryException("Error getting entities by IDs", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> FindSingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(FindSingleOrDefaultAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            var query = BuildQuery(includeProperties: includeProperties);
            return await query.SingleOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in FindSingleOrDefaultAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving single entity", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> FindSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(FindSingleAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            var query = BuildQuery(includeProperties: includeProperties);
            var entity = await query.SingleAsync(predicate, cancellationToken).ConfigureAwait(false);
            return entity ?? throw new InvalidOperationException("Single entity not found matching predicate");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in FindSingleAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving single entity", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetFirstOrDefaultAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            var query = BuildQuery(includeProperties: includeProperties);
            return await query.FirstOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetFirstOrDefaultAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving entity", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetAllAsync)}");
        try
        {
            var query = BuildQuery(includeProperties: includeProperties);
            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAllAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving all entities", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(FindAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            var query = BuildQuery(includeProperties: includeProperties);
            return await query.Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in FindAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error finding entities", ex);
        }
    }

    /// <inheritdoc />
    public virtual IAsyncEnumerable<TEntity> FindAsyncEnumerable(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(FindAsyncEnumerable)}");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return _dbSet.Where(predicate).AsNoTracking().AsAsyncEnumerable();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in FindAsyncEnumerable for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error finding entities as async enumerable", ex);
        }
    }

    /// <inheritdoc />
    public virtual Task<PagedResult<TEntity>> GetPagedAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken = default,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool orderByDescending = false) =>
        GetPagedAsync(pagination, null!, cancellationToken, orderBy, orderByDescending);

    /// <inheritdoc />
    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        PaginationRequest pagination,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool orderByDescending = false,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetPagedAsync)}");
        try
        {
            ValidatePagination(pagination);
            var query = BuildQuery(false, includeProperties);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            if (orderBy != null)
            {
                query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }
            else // Default sorting by ID if not specified
            {
                if (_idPropertyName != null)
                {
                    query = query.OrderBy(e => EF.Property<object>(e, _idPropertyName));
                }
            }

            var items = await query
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new PagedResult<TEntity>(items, pagination.PageIndex, pagination.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetPagedAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving paged results", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TProjection>> GetPagedProjectionAsync<TProjection>(
        PaginationRequest pagination,
        Expression<Func<TEntity, TProjection>> projection,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetPagedProjectionAsync)}");
        try
        {
            ValidatePagination(pagination);
            ArgumentNullException.ThrowIfNull(projection);

            var query = _dbSet.AsNoTracking();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            if (orderBy != null)
            {
                query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }
            else // Default sorting by ID if not specified
            {
                if (_idPropertyName != null)
                {
                    query = query.OrderBy(e => EF.Property<object>(e, _idPropertyName));
                }
            }

            var items = await query
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new PagedResult<TProjection>(items, pagination.PageIndex, pagination.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetPagedProjectionAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving paged projected results", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(ExistsAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return await _dbSet.AnyAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in ExistsAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error checking entity existence", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(CountAsync)}");
        try
        {
            return predicate == null
                ? await _dbSet.CountAsync(cancellationToken).ConfigureAwait(false)
                : await _dbSet.CountAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CountAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error counting entities", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<long> LongCountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(LongCountAsync)}");
        try
        {
            return predicate == null
                ? await _dbSet.LongCountAsync(cancellationToken).ConfigureAwait(false)
                : await _dbSet.LongCountAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in LongCountAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error counting entities", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TProperty?> GetMaxAsync<TProperty>(
        Expression<Func<TEntity, TProperty>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetMaxAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(selector);
            var query = _dbSet.AsNoTracking();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.MaxAsync(selector, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetMaxAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error getting max value", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TProperty?> GetMinAsync<TProperty>(
        Expression<Func<TEntity, TProperty>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetMinAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(selector);
            var query = _dbSet.AsNoTracking();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.MinAsync(selector, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetMinAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error getting min value", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<decimal> GetSumAsync(
        Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetSumAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(selector);
            var query = _dbSet.AsNoTracking();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.SumAsync(selector, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetSumAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error calculating sum", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<double> GetAverageAsync(
        Expression<Func<TEntity, decimal>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetAverageAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(selector);
            var query = _dbSet.AsNoTracking();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return (double)await query.AverageAsync(selector, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetAverageAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error calculating average", ex);
        }
    }

    #endregion

    #region Persistence Methods

    /// <inheritdoc />
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(AddAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(entity);
            var entry = await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding entity of type {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error adding entity", ex);
        }
    }

    /// <inheritdoc />
    public virtual Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(AddRangeAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(entities);
            return _dbSet.AddRangeAsync(entities, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error adding entity range of type {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error adding entity range", ex);
        }
    }

    /// <inheritdoc />
    public virtual Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(UpdateAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(entity);
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }

            entry.State = EntityState.Modified;
            return Task.FromResult(entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating entity of type {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error updating entity", ex);
        }
    }

    /// <inheritdoc />
    public virtual Task<IEnumerable<TEntity>> UpdateRangeAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(UpdateRangeAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(entities);
            var entityList = entities.ToList();
            if (entityList.Count == 0)
            {
                return Task.FromResult(Enumerable.Empty<TEntity>());
            }

            _dbSet.UpdateRange(entityList);
            return Task.FromResult<IEnumerable<TEntity>>(entityList);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating entity range of type {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error updating entity range", ex);
        }
    }

    /// <inheritdoc />
    public virtual Task<TEntity> UpdatePropertiesAsync(
        TEntity entity,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] propertiesToUpdate)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(UpdatePropertiesAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(entity);

            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }

            if (propertiesToUpdate is { Length: > 0 })
            {
                entry.State = EntityState.Unchanged;
                foreach (var property in propertiesToUpdate)
                {
                    entry.Property(property).IsModified = true;
                }
            }
            else
            {
                entry.State = EntityState.Modified;
            }

            return Task.FromResult(entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating specific properties for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error performing partial update on entity.", ex);
        }
    }

    /// <inheritdoc />
    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(DeleteAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(entity);
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting entity of type {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error deleting entity", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(DeleteByIdAsync)}");
        try
        {
            var entity = await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting entity of type {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
            throw new RepositoryException($"Error deleting entity by ID {id}", ex);
        }
    }

    /// <inheritdoc />
    public virtual Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(DeleteRangeAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(entities);
            _dbSet.RemoveRange(entities);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting entity range of type {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error deleting entity range", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(DeleteAsync)}_Predicate");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            var entities = await _dbSet.Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
            if (entities.Count > 0)
            {
                _dbSet.RemoveRange(entities);
            }

            return entities.Count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting entities by predicate for type {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error deleting entities by predicate", ex);
        }
    }

    #endregion

    #region Bulk Operations

    /// <inheritdoc />
    public virtual async Task<int> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(BulkDeleteAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return await _dbSet
                .Where(predicate)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in BulkDeleteAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error performing bulk delete", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> BulkUpdateAsync<TProperty>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TProperty>> propertyExpression,
        TProperty newValue,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(BulkUpdateAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(propertyExpression);

            return await _dbSet
                .Where(predicate)
                .ExecuteUpdateAsync(setters => setters.SetProperty(propertyExpression, newValue), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in BulkUpdateAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error performing bulk update", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> BulkInsertAsync(IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(BulkInsertAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(entities);
            var entityList = entities.ToList();
            if (entityList.Count == 0)
            {
                return 0;
            }

            await _dbSet.AddRangeAsync(entityList, cancellationToken).ConfigureAwait(false);
            // Bulk insert is typically a "unit of work" in itself, so SaveChanges might be called here
            // or by the UnitOfWork. For this implementation, we assume UoW handles it.
            return entityList.Count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in BulkInsertAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error performing bulk insert", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> BulkUpdateAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TEntity>> updateExpression,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(BulkUpdateAsync)}_Expression");
        try
        {
            ArgumentNullException.ThrowIfNull(predicate);
            ArgumentNullException.ThrowIfNull(updateExpression);

            // For EF Core 7+, we can use ExecuteUpdateAsync with a more complex update expression
            // This is a simplified implementation - in practice, you'd need to analyze the updateExpression
            // and convert it to the proper SetProperty calls
            var entities = await _dbSet.Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
            var compiledUpdate = updateExpression.Compile();

            foreach (var entity in entities)
            {
                var updatedEntity = compiledUpdate(entity);
                _context.Entry(entity).CurrentValues.SetValues(updatedEntity);
            }

            return entities.Count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in BulkUpdateAsync with expression for entity {EntityType}",
                typeof(TEntity).Name);
            throw new RepositoryException("Error performing bulk update with expression", ex);
        }
    }

    #endregion

    #region Specification Pattern

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindWithSpecificationAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(FindWithSpecificationAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(specification);
            return await BuildSpecificationQuery(specification).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in FindWithSpecificationAsync for {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving entities with specification", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TEntity> GetFirstOrDefaultWithSpecificationAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        using var activity =
            DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetFirstOrDefaultWithSpecificationAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(specification);
            var entity = await BuildSpecificationQuery(specification).FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            return entity ?? throw new InvalidOperationException("No entity found matching specification");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetFirstOrDefaultWithSpecificationAsync for {EntityType}",
                typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving entity with specification", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TEntity>> GetPagedWithSpecificationAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetPagedWithSpecificationAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(specification);
            ValidatePagination(specification.Pagination);

            var query = BuildSpecificationQuery(specification, true);
            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var pagedQuery = ApplyPaging(query, specification);
            var items = await pagedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

            return new PagedResult<TEntity>(items, specification.Pagination.PageIndex,
                specification.Pagination.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetPagedWithSpecificationAsync for {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving paged results with specification", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TProjection>> FindProjectionWithSpecificationAsync<TProjection>(
        ISpecification<TEntity> specification,
        Expression<Func<TEntity, TProjection>> projection,
        CancellationToken cancellationToken = default)
    {
        using var activity =
            DiagnosticConfig.ActivitySource.StartActivity($"{nameof(FindProjectionWithSpecificationAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(specification);
            ArgumentNullException.ThrowIfNull(projection);

            var query = BuildSpecificationQuery(specification).Select(projection);
            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in FindProjectionWithSpecificationAsync for {EntityType}",
                typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving projected results with specification", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> CountWithSpecificationAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(CountWithSpecificationAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(specification);
            var query = BuildSpecificationQuery(specification, true, true);
            return await query.CountAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in CountWithSpecificationAsync for {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error counting entities with specification", ex);
        }
    }

    #endregion

    #region Advanced Query Operations

    /// <inheritdoc />
    public virtual IQueryable<TEntity> GetQueryable(
        bool tracking = false,
        params Expression<Func<TEntity, object>>[] includeProperties) =>
        BuildQuery(tracking, includeProperties);

    /// <inheritdoc />
    public virtual Task<TResult> ExecuteQueryAsync<TResult>(
        Expression<Func<IQueryable<TEntity>, TResult>> queryExpression,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(ExecuteQueryAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(queryExpression);
            var query = _dbSet.AsNoTracking();
            var compiledExpression = queryExpression.Compile();
            var result = compiledExpression(query);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing custom query for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error executing custom query", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TResult>> ExecuteQueryToListAsync<TResult>(
        Expression<Func<IQueryable<TEntity>, IQueryable<TResult>>> queryExpression,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(ExecuteQueryToListAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(queryExpression);
            var query = _dbSet.AsNoTracking();
            var compiledExpression = queryExpression.Compile();
            var resultQuery = compiledExpression(query);
            return await resultQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing custom query to list for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error executing custom query to list", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TResult>> ExecutePagedQueryAsync<TResult>(
        Expression<Func<IQueryable<TEntity>, IQueryable<TResult>>> queryExpression,
        PaginationRequest pagination,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(ExecutePagedQueryAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(queryExpression);
            ValidatePagination(pagination);

            var query = _dbSet.AsNoTracking();
            var compiledExpression = queryExpression.Compile();
            var resultQuery = compiledExpression(query);

            // Get total count
            var totalCount = await resultQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Apply pagination
            var items = await resultQuery
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new PagedResult<TResult>(items, pagination.PageIndex, pagination.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing paged custom query for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error executing paged custom query", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetOrderedAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params (Expression<Func<TEntity, object>> KeySelector, bool Descending)[] orderExpressions)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetOrderedAsync)}");
        try
        {
            var query = _dbSet.AsNoTracking();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            query = ApplyOrdering(query, orderExpressions);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetOrderedAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving ordered entities", ex);
        }
    }

    /// <inheritdoc />
    public virtual Task<TResult> AggregateAsync<TResult>(
        Expression<Func<IQueryable<TEntity>, TResult>> aggregateExpression,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(AggregateAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(aggregateExpression);

            var query = _dbSet.AsNoTracking();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // This is a bit complex. The expression needs to be invoked.
            // EF Core can often translate this if the expression is simple enough (e.g., q => q.Sum(e => e.Property))
            // This approach provides maximum flexibility.
            var compiledAggregate = aggregateExpression.Compile();
            var result = compiledAggregate(query);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AggregateAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error performing aggregation", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TProperty>> GetDistinctAsync<TProperty>(
        Expression<Func<TEntity, TProperty>> propertySelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetDistinctAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(propertySelector);

            var query = _dbSet.AsNoTracking();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.Select(propertySelector).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetDistinctAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving distinct values", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TProjection>> GetProjectionAsync<TProjection>(
        Expression<Func<TEntity, TProjection>> projection,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetProjectionAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(projection);
            var query = BuildQuery(false, includeProperties);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.Select(projection).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetProjectionAsync for entity {EntityType}", typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving projected results", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<TProjection?> GetFirstProjectionOrDefaultAsync<TProjection>(
        Expression<Func<TEntity, TProjection>> projection,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        using var activity =
            DiagnosticConfig.ActivitySource.StartActivity($"{nameof(GetFirstProjectionOrDefaultAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(projection);
            var query = BuildQuery(false, includeProperties);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.Select(projection).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in GetFirstProjectionOrDefaultAsync for entity {EntityType}",
                typeof(TEntity).Name);
            throw new RepositoryException("Error retrieving first projected result", ex);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Builds an entity query with optional tracking and eager loading inclusions.
    /// </summary>
    /// <param name="tracking">Whether to enable change tracking. Defaults to <see langword="true"/>.</param>
    /// <param name="includeProperties">Navigation properties to eagerly load.</param>
    /// <returns>An <see cref="IQueryable{TEntity}"/> query configured with the specified options.</returns>
    protected virtual IQueryable<TEntity> BuildQuery(
        bool tracking = true,
        params Expression<Func<TEntity, object>>[] includeProperties)
    {
        var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();

        if (includeProperties is { Length: > 0 })
        {
            query = includeProperties.Aggregate(query, (current, include) => current.Include(include));
        }

        return query;
    }

    /// <summary>
    /// Builds an entity query based on a domain specification.
    /// </summary>
    /// <param name="spec">The specification to apply.</param>
    /// <param name="ignorePaging">Whether to ignore pagination rules defined in the specification.</param>
    /// <param name="ignoreOrdering">Whether to ignore ordering rules defined in the specification.</param>
    /// <returns>An <see cref="IQueryable{TEntity}"/> query filtered by the specification.</returns>
    protected virtual IQueryable<TEntity> BuildSpecificationQuery(ISpecification<TEntity> spec,
        bool ignorePaging = false, bool ignoreOrdering = false)
    {
        var query = spec.IsTrackingEnabled ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();

        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria);
        }

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (!ignoreOrdering && spec.OrderByExpressions.Count > 0)
        {
            query = ApplyOrdering(query, spec.OrderByExpressions.Select(o => (o.Expression, o.IsDescending)).ToArray());
        }

        if (!ignorePaging && spec.IsPagingEnabled)
        {
            query = ApplyPaging(query, spec);
        }

        return query;
    }

    /// <summary>
    /// Applies pagination parameters from a specification to the query.
    /// </summary>
    /// <param name="query">The base query.</param>
    /// <param name="spec">The specification with paging parameters.</param>
    /// <returns>The paginated query.</returns>
    protected virtual IQueryable<TEntity> ApplyPaging(IQueryable<TEntity> query, ISpecification<TEntity> spec)
    {
        return query.Skip((spec.Pagination.PageIndex - 1) * spec.Pagination.PageSize)
            .Take(spec.Pagination.PageSize);
    }

    /// <summary>
    /// Applies ordering expressions from a specification to the query.
    /// </summary>
    /// <param name="query">The base query.</param>
    /// <param name="orderExpressions">Ordering expressions specifying key selector and direction.</param>
    /// <returns>The ordered query.</returns>
    protected virtual IQueryable<TEntity> ApplyOrdering(IQueryable<TEntity> query,
        params (Expression<Func<TEntity, object>> KeySelector, bool Descending)[] orderExpressions)
    {
        if (orderExpressions is null || orderExpressions.Length == 0)
        {
            return _idPropertyName != null
                ? query.OrderBy(e => EF.Property<object>(e, _idPropertyName))
                : query; // Default order
        }

        IOrderedQueryable<TEntity>? orderedQuery = null;
        foreach (var (keySelector, descending) in orderExpressions)
        {
            if (orderedQuery is null)
            {
                orderedQuery = descending
                    ? query.OrderByDescending(keySelector)
                    : query.OrderBy(keySelector);
            }
            else
            {
                orderedQuery = descending
                    ? orderedQuery.ThenByDescending(keySelector)
                    : orderedQuery.ThenBy(keySelector);
            }
        }

        return orderedQuery ?? query;
    }

    /// <summary>
    /// Validates pagination parameters to ensure valid page number and page size.
    /// </summary>
    /// <param name="pagination">The pagination request to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pagination"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when page number or page size is invalid.</exception>
    protected virtual void ValidatePagination(PaginationRequest pagination)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        if (pagination.PageIndex < 1)
        {
            throw new ArgumentException("Page index must be greater than 0.", nameof(pagination.PageIndex));
        }

        if (pagination.PageSize < 1 || pagination.PageSize > 500) // Max page size guard
        {
            throw new ArgumentException("Page size must be between 1 and 500.", nameof(pagination.PageSize));
        }
    }

    #endregion


    #region Transaction Support

    /// <inheritdoc />
    public virtual async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(ExecuteInTransactionAsync)}");
        try
        {
            ArgumentNullException.ThrowIfNull(operation);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await operation().ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing operation in transaction for entity {EntityType}",
                typeof(TEntity).Name);
            throw new RepositoryException("Error executing operation in transaction", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        using var activity = DiagnosticConfig.ActivitySource.StartActivity($"{nameof(ExecuteInTransactionAsync)}_Void");
        try
        {
            ArgumentNullException.ThrowIfNull(operation);

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await operation().ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error executing operation in transaction for entity {EntityType}",
                typeof(TEntity).Name);
            throw new RepositoryException("Error executing operation in transaction", ex);
        }
    }

    #endregion
}
