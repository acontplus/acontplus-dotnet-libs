namespace Acontplus.Billing.Models.Documents;

/// <summary>
/// Represents a normalized electronic document processed through the SRI system.
/// </summary>
public class ComprobanteElectronico
{
    /// <summary>Gets or sets the document schema version.</summary>
    public string VersionComp { get; set; } = string.Empty;

    /// <summary>Gets or sets the document type code.</summary>
    public string CodDoc { get; set; } = string.Empty;

    /// <summary>Gets or sets the SRI authorization date.</summary>
    public string FechaAutorizacion { get; set; } = string.Empty;

    /// <summary>Gets or sets the SRI authorization number.</summary>
    public string NumeroAutorizacion { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax information section.</summary>
    public InfoTributaria InfoTributaria { get; set; } = new();

    /// <summary>Gets or sets the invoice information section, if applicable.</summary>
    public InfoFactura? InfoFactura { get; set; }

    /// <summary>Gets or sets the purchase settlement information section, if applicable.</summary>
    public InfoLiquidacionCompra? InfoLiquidacionCompra { get; set; }

    /// <summary>Gets or sets the credit note information section, if applicable.</summary>
    public InfoNotaCredito? InfoNotaCredito { get; set; }

    /// <summary>Gets or sets the debit note information section, if applicable.</summary>
    public InfoNotaDebito? InfoNotaDebito { get; set; }

    /// <summary>Gets or sets the consignment note information section, if applicable.</summary>
    public InfoGuiaRemision? InfoGuiaRemision { get; set; }

    /// <summary>Gets or sets the withholding voucher information section, if applicable.</summary>
    public InfoCompRetencion? InfoCompRetencion { get; set; }

    /// <summary>Gets or sets the collection of line items.</summary>
    public List<Detalle>? Detalles { get; set; }

    /// <summary>Gets or sets the collection of taxes.</summary>
    public List<Impuesto>? Impuestos { get; set; }

    /// <summary>Gets or sets the collection of withholding taxes.</summary>
    public List<ImpuestoRetencion>? ImpuestosRetencion { get; set; }

    /// <summary>Gets or sets the collection of supporting documents.</summary>
    public List<DocSustento>? DocSustentos { get; set; }

    /// <summary>Gets or sets the collection of recipients for consignment notes.</summary>
    public List<Destinatario>? Destinatarios { get; set; }

    /// <summary>Gets or sets the collection of additional informational fields.</summary>
    public List<InfoAdicional>? InfoAdicional { get; set; }

    /// <summary>
    /// Assigns the specific document info section based on the document type code.
    /// </summary>
    /// <param name="codDoc">The SRI document type code.</param>
    /// <param name="obj">The document information instance.</param>
    public void CreateInfoComp(string codDoc, object? obj)
    {
        switch (codDoc)
        {
            case "01":
                InfoFactura = obj as InfoFactura;
                break;
            case "03":
                InfoLiquidacionCompra = obj as InfoLiquidacionCompra;
                break;
            case "04":
                InfoNotaCredito = obj as InfoNotaCredito;
                break;
            case "05":
                InfoNotaDebito = obj as InfoNotaDebito;
                break;
            case "06":
                InfoGuiaRemision = obj as InfoGuiaRemision;
                break;
            case "07":
                InfoCompRetencion = obj as InfoCompRetencion;
                break;
        }
    }

    /// <summary>Sets the line items list.</summary>
    /// <param name="obj">The line items list.</param>
    public void CreateDetails(object? obj) => Detalles = obj as List<Detalle>;

    /// <summary>Sets the taxes list.</summary>
    /// <param name="obj">The taxes list.</param>
    public void CreateTaxes(object? obj) => Impuestos = obj as List<Impuesto>;

    /// <summary>Sets the withholding taxes list.</summary>
    /// <param name="obj">The withholding taxes list.</param>
    public void CreateRetencionTaxes(object? obj) => ImpuestosRetencion = obj as List<ImpuestoRetencion>;

    /// <summary>Sets the supporting documents list.</summary>
    /// <param name="obj">The supporting documents list.</param>
    public void CreateDocSustentos(object? obj) => DocSustentos = obj as List<DocSustento>;

    /// <summary>Sets the additional info fields list.</summary>
    /// <param name="obj">The additional info fields list.</param>
    public void CreateAdditionalInfo(object? obj) => InfoAdicional = obj as List<InfoAdicional>;

    /// <summary>Sets the recipients list.</summary>
    /// <param name="obj">The recipients list.</param>
    public void CreateDestinatarios(object? obj) => Destinatarios = obj as List<Destinatario>;
}

/// <summary>
/// Represents tax identification and general authorization data in an SRI document.
/// </summary>
public class InfoTributaria
{
    /// <summary>Gets or sets the SRI environment code (1: Test, 2: Production).</summary>
    public string Ambiente { get; set; } = string.Empty;

