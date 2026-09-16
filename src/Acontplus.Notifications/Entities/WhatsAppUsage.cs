namespace Acontplus.Notifications.Entities;

/// <summary>
/// Tracks monthly or billing-period WhatsApp message usage and quotas per company.
/// </summary>
public class WhatsAppUsage : BaseEntity
{
    /// <summary>
    /// Gets or sets the company identifier.
    /// </summary>
    public int CompanyId { get; set; }

    /// <summary>
    /// Gets or sets the count of messages dispatched in the period.
    /// </summary>
    public int Used { get; set; }

    /// <summary>
    /// Gets or sets the message quota limit for the period.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets whether message sending is unlimited for the company.
    /// </summary>
    public bool Unlimited { get; set; }

    /// <summary>
    /// Gets or sets the billing period start timestamp.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the optional billing period end timestamp.
    /// </summary>
    public DateTime? EndDate { get; set; }
}
