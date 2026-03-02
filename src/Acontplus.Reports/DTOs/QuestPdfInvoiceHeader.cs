namespace Acontplus.Reports.Dtos;

/// <summary>
/// First-class SRI Ecuador invoice / voucher header block for QuestPDF rendering.
/// Produces the standard two-panel layout used by all SRI electronic documents:
/// left panel (logo + company info) and right panel (bordered SRI authorization box),
/// followed by a buyer information row.
/// </summary>
public class QuestPdfInvoiceHeader
{
    // ── Logo ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// In-memory logo bytes (PNG/JPEG). Takes priority over <see cref="LogoPath"/>.
    /// Equivalent to the RDLC <c>Parameters!logo.Value</c> (byte[]) source.
    /// </summary>
    public byte[]? LogoBytes { get; set; }

    /// <summary>Filesystem path to the logo image file. Used when <see cref="LogoBytes"/> is null.</summary>
    public string? LogoPath { get; set; }

    /// <summary>Maximum logo height in points (default: 50)</summary>
    public float LogoMaxHeight { get; set; } = 50f;

    // ── Company info (left panel) ─────────────────────────────────────────────

    /// <summary>Legal company name (Razón Social) — rendered bold</summary>
    public string? CompanyName { get; set; }

    /// <summary>Trade / commercial name (Nombre Comercial)</summary>
    public string? TradeName { get; set; }

    /// <summary>Main establishment address (Dirección Matriz)</summary>
    public string? CompanyAddress { get; set; }

    /// <summary>Branch / establishment address (Dirección Sucursal) — shown when non-empty</summary>
    public string? BranchAddress { get; set; }

    /// <summary>Phone number(s)</summary>
    public string? CompanyPhone { get; set; }

    /// <summary>Email address of the entity</summary>
    public string? CompanyEmail { get; set; }

    /// <summary>Economic activity description</summary>
    public string? CompanyActivity { get; set; }

    /// <summary>Contribuyente Especial number — shown when non-empty</summary>
    public string? ContribuyenteEspecial { get; set; }

    /// <summary>Obligado a llevar Contabilidad: "SÍ" / "NO"</summary>
    public string? ObligadoContabilidad { get; set; }

    /// <summary>Contribuyente Régimen RIMPE — shown when non-empty</summary>
    public string? ContribuyenteRimpe { get; set; }

    /// <summary>Agente de Retención (resolution number) — shown when non-empty</summary>
    public string? AgenteRetencion { get; set; }

    // ── SRI authorization box (right panel) ───────────────────────────────────

    /// <summary>RUC of the emitter</summary>
    public string? Ruc { get; set; }

    /// <summary>Document type label, e.g. "FACTURA", "NOTA DE CRÉDITO", "RETENCIÓN"</summary>
    public string? DocumentType { get; set; }

    /// <summary>Sequential document number, e.g. "001-001-000000001"</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>SRI authorization number (Número de Autorización)</summary>
    public string? AuthorizationNumber { get; set; }

    /// <summary>Authorization date/time (Fecha y Hora de Autorización)</summary>
    public string? AuthorizationDate { get; set; }

    /// <summary>
    /// 49-character SRI access key (Clave de Acceso).
    /// Rendered as small text; associated barcode is a separate <see cref="QuestPdfSection"/>
    /// with <see cref="QuestPdfSectionType.Barcode"/>.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>Processing environment: "PRODUCCIÓN" or "PRUEBAS"</summary>
    public string? Environment { get; set; }

    /// <summary>Emission type: "EMISIÓN NORMAL" (default)</summary>
    public string? EmissionType { get; set; } = "EMISIÓN NORMAL";

    // ── Buyer block (below both panels) ──────────────────────────────────────

    /// <summary>Buyer's legal name (Razón Social / Nombres Completos del Comprador)</summary>
    public string? BuyerName { get; set; }

    /// <summary>Buyer's tax/ID number (Identificación)</summary>
    public string? BuyerIdentification { get; set; }

    /// <summary>Buyer's address</summary>
    public string? BuyerAddress { get; set; }

    /// <summary>Document emission date</summary>
    public string? EmissionDate { get; set; }

    /// <summary>Referenced delivery note (Guía de Remisión) — shown when non-empty</summary>
    public string? DeliveryReference { get; set; }

    /// <summary>
    /// Additional key-value pairs appended to the buyer block.
    /// Useful for custom fields (e.g. order number, payment terms).
    /// </summary>
    public Dictionary<string, string> ExtraFields { get; set; } = [];

    // ── Layout ────────────────────────────────────────────────────────────────

    /// <summary>Relative width of the company info (left) panel (default: 6)</summary>
    public float LeftPanelRatio { get; set; } = 6f;

    /// <summary>Relative width of the SRI authorization (right) panel (default: 4)</summary>
    public float RightPanelRatio { get; set; } = 4f;

    /// <summary>Border color of the SRI authorization box (default: "#252525")</summary>
    public string AuthBoxBorderColor { get; set; } = "#252525";

    /// <summary>Font size used throughout the invoice header (default: 8)</summary>
    public float FontSize { get; set; } = 8f;
}
