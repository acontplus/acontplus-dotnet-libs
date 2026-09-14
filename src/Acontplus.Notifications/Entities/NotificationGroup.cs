namespace Acontplus.Notifications.Entities;

/// <summary>
/// Represents a notification recipient group spanning one or multiple companies.
/// </summary>
public class NotificationGroup : BaseEntity
{
    /// <summary>
    /// Gets or sets the group name (e.g. "Finance Team", "Company-Wide").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the optional company identifier.
    /// </summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// Gets or sets user associations for this group.
    /// </summary>
    public ICollection<UserGroup>? UserGroups { get; set; }
}