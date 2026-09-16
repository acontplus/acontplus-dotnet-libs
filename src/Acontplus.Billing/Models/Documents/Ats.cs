namespace Acontplus.Billing.Models.Documents;

/// <summary>
/// Represents the header information of an ATS (Anexo Transaccional Simplificado) report.
/// </summary>
public record AtsHeader
{
    /// <summary>Gets or sets the informant identification type code.</summary>
    public string TipoIdInformante { get; init; } = string.Empty;

    /// <summary>Gets or sets the informant identification number (RUC).</summary>
    public string IdInformante { get; init; } = string.Empty;

    /// <summary>Gets or sets the informant business or legal name.</summary>
    public string RazonSocial { get; init; } = string.Empty;

    /// <summary>Gets or sets the reporting fiscal year.</summary>
    public string Anio { get; init; } = string.Empty;

    /// <summary>Gets or sets the reporting fiscal month.</summary>
    public string Mes { get; init; } = string.Empty;

    /// <summary>Gets or sets the establishment number registered with RUC.</summary>
    public string NumEstabRuc { get; init; } = string.Empty;

    /// <summary>Gets or sets the total sales amount reported.</summary>
    public string TotalVentas { get; init; } = string.Empty;

    /// <summary>Gets or sets the operational code.</summary>
    public string CodigoOperativo { get; init; } = string.Empty;
}

/// <summary>
/// Represents a purchase transaction in the ATS report.
/// </summary>
public record Purchase
{
    /// <summary>Gets or sets the tax credit support code.</summary>
    public string CodSustento { get; init; } = string.Empty;

    /// <summary>Gets or sets the supplier identification type code.</summary>
    public string TpIdProv { get; init; } = string.Empty;

    /// <summary>Gets or sets the supplier identification number.</summary>
    public string IdProv { get; init; } = string.Empty;

    /// <summary>Gets or sets the voucher type code.</summary>
    public string TipoComprobante { get; init; } = string.Empty;

    /// <summary>Gets or sets whether the supplier is a related party.</summary>
    public string ParteRel { get; init; } = string.Empty;

    /// <summary>Gets or sets the accounting registration date.</summary>
    public string FechaRegistro { get; init; } = string.Empty;

    /// <summary>Gets or sets the establishment code.</summary>
    public string Establecimiento { get; init; } = string.Empty;

    /// <summary>Gets or sets the emission point code.</summary>
    public string PuntoEmision { get; init; } = string.Empty;

    /// <summary>Gets or sets the sequential document number.</summary>
    public string Secuencial { get; init; } = string.Empty;

    /// <summary>Gets or sets the document issue date.</summary>
    public string FechaEmision { get; init; } = string.Empty;

    /// <summary>Gets or sets the authorization number.</summary>
    public string Autorizacion { get; init; } = string.Empty;

    /// <summary>Gets or sets the non-subject to VAT tax base.</summary>
    public string BaseNoGraIva { get; init; } = string.Empty;

    /// <summary>Gets or sets the 0% VAT tax base.</summary>
    public string BaseImponible { get; init; } = string.Empty;

    /// <summary>Gets or sets the taxed VAT tax base.</summary>
    public string BaseImpGrav { get; init; } = string.Empty;

    /// <summary>Gets or sets the exempt VAT tax base.</summary>
    public string BaseImpExe { get; init; } = string.Empty;

    /// <summary>Gets or sets the ICE tax amount.</summary>
    public string MontoIce { get; init; } = string.Empty;

    /// <summary>Gets or sets the VAT tax amount.</summary>
    public string MontoIva { get; init; } = string.Empty;

    /// <summary>Gets or sets the 10% goods withholding amount.</summary>
    public string ValRetBien10 { get; init; } = string.Empty;

    /// <summary>Gets or sets the 20% services withholding amount.</summary>
    public string ValRetServ20 { get; init; } = string.Empty;

    /// <summary>Gets or sets the goods withholding amount (30% or 70%).</summary>
    public string ValorRetBienes { get; init; } = string.Empty;

    /// <summary>Gets or sets the 50% services withholding amount.</summary>
    public string ValRetServ50 { get; init; } = string.Empty;

    /// <summary>Gets or sets the 100% services withholding amount.</summary>
    public string ValorRetServicios { get; init; } = string.Empty;

