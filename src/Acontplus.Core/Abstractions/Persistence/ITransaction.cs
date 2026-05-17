namespace Acontplus.Core.Abstractions.Persistence;

/// <summary>
/// Represents a database transaction that can be committed or rolled back.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction asynchronously, persisting all changes made within the transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous commit operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back the transaction asynchronously, discarding all changes made within the transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a value indicating whether the transaction has been completed (either committed or rolled back).
    /// </summary>
    bool IsCompleted { get; }
}
