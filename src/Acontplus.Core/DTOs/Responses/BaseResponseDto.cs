namespace Acontplus.Core.Dtos.Responses;

/// <summary>
/// Base DTO that mirrors the common audit fields of <see cref="Acontplus.Core.Domain.Common.Entities.BaseEntity"/>.
/// Extend this class in response DTOs that need to surface audit information to clients.
/// </summary>
public class BaseResponseDto
{
    /// <summary>Primary key of the entity.</summary>
    public int? Id { get; set; }

    /// <summary>UTC timestamp of when the entity was created.</summary>
    public DateTime? CreatedDate { get; set; }

    /// <summary>ID of the user who created the entity.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>UTC timestamp of the last update.</summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>ID of the user who last updated the entity.</summary>
    public int? UpdatedByUserId { get; set; }

    /// <summary>Whether the entity is currently active.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Whether the entity has been soft-deleted.</summary>
    public bool? IsDeleted { get; set; }
}