    /// <summary>Gets or sets the 100% withholding amount.</summary>
    public string ValRetServ100 { get; init; } = string.Empty;

    /// <summary>Gets or sets the total reimbursement tax bases.</summary>
    public string TotbasesImpReemb { get; init; } = string.Empty;

    /// <summary>Gets or sets local or foreign payment indicator.</summary>
    public string PagoLocExt { get; init; } = string.Empty;

    /// <summary>Gets or sets the tax regime type for foreign payments.</summary>
    public string TipoRegi { get; init; } = string.Empty;

    /// <summary>Gets or sets the fiscal regime description.</summary>
    public string DenopagoRegFis { get; init; } = string.Empty;

    /// <summary>Gets or sets the country code where payment was made.</summary>
    public string PaisEfecPago { get; init; } = string.Empty;

    /// <summary>Gets or sets whether double taxation treaty applies.</summary>
    public string AplicConvDobTrib { get; init; } = string.Empty;

    /// <summary>Gets or sets whether foreign payment is subject to legal withholding.</summary>
    public string PagExtSujRetNorLeg { get; init; } = string.Empty;

    /// <summary>Gets or sets the payment method code.</summary>
    public string FormaPago { get; init; } = string.Empty;

    /// <summary>Gets or sets the modified document type code, if applicable.</summary>
    public string? DocModificado { get; init; }

    /// <summary>Gets or sets the modified document establishment, if applicable.</summary>
    public string? EstabModificado { get; init; }

    /// <summary>Gets or sets the modified document emission point, if applicable.</summary>
    public string? PtoEmiModificado { get; init; }

    /// <summary>Gets or sets the modified document sequential number, if applicable.</summary>
    public string? SecModificado { get; init; }

    /// <summary>Gets or sets the modified document authorization number, if applicable.</summary>
    public string? AutModificado { get; init; }

    /// <summary>Gets or sets the withholding establishment code, if applicable.</summary>
    public string? EstabRetencion1 { get; init; }

    /// <summary>Gets or sets the withholding emission point, if applicable.</summary>
    public string? PtoEmiRetencion1 { get; init; }

    /// <summary>Gets or sets the withholding sequential number, if applicable.</summary>
    public string? SecRetencion1 { get; init; }

    /// <summary>Gets or sets the withholding authorization number, if applicable.</summary>
    public string? AutRetencion1 { get; init; }

    /// <summary>Gets or sets the withholding issue date, if applicable.</summary>
    public string? FechaEmiRet1 { get; init; }

    /// <summary>Gets or sets the document number used for linking with withholding taxes.</summary>
    public string NroDocumento { get; init; } = string.Empty; // Used for linking with withholding taxes
}

/// <summary>
/// Represents income tax withholding concept details in the ATS report.
/// </summary>
public record WithholdingTax
{
    /// <summary>Gets or sets the income tax withholding concept code (AIR).</summary>
    public string CodRetAir { get; init; } = string.Empty;

    /// <summary>Gets or sets the taxable base for income tax withholding.</summary>
    public string BaseImpAir { get; init; } = string.Empty;

    /// <summary>Gets or sets the withholding percentage applied.</summary>
    public string PorcentajeAir { get; init; } = string.Empty;

    /// <summary>Gets or sets the withheld tax amount.</summary>
    public string ValRetAir { get; init; } = string.Empty;

    /// <summary>Gets or sets the linked purchase document number.</summary>
    public string NroDocumento { get; init; } = string.Empty; // Used for linking

    /// <summary>Gets or sets the linked purchase authorization or access key.</summary>
    public string ClaveAcceso { get; init; } = string.Empty; // Used for linking (Autorizacion)
}

/// <summary>
/// Represents a sales transaction in the ATS report.
/// </summary>
public record Sale
{
    /// <summary>Gets or sets the customer identification type code.</summary>
    public string TpIdCliente { get; init; } = string.Empty;

    /// <summary>Gets or sets the customer identification number.</summary>
    public string IdCliente { get; init; } = string.Empty;

    /// <summary>Gets or sets whether the customer is a related party.</summary>
    public string? ParteRelVtas { get; init; }

    /// <summary>Gets or sets the customer classification type.</summary>
    public string? TipoCliente { get; init; }

    /// <summary>Gets or sets the customer legal or trade name.</summary>
    public string? DenoCli { get; init; }