    /// <summary>Gets or sets the emission type code (1: Normal).</summary>
    public string TipoEmision { get; set; } = string.Empty;

    /// <summary>Gets or sets the issuer legal or business name.</summary>
    public string RazonSocial { get; set; } = string.Empty;

    /// <summary>Gets or sets the issuer commercial or trade name.</summary>
    public string NombreComercial { get; set; } = string.Empty;

    /// <summary>Gets or sets the issuer tax ID (RUC).</summary>
    public string Ruc { get; set; } = string.Empty;

    /// <summary>Gets or sets the 49-digit authorization access key.</summary>
    public string ClaveAcceso { get; set; } = string.Empty;

    /// <summary>Gets or sets the electronic document type code.</summary>
    public string CodDoc { get; set; } = string.Empty;

    /// <summary>Gets or sets the establishment code (3 digits).</summary>
    public string Estab { get; set; } = string.Empty;

    /// <summary>Gets or sets the emission point code (3 digits).</summary>
    public string PtoEmi { get; set; } = string.Empty;

    /// <summary>Gets or sets the sequential document number (9 digits).</summary>
    public string Secuencial { get; set; } = string.Empty;

    /// <summary>Gets or sets the head office address.</summary>
    public string DirMatriz { get; set; } = string.Empty;
}

/// <summary>
/// Represents invoice header information in an SRI electronic invoice.
/// </summary>
public class InfoFactura
{
    /// <summary>Gets or sets the document issuance date (dd/MM/yyyy).</summary>
    public string FechaEmision { get; set; } = string.Empty;

    /// <summary>Gets or sets the branch establishment address.</summary>
    public string DirEstablecimiento { get; set; } = string.Empty;

