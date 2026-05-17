namespace Acontplus.Core.Domain.Enums;

/// <summary>
/// Electronic document types defined by the Ecuadorian SRI (Servicio de Rentas Internas).
/// Integer values correspond to the official SRI document type codes.
/// </summary>
public enum SriDocument
{
    /// <summary>Factura electrónica (code 01).</summary>
    [Description("Factura")] Factura = 1,
    /// <summary>Liquidación de compra de bienes y prestación de servicios (code 03).</summary>
    [Description("Liquidación de Compra")] Liquidacion = 3,
    /// <summary>Nota de crédito (code 04).</summary>
    [Description("Nota de Crédito")] NotaCredito = 4,
    /// <summary>Nota de débito (code 05).</summary>
    [Description("Nota de Débito")] NotaDebito = 5,
    /// <summary>Guía de remisión (code 06).</summary>
    [Description("Guía de Remisión")] GuiaRemision = 6,
    /// <summary>Comprobante de retención (code 07).</summary>
    [Description("Retención")] Retencion = 7
}
