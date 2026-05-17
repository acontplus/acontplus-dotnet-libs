using System.ComponentModel.DataAnnotations;

namespace Acontplus.Core.Domain.Common.Entities;

/// <summary>
/// Base entity class that provides common functionality for all domain entities including audit fields and soft delete support.
/// </summary>
public abstract class BaseEntity : Entity<int>, IAuditableEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the user who created the entity.
    /// </summary>
    public int? CreatedByUserId { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the user who last updated the entity.
    /// </summary>
    public int? UpdatedByUserId { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the entity is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been soft deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the date and time when the entity was deleted.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the ID of the user who deleted the entity.
    /// </summary>
    public int? DeletedByUserId { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the request originated from a mobile device.
    /// </summary>
    public bool IsMobileRequest { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who created the entity.
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who last updated the entity.
    /// </summary>
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who deleted the entity.
    /// </summary>
    [MaxLength(100)]
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class.
    /// </summary>
    protected BaseEntity()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class with the specified creator information.
    /// </summary>
    /// <param name="createdByUserId">The ID of the user creating the entity.</param>
    /// <param name="isMobileRequest">Indicates whether the request originated from a mobile device.</param>
    protected BaseEntity(int createdByUserId, bool isMobileRequest = false)
    {
        CreatedAt = DateTime.UtcNow;
        CreatedByUserId = createdByUserId;
        IsMobileRequest = isMobileRequest;
    }

    /// <summary>
    /// Marks the entity as deleted (soft delete) and raises the appropriate domain event.
    /// </summary>
    /// <param name="deletedByUserId">The ID of the user deleting the entity.</param>
    public void MarkAsDeleted(int? deletedByUserId = default)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAt = DateTime.UtcNow;
        IsActive = false;

        AddDomainEvent(new EntityDeletedEvent(
            Id,
            GetType().Name,
            deletedByUserId
        ));
    }

    /// <summary>
    /// Restores the entity from deleted state and raises the appropriate domain event.
    /// </summary>
    public void RestoreFromDeleted()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = default;

        AddDomainEvent(new EntityRestoredEvent(
            Id,
            GetType().Name,
            UpdatedByUserId
        ));
    }

    /// <summary>
    /// Deactivates the entity if it is currently active.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the entity if it is currently inactive.
    /// </summary>
    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the audit fields with the current timestamp and user information, and raises a domain event.
    /// </summary>
    /// <param name="updatedByUserId">The ID of the user updating the entity.</param>
    public void UpdateAuditFields(int? updatedByUserId)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;

        AddDomainEvent(new EntityModifiedEvent(
            Id,
            GetType().Name,
            updatedByUserId
        ));
    }

    /// <summary>
    /// Creates a new instance of the specified entity type with the given parameters.
    /// </summary>
    /// <typeparam name="T">The type of entity to create.</typeparam>
    /// <param name="id">The entity ID.</param>
    /// <param name="createdByUserId">The ID of the user creating the entity.</param>
    /// <param name="isMobileRequest">Indicates whether the request originated from a mobile device.</param>
    /// <returns>A new instance of the specified entity type.</returns>
    protected static T Create<T>(int id, int createdByUserId, bool isMobileRequest = false)
        where T : BaseEntity, new()
    {
        return new T
        {
            Id = id,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            IsMobileRequest = isMobileRequest
        };
    }
}
