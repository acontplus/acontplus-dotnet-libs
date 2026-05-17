namespace Acontplus.Core.Domain.Enums;

/// <summary>Represents the type of device that originated a request.</summary>
public enum DeviceType
{
    /// <summary>Device type could not be determined.</summary>
    Unknown,
    /// <summary>Smartphone or feature phone.</summary>
    Mobile,
    /// <summary>Tablet device.</summary>
    Tablet,
    /// <summary>Desktop or laptop computer.</summary>
    Desktop,
    /// <summary>Browser-based web application or web-view.</summary>
    Web
}
