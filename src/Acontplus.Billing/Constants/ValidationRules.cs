namespace Acontplus.Billing.Constants;

/// <summary>
/// Validation rules and constraint constants for SRI electronic document validation.
/// </summary>
public static class ValidationRules
{
    /// <summary>
    /// Expected length of an Ecuadorian RUC identifier.
    /// </summary>
    public const int RucLength = 13;

    /// <summary>
    /// Expected length of an Ecuadorian cédula identifier.
    /// </summary>
    public const int CedulaLength = 10;

    /// <summary>
    /// Expected length of the SRI 49-digit access key (clave de acceso).
    /// </summary>
    public const int AccessKeyLength = 49;
}