namespace Acontplus.Core.Domain.Common.Entities;

/// <summary>
/// Defines auditable properties for tracking entity creation, modification, and deletion.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is soft deleted.
    /// </summary>
    bool IsDeleted { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the entity is active.
    /// </summary>
    bool IsActive { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the date and time when the entity was deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the request originated from a mobile device.
    /// </summary>
    bool IsMobileRequest { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    string? CreatedBy { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity.
    /// </summary>
    string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Gets or sets the identifier of the user who deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }
}
