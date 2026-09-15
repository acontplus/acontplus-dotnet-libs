using System.Data;
using System.Data.Common;
using Acontplus.Persistence.Common.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Acontplus.Persistence.Common.UnitOfWork;

/// <summary>
/// Coordinates EF Core and ADO.NET operations within a single transactional unit of work.
/// </summary>
/// <typeparam name="TContext">The EF Core database context type.</typeparam>
public class UnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly ILogger<UnitOfWork<TContext>>? _logger;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private readonly SemaphoreSlim _transactionSemaphore = new(1, 1);
    private bool _disposed;

    private IDbContextTransaction? _efTransaction;

    /// <summary>
    /// Initializes a new <see cref="UnitOfWork{TContext}"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    /// <param name="adoRepository">The ADO.NET repository for raw database operations.</param>
    /// <param name="logger">Optional logger instance.</param>
    public UnitOfWork(
        TContext context,
        IAdoRepository adoRepository,
        ILogger<UnitOfWork<TContext>>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(adoRepository);
        _context = context;
        AdoRepository = adoRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public DbTransaction? CurrentDbTransaction => _efTransaction?.GetDbTransaction();

    /// <inheritdoc/>
    public DbConnection CurrentDbConnection => _context.Database.GetDbConnection();

    /// <inheritdoc/>
    public IAdoRepository AdoRepository { get; }

    /// <inheritdoc/>
    public bool HasActiveTransaction => _efTransaction is not null;

    // Explicit interface implementation to handle nullability
    DbTransaction IUnitOfWork.CurrentDbTransaction => CurrentDbTransaction!;

    /// <inheritdoc/>
    public IRepository<TEntity> GetRepository<TEntity>()
        where TEntity : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return (IRepository<TEntity>)_repositories.GetOrAdd(typeof(TEntity), static (type, context) =>
                new BaseRepository<TEntity>(context._context, context._logger as ILogger<BaseRepository<TEntity>>),
            this);
    }

    /// <inheritdoc/>
    public async Task<ITransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _transactionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_efTransaction is not null)
            {
                throw new InvalidOperationException("A transaction is already active.");
            }

            var connection = CurrentDbConnection;
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            _efTransaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            // Configure ADO repository with transaction context
            AdoRepository.SetTransaction(CurrentDbTransaction!);
            AdoRepository.SetConnection(connection);

            if (_logger?.IsEnabled(LogLevel.Information) == true)
            {
                _logger.LogInformation("Transaction started with isolation level: {IsolationLevel}", isolationLevel);
            }

            return new EfTransaction(_efTransaction, AdoRepository, _logger, OnTransactionDisposed);
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var changes = await _context.SaveChangesAsync(cancellationToken);
            if (_logger?.IsEnabled(LogLevel.Debug) == true)
            {
                _logger.LogDebug("Saved {ChangeCount} changes to database", changes);
            }
            return changes;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger?.LogError(ex, "Concurrency conflict occurred while saving changes");
            throw new UnitOfWorkException("A concurrency conflict occurred while saving changes.", ex);
        }
        catch (DbUpdateException ex)
        {
            _logger?.LogError(ex, "Database update failed while saving changes");
            throw new UnitOfWorkException("Database update failed while saving changes.", ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error occurred while saving changes");
            throw new UnitOfWorkException("An unexpected error occurred while saving changes.", ex);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    private void OnTransactionDisposed() => _efTransaction = null;

    /// <summary>
    /// Releases managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _efTransaction?.Dispose();
            _transactionSemaphore.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Asynchronously releases unmanaged resources.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            if (_efTransaction is not null)
            {
                await _efTransaction.DisposeAsync();
            }

            _transactionSemaphore.Dispose();
            await _context.DisposeAsync();
            _disposed = true;
        }
    }

    private sealed class EfTransaction(
        IDbContextTransaction transaction,
        IAdoRepository adoRepository,
        ILogger<UnitOfWork<TContext>>? logger,
        Action onDisposed) : ITransaction
    {
        private readonly IDbContextTransaction _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        private readonly IAdoRepository _adoRepository = adoRepository ?? throw new ArgumentNullException(nameof(adoRepository));
        private readonly ILogger<UnitOfWork<TContext>>? _logger = logger;
        private readonly Action _onDisposed = onDisposed ?? throw new ArgumentNullException(nameof(onDisposed));
        private bool _disposed;

        public bool IsCompleted { get; private set; }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (IsCompleted)
            {
                throw new InvalidOperationException("Transaction has already been completed.");
            }

            try
            {
                await _transaction.CommitAsync(cancellationToken);
                IsCompleted = true;
                _logger?.LogInformation("Transaction committed successfully");
            }
            catch (Exception ex)
            {
                await RollbackAsync(cancellationToken);
                throw new UnitOfWorkException("Failed to commit transaction", ex);
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (IsCompleted)
            {
                return; // Already completed, nothing to rollback
            }

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
                IsCompleted = true;
                _logger?.LogWarning("Transaction rolled back");
            }
            catch (Exception ex)
            {
                throw new UnitOfWorkException("Failed to rollback transaction", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                try
                {
                    // Auto-rollback if not completed
                    if (!IsCompleted)
                    {
                        _logger?.LogWarning(
                            "Transaction disposed without explicit commit/rollback - performing automatic rollback");
                        await RollbackAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during transaction disposal");
                }
                finally
                {
                    _adoRepository.ClearTransaction();
                    await _transaction.DisposeAsync();
                    _onDisposed();
                    _disposed = true;
                }
            }
        }
    }
}
