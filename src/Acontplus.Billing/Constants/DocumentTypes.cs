namespace Acontplus.Billing.Constants;

/// <summary>
/// SRI Electronic Document Type Codes (Ecuador)
/// Based on SRI Ficha Técnica v2.32
/// </summary>
public static class DocumentTypes
{
    /// <summary>
    /// Invoice - Factura (Code: 01)
    /// </summary>
    public const string Factura = "01";

    /// <summary>
    /// Purchase Settlement - Liquidación de Compra de Bienes y Prestación de Servicios (Code: 03)
    /// </summary>
    public const string LiquidacionCompra = "03";

    /// <summary>
    /// Credit Note - Nota de Crédito (Code: 04)
    /// </summary>
    public const string NotaCredito = "04";

    /// <summary>
    /// Debit Note - Nota de Débito (Code: 05)
    /// </summary>
    public const string NotaDebito = "05";

    /// <summary>
    /// Delivery Guide - Guía de Remisión (Code: 06)
    /// </summary>
    public const string GuiaRemision = "06";

    /// <summary>
    /// Withholding Receipt - Comprobante de Retención (Code: 07)
    /// </summary>
    public const string ComprobanteRetencion = "07";

    /// <summary>
    /// Gets the document type name in Spanish
    /// </summary>
    public static string GetDocumentName(string codDoc) => codDoc switch
    {
        Factura => "Factura",
        LiquidacionCompra => "Liquidación de Compra",
        NotaCredito => "Nota de Crédito",
        NotaDebito => "Nota de Débito",
        GuiaRemision => "Guía de Remisión",
        ComprobanteRetencion => "Comprobante de Retención",
        _ => "Documento Desconocido"
    };

    /// <summary>
    /// Validates if a document code is valid according to SRI normatives
    /// </summary>
    public static bool IsValidDocumentCode(string codDoc) => codDoc is
        Factura or LiquidacionCompra or NotaCredito or
        NotaDebito or GuiaRemision or ComprobanteRetencion;

    /// <summary>
    /// Gets all valid document codes
    /// </summary>
    public static string[] GetAllDocumentCodes() =>
    [
        Factura,
        LiquidacionCompra,
        NotaCredito,
        NotaDebito,
        GuiaRemision,
        ComprobanteRetencion
    ];
}
