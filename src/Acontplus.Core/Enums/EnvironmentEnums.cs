namespace Acontplus.Core.Enums;

/// <summary>Deployment environment identifiers.</summary>
public enum EnvironmentEnums
{
    /// <summary>Local developer workstation.</summary>
    Development,
    /// <summary>Automated integration-test environment.</summary>
    IntegrationTests,
    /// <summary>Quality-assurance environment.</summary>
    QA,
    /// <summary>Pre-production / staging environment.</summary>
    Staging,
    /// <summary>Live production environment.</summary>
    Production
}