    /// <summary>Gets or sets the sales voucher type code.</summary>
    public string TipoComprobante { get; init; } = string.Empty;

    /// <summary>Gets or sets the emission type code.</summary>
    public string TipoEmision { get; init; } = string.Empty;

    /// <summary>Gets or sets the number of vouchers issued.</summary>
    public string NumeroComprobantes { get; init; } = string.Empty;

    /// <summary>Gets or sets the non-subject to VAT tax base.</summary>
    public string BaseNoGraIva { get; init; } = string.Empty;

    /// <summary>Gets or sets the 0% VAT tax base.</summary>
    public string BaseImponible { get; init; } = string.Empty;

    /// <summary>Gets or sets the taxed VAT tax base.</summary>
    public string BaseImpGrav { get; init; } = string.Empty;

    /// <summary>Gets or sets the VAT tax amount.</summary>
    public string MontoIva { get; init; } = string.Empty;

    /// <summary>Gets or sets the compensation type code, if applicable.</summary>
    public string? TipoCompe { get; init; }

    /// <summary>Gets or sets the compensation amount, if applicable.</summary>
    public string? MontoCompensacion { get; init; }

    /// <summary>Gets or sets the ICE tax amount.</summary>
    public string MontoIce { get; init; } = string.Empty;

    /// <summary>Gets or sets the VAT withheld by customer.</summary>
    public string ValorRetIva { get; init; } = string.Empty;

    /// <summary>Gets or sets the income tax withheld by customer.</summary>
    public string ValorRetRenta { get; init; } = string.Empty;

    /// <summary>Gets or sets the main payment method code.</summary>
    public string FormaPago { get; init; } = string.Empty;
}

/// <summary>
/// Represents establishment sales totals in the ATS report.
/// </summary>
public record EstablishmentSale
{
    /// <summary>Gets or sets the establishment code.</summary>
    public string CodEstab { get; init; } = string.Empty;

    /// <summary>Gets or sets total sales reported by establishment.</summary>
    public string VentasEstab { get; init; } = string.Empty;

    /// <summary>Gets or sets compensation VAT amount for the establishment.</summary>
    public string IvaComp { get; init; } = string.Empty;
}

/// <summary>
/// Represents a canceled document in the ATS report.
/// </summary>
public record CanceledDocument
{
    /// <summary>Gets or sets the canceled document voucher type code.</summary>
    public string TipoComprobante { get; init; } = string.Empty;

    /// <summary>Gets or sets the establishment code of the canceled document.</summary>
    public string Establecimiento { get; init; } = string.Empty;

    /// <summary>Gets or sets the emission point of the canceled document.</summary>
    public string PuntoEmision { get; init; } = string.Empty;

    /// <summary>Gets or sets the starting sequential number of the canceled series.</summary>
    public string SecuencialInicio { get; init; } = string.Empty;

    /// <summary>Gets or sets the ending sequential number of the canceled series.</summary>
    public string SecuencialFin { get; init; } = string.Empty;

    /// <summary>Gets or sets the authorization number of the canceled document.</summary>
    public string Autorizacion { get; init; } = string.Empty;
}

/// <summary>
/// Aggregates all sections of an ATS (Anexo Transaccional Simplificado) document.
/// </summary>
public record AtsData
{
    /// <summary>Gets or sets the ATS report header data.</summary>
    public AtsHeader Header { get; init; } = new();

    /// <summary>Gets or sets the collection of reported purchases.</summary>
    public IEnumerable<Purchase> Purchases { get; init; } = Enumerable.Empty<Purchase>();

    /// <summary>Gets or sets the collection of reported sales.</summary>
    public IEnumerable<Sale> Sales { get; init; } = Enumerable.Empty<Sale>();

    /// <summary>Gets or sets the collection of reported withholding taxes.</summary>
    public IEnumerable<WithholdingTax> WithholdingTaxes { get; init; } = Enumerable.Empty<WithholdingTax>();

    /// <summary>Gets or sets the collection of sales grouped by establishment.</summary>
    public IEnumerable<EstablishmentSale> EstablishmentSales { get; init; } = Enumerable.Empty<EstablishmentSale>();

    /// <summary>Gets or sets the collection of canceled documents.</summary>
    public IEnumerable<CanceledDocument> CanceledDocuments { get; init; } = Enumerable.Empty<CanceledDocument>();
}