    /// <summary>Gets or sets the special taxpayer resolution number, if applicable.</summary>
    public string ContribuyenteEspecial { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the issuer is required to keep accounting records (SI/NO).</summary>
    public string ObligadoContabilidad { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer identification type code.</summary>
    public string TipoIdentificacionComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer legal or business name.</summary>
    public string RazonSocialComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer identification number.</summary>
    public string IdentificacionComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer address.</summary>
    public string DireccionComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the associated consignment note number, if applicable.</summary>
    public string GuiaRemision { get; set; } = string.Empty;

    /// <summary>Gets or sets the total amount before taxes.</summary>
    public string TotalSinImpuestos { get; set; } = string.Empty;

    /// <summary>Gets or sets the total discount amount.</summary>
    public string TotalDescuento { get; set; } = string.Empty;

    /// <summary>Gets or sets the tip amount.</summary>
    public string Propina { get; set; } = string.Empty;

    /// <summary>Gets or sets the total payable amount including taxes.</summary>
    public string ImporteTotal { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency code (e.g. DOLAR).</summary>
    public string Moneda { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of tax totals.</summary>
    public List<TotalImpuesto>? TotalImpuestos { get; set; }

    /// <summary>Gets or sets the list of payment methods.</summary>
    public List<Pago>? Pagos { get; set; }

    /// <summary>Sets the tax totals list.</summary>
    /// <param name="obj">The tax totals list.</param>
    public void CreateTotalTaxes(object? obj) => TotalImpuestos = obj as List<TotalImpuesto>;

    /// <summary>Sets the payment methods list.</summary>
    /// <param name="obj">The payment methods list.</param>
    public void CreatePayments(object? obj) => Pagos = obj as List<Pago>;
}

/// <summary>
/// Represents aggregated tax total by tax type and rate code.
/// </summary>
public class TotalImpuesto
{
    /// <summary>Gets or sets the tax code (e.g., 2 for VAT, 3 for ICE).</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax percentage rate code.</summary>
    public string CodigoPorcentaje { get; set; } = string.Empty;

    /// <summary>Gets or sets additional discount applied to the tax base.</summary>
    public string DescuentoAdicional { get; set; } = string.Empty;

    /// <summary>Gets or sets the taxable base amount.</summary>
    public string BaseImponible { get; set; } = string.Empty;

    /// <summary>Gets or sets the calculated tax amount.</summary>
    public string Valor { get; set; } = string.Empty;
}

/// <summary>
/// Represents a payment method and term applied to the document.
/// </summary>
public class Pago
{
    /// <summary>Gets or sets the SRI payment method code.</summary>
    public string FormaPago { get; set; } = string.Empty;

    /// <summary>Gets or sets the total payment amount.</summary>
    public string Total { get; set; } = string.Empty;

    /// <summary>Gets or sets the payment term duration.</summary>
    public string Plazo { get; set; } = string.Empty;

    /// <summary>Gets or sets the time unit for the payment term (e.g., dias, meses).</summary>
    public string UnidadTiempo { get; set; } = string.Empty;
}

/// <summary>
/// Represents a line item detail in an electronic document.
/// </summary>
public class Detalle
{
    /// <summary>Gets or sets the line item identifier.</summary>
    public int IdDetalle { get; set; }

    /// <summary>Gets or sets the principal product code.</summary>
    public string CodigoPrincipal { get; set; } = string.Empty;

    /// <summary>Gets or sets the auxiliary product code.</summary>
    public string CodigoAuxiliar { get; set; } = string.Empty;

    /// <summary>Gets or sets the line item description.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Gets or sets the item quantity.</summary>
    public string Cantidad { get; set; } = string.Empty;

    /// <summary>Gets or sets the unit price before taxes.</summary>
    public string PrecioUnitario { get; set; } = string.Empty;

    /// <summary>Gets or sets the line item discount amount.</summary>
    public string Descuento { get; set; } = string.Empty;

    /// <summary>Gets or sets the total line price before taxes.</summary>
    public string PrecioTotalSinImpuesto { get; set; } = string.Empty;

    /// <summary>Gets or sets the additional detail attribute name.</summary>
    public string DetAdicionalNombre { get; set; } = string.Empty;

    /// <summary>Gets or sets the additional detail attribute value.</summary>
    public string DetAdicionalValor { get; set; } = string.Empty;

    /// <summary>Gets or sets serialized additional details.</summary>
    public string DetallesAdicionales { get; set; } = string.Empty;

    /// <summary>Gets or sets serialized taxes for the line item.</summary>
    public string Impuestos { get; set; } = string.Empty;
}

/// <summary>
/// Represents database-mapped invoice detail line information.
/// </summary>
public class DetalleFactura
{
    /// <summary>Gets or sets the supplier identifier.</summary>
    public int IdProveedor { get; set; }

    /// <summary>Gets or sets the product or article identifier.</summary>
    public int IdArticulo { get; set; }

    /// <summary>Gets or sets the unit of measure identifier.</summary>
    public int IdMedida { get; set; }

    /// <summary>Gets or sets the principal product code.</summary>
    public string CodigoPrincipal { get; set; } = string.Empty;

    /// <summary>Gets or sets the auxiliary product code.</summary>
    public string CodigoAuxiliar { get; set; } = string.Empty;

    /// <summary>Gets or sets the product description.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Gets or sets the product quantity.</summary>
    public string Cantidad { get; set; } = string.Empty;

    /// <summary>Gets or sets the unit price.</summary>
    public string PrecioUnitario { get; set; } = string.Empty;

    /// <summary>Gets or sets the item discount amount.</summary>
    public string Descuento { get; set; } = string.Empty;

    /// <summary>Gets or sets the line item total before tax.</summary>
    public string PrecioTotalSinImpuesto { get; set; } = string.Empty;

    /// <summary>Gets or sets the additional detail attribute name.</summary>
    public string DetAdicionalNombre { get; set; } = string.Empty;

    /// <summary>Gets or sets the additional detail attribute value.</summary>
    public string DetAdicionalValor { get; set; } = string.Empty;
}

/// <summary>
/// Represents a tax line item associated with a product detail.
/// </summary>
public class Impuesto
{
    /// <summary>Gets or sets the associated detail line identifier.</summary>
    public int IdDetalle { get; set; }

    /// <summary>Gets or sets the associated product code.</summary>
    public string CodArticulo { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax type code.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax rate percentage code.</summary>
    public string CodigoPorcentaje { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax percentage rate.</summary>
    public string Tarifa { get; set; } = string.Empty;

    /// <summary>Gets or sets the taxable base amount.</summary>
    public string BaseImponible { get; set; } = string.Empty;

    /// <summary>Gets or sets the calculated tax amount.</summary>
    public string Valor { get; set; } = string.Empty;
}

/// <summary>
/// Represents credit note header information in an SRI electronic credit note.
/// </summary>
public class InfoNotaCredito
{
    /// <summary>Gets or sets the issuance date.</summary>
    public string FechaEmision { get; set; } = string.Empty;

    /// <summary>Gets or sets the branch establishment address.</summary>
    public string DirEstablecimiento { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer identification type code.</summary>
    public string TipoIdentificacionComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer legal or business name.</summary>
    public string RazonSocialComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer identification number.</summary>
    public string IdentificacionComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the special taxpayer resolution number.</summary>
    public string ContribuyenteEspecial { get; set; } = string.Empty;

    /// <summary>Gets or sets whether required to keep accounting records.</summary>
    public string ObligadoContabilidad { get; set; } = string.Empty;

    /// <summary>Gets or sets RISE regime indicator.</summary>
    public string Rise { get; set; } = string.Empty;

    /// <summary>Gets or sets modified document type code.</summary>
    public string CodDocModificado { get; set; } = string.Empty;

    /// <summary>Gets or sets modified document number.</summary>
    public string NumDocModificado { get; set; } = string.Empty;

    /// <summary>Gets or sets modified document issuance date.</summary>
    public string FechaEmisionDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets the total amount before taxes.</summary>
    public string TotalSinImpuestos { get; set; } = string.Empty;

    /// <summary>Gets or sets the total modification amount.</summary>
    public string ValorModificacion { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency code.</summary>
    public string Moneda { get; set; } = string.Empty;

    /// <summary>Gets or sets the reason for the credit note.</summary>
    public string Motivo { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of tax totals.</summary>
    public List<TotalImpuesto>? TotalImpuestos { get; set; }

    /// <summary>Sets the total taxes list.</summary>
    /// <param name="obj">The total taxes list.</param>
    public void CreateTotalTaxes(object? obj) => TotalImpuestos = obj as List<TotalImpuesto>;
}

/// <summary>
/// Represents withholding voucher header information in an SRI electronic withholding receipt.
/// </summary>
public class InfoCompRetencion
{
    /// <summary>Gets or sets the issuance date.</summary>
    public string FechaEmision { get; set; } = string.Empty;

    /// <summary>Gets or sets the branch establishment address.</summary>
    public string DirEstablecimiento { get; set; } = string.Empty;

    /// <summary>Gets or sets the special taxpayer resolution number.</summary>
    public string ContribuyenteEspecial { get; set; } = string.Empty;

    /// <summary>Gets or sets whether required to keep accounting records.</summary>
    public string ObligadoContabilidad { get; set; } = string.Empty;

    /// <summary>Gets or sets the withheld subject identification type code.</summary>
    public string TipoIdentificacionSujetoRetenido { get; set; } = string.Empty;

    /// <summary>Gets or sets the withheld subject legal or business name.</summary>
    public string RazonSocialSujetoRetenido { get; set; } = string.Empty;

    /// <summary>Gets or sets the withheld subject identification number.</summary>
    public string IdentificacionSujetoRetenido { get; set; } = string.Empty;

    /// <summary>Gets or sets the fiscal period (MM/yyyy).</summary>
    public string PeriodoFiscal { get; set; } = string.Empty;

    /// <summary>Gets or sets the ATS withheld subject type code.</summary>
    public string TipoSujetoRetenido { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the withheld subject is a related party.</summary>
    public string ParteRel { get; set; } = string.Empty;
}

/// <summary>
/// Represents a withholding tax entry applied to a transaction.
/// </summary>
public class ImpuestoRetencion
{
    /// <summary>Gets or sets the tax code.</summary>
    public string? Codigo { get; set; }

    /// <summary>Gets or sets the withholding tax code.</summary>
    public string CodigoRetencion { get; set; } = string.Empty;

    /// <summary>Gets or sets the taxable base amount.</summary>
    public string BaseImponible { get; set; } = string.Empty;

    /// <summary>Gets or sets the withholding percentage rate.</summary>
    public string PorcentajeRetener { get; set; } = string.Empty;

    /// <summary>Gets or sets the withheld tax amount.</summary>
    public string ValorRetenido { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting document type code.</summary>
    public string CodDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting document number.</summary>
    public string NumDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting document issuance date.</summary>
    public string FechaEmisionDocSustento { get; set; } = string.Empty;
}

/// <summary>
/// Represents supporting document information and associated taxes/reimbursements.
/// </summary>
public class DocSustento
{
    /// <summary>Gets or sets the tax credit support code.</summary>
    public string CodSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting document type code.</summary>
    public string CodDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting document number.</summary>
    public string NumDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting document issuance date.</summary>
    public string FechaEmisionDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets the accounting registration date.</summary>
    public string FechaRegistroContable { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting document authorization number.</summary>
    public string NumAutDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets local or foreign payment indicator.</summary>
    public string PagoLocExt { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax regime type.</summary>
    public string TipoRegi { get; set; } = string.Empty;

    /// <summary>Gets or sets the country code of payment.</summary>
    public string PaisEfecPago { get; set; } = string.Empty;

    /// <summary>Gets or sets double taxation treaty application.</summary>
    public string AplicConvDobTrib { get; set; } = string.Empty;

    /// <summary>Gets or sets foreign payment withholding applicability.</summary>
    public string PagExtSujRetNorLeg { get; set; } = string.Empty;

    /// <summary>Gets or sets payment fiscal regime indicator.</summary>
    public string PagoRegFis { get; set; } = string.Empty;

    /// <summary>Gets or sets total count of reimbursement documents.</summary>
    public string TotalComprobantesReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets total taxable base for reimbursement.</summary>
    public string TotalBaseImponibleReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets total tax amount for reimbursement.</summary>
    public string TotalImpuestoReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets total amount before taxes.</summary>
    public string TotalSinImpuestos { get; set; } = string.Empty;

    /// <summary>Gets or sets total amount.</summary>
    public string ImporteTotal { get; set; } = string.Empty;

    /// <summary>Gets or sets supporting document taxes list.</summary>
    public List<ImpuestoDocSustento>? Impuestos { get; set; }

    /// <summary>Gets or sets supporting document withholdings list.</summary>
    public List<Retencion>? Retenciones { get; set; }

    /// <summary>Gets or sets supporting document reimbursements list.</summary>
    public List<ReembolsoDetalle>? Reembolsos { get; set; }

    /// <summary>Gets or sets supporting document payments list.</summary>
    public List<Pago>? Pagos { get; set; }

    /// <summary>Sets the taxes list.</summary>
    /// <param name="obj">The taxes list.</param>
    public void CreateTax(object? obj) => Impuestos = obj as List<ImpuestoDocSustento>;

    /// <summary>Sets the withholdings list.</summary>
    /// <param name="obj">The withholdings list.</param>
    public void CreateRetencion(object? obj) => Retenciones = obj as List<Retencion>;

    /// <summary>Sets the reimbursements list.</summary>
    /// <param name="obj">The reimbursements list.</param>
    public void CreateReembolsos(object? obj) => Reembolsos = obj as List<ReembolsoDetalle>;

    /// <summary>Sets the payments list.</summary>
    /// <param name="obj">The payments list.</param>
    public void CreatePayments(object? obj) => Pagos = obj as List<Pago>;
}

/// <summary>
/// Represents tax detail on a supporting document in a withholding receipt.
/// </summary>
public class ImpuestoDocSustento
{
    /// <summary>Gets or sets the tax code of the supporting document.</summary>
    public string CodImpuestoDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax percentage code.</summary>
    public string CodigoPorcentaje { get; set; } = string.Empty;

    /// <summary>Gets or sets the taxable base amount.</summary>
    public string BaseImponible { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax percentage rate.</summary>
    public string Tarifa { get; set; } = string.Empty;

    /// <summary>Gets or sets the calculated tax amount.</summary>
    public string ValorImpuesto { get; set; } = string.Empty;
}

/// <summary>
/// Represents a withholding line item with optional dividend or banana box details.
/// </summary>
public class Retencion
{
    /// <summary>Gets or sets the tax code.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Gets or sets the withholding concept code.</summary>
    public string CodigoRetencion { get; set; } = string.Empty;

    /// <summary>Gets or sets the taxable base amount.</summary>
    public string BaseImponible { get; set; } = string.Empty;

    /// <summary>Gets or sets the withholding percentage rate.</summary>
    public string PorcentajeRetener { get; set; } = string.Empty;

    /// <summary>Gets or sets the withheld tax amount.</summary>
    public string ValorRetenido { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of dividend details.</summary>
    public List<Dividendo>? Dividendos { get; set; }

    /// <summary>Gets or sets the list of banana box purchase details.</summary>
    public List<CompraCajBanano>? BananasBox { get; set; }

    /// <summary>Sets the dividends list.</summary>
    /// <param name="obj">The dividends list.</param>
    public void CreateDividendo(object? obj) => Dividendos = obj as List<Dividendo>;

    /// <summary>Sets the banana box purchases list.</summary>
    /// <param name="obj">The banana box purchases list.</param>
    public void CreateBananaBox(object? obj) => BananasBox = obj as List<CompraCajBanano>;
}

/// <summary>
/// Represents dividend distribution details in a withholding receipt.
/// </summary>
public class Dividendo
{
    /// <summary>Gets or sets the dividend payment date.</summary>
    public string FechaPagoDiv { get; set; } = string.Empty;

    /// <summary>Gets or sets the corporate income tax paid on dividend.</summary>
    public string ImRentaSoc { get; set; } = string.Empty;

    /// <summary>Gets or sets the fiscal year of dividend profits.</summary>
    public string EjerFisUtDiv { get; set; } = string.Empty;
}

/// <summary>
/// Represents banana box purchase details in a withholding receipt.
/// </summary>
public class CompraCajBanano
{
    /// <summary>Gets or sets the number of banana boxes.</summary>
    public string NumCajBan { get; set; } = string.Empty;

    /// <summary>Gets or sets the price per banana box.</summary>
    public string PrecCajBan { get; set; } = string.Empty;
}

/// <summary>
/// Represents reimbursement detail in a purchase settlement or invoice.
/// </summary>
public class ReembolsoDetalle
{
    /// <summary>Gets or sets the reimbursement supplier identification type code.</summary>
    public string TipoIdentificacionProveedorReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement supplier identification number.</summary>
    public string IdentificacionProveedorReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement payment country code.</summary>
    public string CodPaisPagoProveedorReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement supplier type code.</summary>
    public string TipoProveedorReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement document type code.</summary>
    public string CodDocReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement document establishment code.</summary>
    public string EstabDocReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement document emission point.</summary>
    public string PtoEmiDocReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement document sequential number.</summary>
    public string SecuencialDocReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement document issuance date.</summary>
    public string FechaEmisionDocReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement document authorization number.</summary>
    public string NumeroAutorizacionDocReemb { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of reimbursement taxes.</summary>
    public List<DetalleImpuesto>? ImpuestosReembolso { get; set; }

    /// <summary>Sets the reimbursement taxes list.</summary>
    /// <param name="obj">The reimbursement taxes list.</param>
    public void CreateTax(object? obj) => ImpuestosReembolso = obj as List<DetalleImpuesto>;
}

/// <summary>
/// Represents tax detail on a reimbursement document.
/// </summary>
public class DetalleImpuesto
{
    /// <summary>Gets or sets the tax code.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax rate percentage code.</summary>
    public string CodigoPorcentaje { get; set; } = string.Empty;

    /// <summary>Gets or sets the tax rate percentage.</summary>
    public string Tarifa { get; set; } = string.Empty;

    /// <summary>Gets or sets the reimbursement taxable base.</summary>
    public string BaseImponibleReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets the calculated reimbursement tax amount.</summary>
    public string ImpuestoReembolso { get; set; } = string.Empty;
}

/// <summary>
/// Represents an additional information key-value pair on an electronic document.
/// </summary>
public class InfoAdicional
{
    /// <summary>Gets or sets the additional information attribute name.</summary>
    public string? Nombre { get; set; }

    /// <summary>Gets or sets the additional information attribute value.</summary>
    public string? Valor { get; set; }
}

/// <summary>
/// Represents purchase settlement header information in an SRI electronic purchase settlement.
/// </summary>
public class InfoLiquidacionCompra
{
    /// <summary>Gets or sets the issuance date.</summary>
    public string FechaEmision { get; set; } = string.Empty;

    /// <summary>Gets or sets the establishment address.</summary>
    public string DirEstablecimiento { get; set; } = string.Empty;

    /// <summary>Gets or sets the special taxpayer resolution number.</summary>
    public string ContribuyenteEspecial { get; set; } = string.Empty;

    /// <summary>Gets or sets whether required to keep accounting records.</summary>
    public string ObligadoContabilidad { get; set; } = string.Empty;

    /// <summary>Gets or sets the supplier identification type code.</summary>
    public string TipoIdentificacionProveedor { get; set; } = string.Empty;

    /// <summary>Gets or sets the supplier legal or business name.</summary>
    public string RazonSocialProveedor { get; set; } = string.Empty;

    /// <summary>Gets or sets the supplier identification number.</summary>
    public string IdentificacionProveedor { get; set; } = string.Empty;

    /// <summary>Gets or sets the supplier address.</summary>
    public string DireccionProveedor { get; set; } = string.Empty;

    /// <summary>Gets or sets total amount before taxes.</summary>
    public string TotalSinImpuestos { get; set; } = string.Empty;

    /// <summary>Gets or sets total discount amount.</summary>
    public string TotalDescuento { get; set; } = string.Empty;

    /// <summary>Gets or sets reimbursement document type code.</summary>
    public string CodDocReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets total count of reimbursement documents.</summary>
    public string TotalComprobantesReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets total reimbursement taxable base.</summary>
    public string TotalBaseImponibleReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets total reimbursement tax amount.</summary>
    public string TotalImpuestoReembolso { get; set; } = string.Empty;

    /// <summary>Gets or sets total payable amount.</summary>
    public string ImporteTotal { get; set; } = string.Empty;

    /// <summary>Gets or sets currency code.</summary>
    public string Moneda { get; set; } = string.Empty;

    /// <summary>Gets or sets list of tax totals.</summary>
    public List<TotalImpuesto>? TotalImpuestos { get; set; }

    /// <summary>Gets or sets list of payments.</summary>
    public List<Pago>? Pagos { get; set; }

    /// <summary>Gets or sets list of reimbursements.</summary>
    public List<ReembolsoDetalle>? Reembolsos { get; set; }

    /// <summary>Sets the total taxes list.</summary>
    /// <param name="obj">The total taxes list.</param>
    public void CreateTotalTaxes(object? obj) => TotalImpuestos = obj as List<TotalImpuesto>;

    /// <summary>Sets the payments list.</summary>
    /// <param name="obj">The payments list.</param>
    public void CreatePayments(object? obj) => Pagos = obj as List<Pago>;

    /// <summary>Sets the reimbursements list.</summary>
    /// <param name="obj">The reimbursements list.</param>
    public void CreateReembolsos(object? obj) => Reembolsos = obj as List<ReembolsoDetalle>;
}

/// <summary>
/// Represents debit note header information in an SRI electronic debit note.
/// </summary>
public class InfoNotaDebito
{
    /// <summary>Gets or sets the issuance date.</summary>
    public string FechaEmision { get; set; } = string.Empty;

    /// <summary>Gets or sets the establishment address.</summary>
    public string DirEstablecimiento { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer identification type code.</summary>
    public string TipoIdentificacionComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer legal or business name.</summary>
    public string RazonSocialComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the buyer identification number.</summary>
    public string IdentificacionComprador { get; set; } = string.Empty;

    /// <summary>Gets or sets the special taxpayer resolution number.</summary>
    public string ContribuyenteEspecial { get; set; } = string.Empty;

    /// <summary>Gets or sets whether required to keep accounting records.</summary>
    public string ObligadoContabilidad { get; set; } = string.Empty;

    /// <summary>Gets or sets RISE regime indicator.</summary>
    public string Rise { get; set; } = string.Empty;

    /// <summary>Gets or sets modified document type code.</summary>
    public string CodDocModificado { get; set; } = string.Empty;

    /// <summary>Gets or sets modified document number.</summary>
    public string NumDocModificado { get; set; } = string.Empty;

    /// <summary>Gets or sets modified document issuance date.</summary>
    public string FechaEmisionDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets total amount before taxes.</summary>
    public string TotalSinImpuestos { get; set; } = string.Empty;

    /// <summary>Gets or sets total tax amount.</summary>
    public string ImpuestoTotal { get; set; } = string.Empty;

    /// <summary>Gets or sets currency code.</summary>
    public string Moneda { get; set; } = string.Empty;

    /// <summary>Gets or sets list of tax totals.</summary>
    public List<TotalImpuesto>? TotalImpuestos { get; set; }

    /// <summary>Gets or sets list of debit reasons.</summary>
    public List<MotivoNotaDebito>? Motivos { get; set; }

    /// <summary>Gets or sets list of payments.</summary>
    public List<Pago>? Pagos { get; set; }

    /// <summary>Sets the total taxes list.</summary>
    /// <param name="obj">The total taxes list.</param>
    public void CreateTotalTaxes(object? obj) => TotalImpuestos = obj as List<TotalImpuesto>;

    /// <summary>Sets the debit reasons list.</summary>
    /// <param name="obj">The debit reasons list.</param>
    public void CreateMotivos(object? obj) => Motivos = obj as List<MotivoNotaDebito>;

    /// <summary>Sets the payments list.</summary>
    /// <param name="obj">The payments list.</param>
    public void CreatePayments(object? obj) => Pagos = obj as List<Pago>;
}

/// <summary>
/// Represents a debit reason item on an electronic debit note.
/// </summary>
public class MotivoNotaDebito
{
    /// <summary>Gets or sets the reason description for the debit note.</summary>
    public string Razon { get; set; } = string.Empty;

    /// <summary>Gets or sets the monetary value associated with the reason.</summary>
    public string Valor { get; set; } = string.Empty;
}

/// <summary>
/// Represents consignment note header information in an SRI electronic consignment note.
/// </summary>
public class InfoGuiaRemision
{
    /// <summary>Gets or sets the establishment address.</summary>
    public string DirEstablecimiento { get; set; } = string.Empty;

    /// <summary>Gets or sets the departure address.</summary>
    public string DirPartida { get; set; } = string.Empty;

    /// <summary>Gets or sets the carrier legal or business name.</summary>
    public string RazonSocialTransportista { get; set; } = string.Empty;

    /// <summary>Gets or sets the carrier identification type code.</summary>
    public string TipoIdentificacionTransportista { get; set; } = string.Empty;

    /// <summary>Gets or sets the carrier tax ID (RUC).</summary>
    public string RucTransportista { get; set; } = string.Empty;

    /// <summary>Gets or sets RISE regime indicator.</summary>
    public string Rise { get; set; } = string.Empty;

    /// <summary>Gets or sets whether required to keep accounting records.</summary>
    public string ObligadoContabilidad { get; set; } = string.Empty;

    /// <summary>Gets or sets the special taxpayer resolution number.</summary>
    public string ContribuyenteEspecial { get; set; } = string.Empty;

    /// <summary>Gets or sets transport start date.</summary>
    public string FechaIniTransporte { get; set; } = string.Empty;

    /// <summary>Gets or sets transport end date.</summary>
    public string FechaFinTransporte { get; set; } = string.Empty;

    /// <summary>Gets or sets vehicle license plate.</summary>
    public string Placa { get; set; } = string.Empty;
}

/// <summary>
/// Represents a consignment note recipient and transfer destination.
/// </summary>
public class Destinatario
{
    /// <summary>Gets or sets recipient identification number.</summary>
    public string IdentificacionDestinatario { get; set; } = string.Empty;

    /// <summary>Gets or sets recipient legal or business name.</summary>
    public string RazonSocialDestinatario { get; set; } = string.Empty;

    /// <summary>Gets or sets recipient destination address.</summary>
    public string DirDestinatario { get; set; } = string.Empty;

    /// <summary>Gets or sets transport reason.</summary>
    public string MotivoTraslado { get; set; } = string.Empty;

    /// <summary>Gets or sets single customs document (DAU), if applicable.</summary>
    public string DocAduaneroUnico { get; set; } = string.Empty;

    /// <summary>Gets or sets destination establishment code.</summary>
    public string CodEstabDestino { get; set; } = string.Empty;

    /// <summary>Gets or sets transit route description.</summary>
    public string Ruta { get; set; } = string.Empty;

    /// <summary>Gets or sets supporting document type code.</summary>
    public string CodDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets supporting document number.</summary>
    public string NumDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets supporting document authorization number.</summary>
    public string NumAutDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets supporting document issuance date.</summary>
    public string FechaEmisionDocSustento { get; set; } = string.Empty;

    /// <summary>Gets or sets list of recipient details.</summary>
    public List<DetalleDestinatario>? Detalles { get; set; }

    /// <summary>Sets the recipient details list.</summary>
    /// <param name="obj">The recipient details list.</param>
    public void CreateDetalles(object? obj) => Detalles = obj as List<DetalleDestinatario>;
}

/// <summary>
/// Represents a transferred product item detail for a recipient in a consignment note.
/// </summary>
public class DetalleDestinatario
{
    /// <summary>Gets or sets internal product code.</summary>
    public string CodigoInterno { get; set; } = string.Empty;

    /// <summary>Gets or sets additional product code.</summary>
    public string CodigoAdicional { get; set; } = string.Empty;

    /// <summary>Gets or sets product description.</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Gets or sets product quantity.</summary>
    public string Cantidad { get; set; } = string.Empty;

    /// <summary>Gets or sets list of additional details.</summary>
    public List<DetalleAdicional>? DetallesAdicionales { get; set; }

    /// <summary>Sets additional details list.</summary>
    /// <param name="obj">The additional details list.</param>
    public void CreateDetallesAdicionales(object? obj) => DetallesAdicionales = obj as List<DetalleAdicional>;
}

/// <summary>
/// Represents an additional attribute key-value pair for a consignment recipient detail.
/// </summary>
public class DetalleAdicional
{
    /// <summary>Gets or sets additional detail attribute name.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Gets or sets additional detail attribute value.</summary>
    public string Valor { get; set; } = string.Empty;
}
