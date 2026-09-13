using Acontplus.Billing.Models.Documents;

namespace Acontplus.Billing.Services.Documents.Mapping;

public class AtsDataSetMapper
{
    public AtsData MapDataSetToAtsData(DataSet ds)
    {
        if (ds == null)
        {
            throw new ArgumentNullException(nameof(ds), "DataSet cannot be null for mapping.");
        }

        var headerDt = ds.Tables["header"];
        var purchasesDt = ds.Tables["purchases"];
        var salesDt = ds.Tables["sales"];
        var creditNotesDt = ds.Tables["credit_notes"]; // Used for canceled documents
        var withHoldingTaxesDt = ds.Tables["with_holding_taxes"];
        var establishmentSalesDt = ds.Tables["establishment_sales"];

        return new AtsData
        {
            Header = MapHeader(headerDt),
            Purchases = MapPurchases(purchasesDt),
            WithholdingTaxes = MapWithholdingTaxes(withHoldingTaxesDt),
            Sales = MapSales(salesDt),
            EstablishmentSales = MapEstablishmentSales(establishmentSalesDt),
            CanceledDocuments = MapCanceledDocuments(creditNotesDt)
        };
    }

    private static AtsHeader MapHeader(DataTable? dt)
    {
        if (dt == null || dt.Rows.Count == 0)
        {
            // Log this or throw a more specific exception if header data is mandatory
            return new AtsHeader(); // Return an empty/default header
        }

        var dr = dt.Rows[0]; // Assuming only one header row
        return new AtsHeader
        {
            TipoIdInformante = dr["TipoIdInformante"].ToString() ?? string.Empty,
            IdInformante = dr["IdInformante"].ToString() ?? string.Empty,
            RazonSocial = dr["razonSocial"].ToString() ?? string.Empty,
            Anio = dr["anio"].ToString() ?? string.Empty,
            Mes = dr["mes"].ToString() ?? string.Empty,
            NumEstabRuc = dr["numEstabRuc"].ToString() ?? string.Empty,
            TotalVentas = dr["totalVentas"].ToString() ?? string.Empty,
            CodigoOperativo = dr["codigoOperativo"].ToString() ?? string.Empty
        };
    }

    private static List<Purchase> MapPurchases(DataTable? dt)
    {
        return dt == null || dt.Rows.Count == 0
            ? []
            : (from DataRow dr in dt.Rows
               select new Purchase
               {
                   CodSustento = dr["codSustento"].ToString() ?? string.Empty,
                   TpIdProv = dr["tpIdProv"].ToString() ?? string.Empty,
                   IdProv = dr["idProv"].ToString() ?? string.Empty,
                   TipoComprobante = dr["tipoComprobante"].ToString() ?? string.Empty,
                   ParteRel = dr["parteRel"].ToString() ?? string.Empty,
                   FechaRegistro = dr["fechaRegistro"].ToString() ?? string.Empty,
                   Establecimiento = dr["establecimiento"].ToString() ?? string.Empty,
                   PuntoEmision = dr["puntoEmision"].ToString() ?? string.Empty,
                   Secuencial = dr["secuencial"].ToString() ?? string.Empty,
                   FechaEmision = dr["fechaEmision"].ToString() ?? string.Empty,
                   Autorizacion = dr["autorizacion"].ToString() ?? string.Empty,
                   BaseNoGraIva = dr["baseNoGraIva"].ToString() ?? string.Empty,
                   BaseImponible = dr["baseImponible"].ToString() ?? string.Empty,
                   BaseImpGrav = dr["baseImpGrav"].ToString() ?? string.Empty,
                   BaseImpExe = dr["baseImpExe"].ToString() ?? string.Empty,
                   MontoIce = dr["montoIce"].ToString() ?? string.Empty,
                   MontoIva = dr["montoIva"].ToString() ?? string.Empty,
                   ValRetBien10 = dr["valRetBien10"].ToString() ?? string.Empty,
                   ValRetServ20 = dr["valRetServ20"].ToString() ?? string.Empty,
                   ValorRetBienes = dr["valorRetBienes"].ToString() ?? string.Empty,
                   ValRetServ50 = dr["valRetServ50"].ToString() ?? string.Empty,
                   ValorRetServicios = dr["valorRetServicios"].ToString() ?? string.Empty,
                   ValRetServ100 = dr["valRetServ100"].ToString() ?? string.Empty,
                   TotbasesImpReemb = dr["totbasesImpReemb"].ToString() ?? string.Empty,
                   PagoLocExt = dr["pagoLocExt"].ToString() ?? string.Empty,
                   TipoRegi = GetColumnValueOrDefault(dr, "tipoRegi") ?? string.Empty, // Conditionally present
                   DenopagoRegFis = dr["denopagoRegFis"].ToString() ?? string.Empty,
                   PaisEfecPago = dr["paisEfecPago"].ToString() ?? string.Empty,
                   AplicConvDobTrib = dr["aplicConvDobTrib"].ToString() ?? string.Empty,
                   PagExtSujRetNorLeg = dr["pagExtSujRetNorLeg"].ToString() ?? string.Empty,
                   FormaPago = dr["formaPago"].ToString() ?? string.Empty,
                   DocModificado = GetColumnValueOrDefault(dr, "docModificado"),
                   EstabModificado = GetColumnValueOrDefault(dr, "estabModificado"),
                   PtoEmiModificado = GetColumnValueOrDefault(dr, "ptoEmiModificado"),
                   SecModificado = GetColumnValueOrDefault(dr, "secModificado"),
                   AutModificado = GetColumnValueOrDefault(dr, "autModificado"),
                   EstabRetencion1 = GetColumnValueOrDefault(dr, "estabRetencion1"),
                   PtoEmiRetencion1 = GetColumnValueOrDefault(dr, "ptoEmiRetencion1"),
                   SecRetencion1 = GetColumnValueOrDefault(dr, "secRetencion1"),
                   AutRetencion1 = GetColumnValueOrDefault(dr, "autRetencion1"),
                   FechaEmiRet1 = GetColumnValueOrDefault(dr, "fechaEmiRet1"),
                   NroDocumento = dr["nroDocumento"].ToString() ?? string.Empty // Essential for linking
               }).ToList();
    }

    private static List<WithholdingTax> MapWithholdingTaxes(DataTable? dt)
    {
        return dt == null || dt.Rows.Count == 0
            ? []
            : (from DataRow dr in dt.Rows
               select new WithholdingTax
               {
                   CodRetAir = dr["codRetAir"].ToString() ?? string.Empty,
                   BaseImpAir = dr["baseImpAir"].ToString() ?? string.Empty,
                   PorcentajeAir = dr["porcentajeAir"].ToString() ?? string.Empty,
                   ValRetAir = dr["valRetAir"].ToString() ?? string.Empty,
                   NroDocumento = dr["nroDocumento"].ToString() ?? string.Empty, // Essential for linking
                   ClaveAcceso =
                       dr["claveAcceso"].ToString() ?? string.Empty // Essential for linking (original Autorizacion)
               }).ToList();
    }

    private static List<Sale> MapSales(DataTable? dt)
    {
        return dt == null || dt.Rows.Count == 0
            ? []
            : (from DataRow dr in dt.Rows
               select new Sale
               {
                   TpIdCliente = dr["tpIdCliente"].ToString() ?? string.Empty,
                   IdCliente = dr["idCliente"].ToString() ?? string.Empty,
                   ParteRelVtas = GetColumnValueOrDefault(dr, "parteRelVtas"), // Conditionally present
                   TipoCliente = GetColumnValueOrDefault(dr, "tipoCliente"), // Conditionally present
                   DenoCli = GetColumnValueOrDefault(dr, "denoCli"), // Conditionally present
                   TipoComprobante = dr["tipoComprobante"].ToString() ?? string.Empty,
                   TipoEmision = dr["tipoEmision"].ToString() ?? string.Empty,
                   NumeroComprobantes = dr["numeroComprobantes"].ToString() ?? string.Empty,
                   BaseNoGraIva = dr["baseNoGraIva"].ToString() ?? string.Empty,
                   BaseImponible = dr["baseImponible"].ToString() ?? string.Empty,
                   BaseImpGrav = dr["baseImpGrav"].ToString() ?? string.Empty,
                   MontoIva = dr["montoIva"].ToString() ?? string.Empty,
                   TipoCompe = GetColumnValueOrDefault(dr, "tipoCompe"), // Conditionally present
                   MontoCompensacion = GetColumnValueOrDefault(dr, "monto"), // Conditionally present
                   MontoIce = dr["montoIce"].ToString() ?? string.Empty,
                   ValorRetIva = dr["valorRetIva"].ToString() ?? string.Empty,
                   ValorRetRenta = dr["valorRetRenta"].ToString() ?? string.Empty,
                   FormaPago = dr["formaPago"].ToString() ?? string.Empty
               }).ToList();
    }

    private static List<EstablishmentSale> MapEstablishmentSales(DataTable? dt)
    {
        return dt == null || dt.Rows.Count == 0
            ? []
            : (from DataRow dr in dt.Rows
               select new EstablishmentSale
               {
                   CodEstab = dr["codEstab"].ToString() ?? string.Empty,
                   VentasEstab = dr["ventasEstab"].ToString() ?? string.Empty,
                   IvaComp = dr["ivaComp"].ToString() ?? string.Empty
               }).ToList();
    }

    private static List<CanceledDocument> MapCanceledDocuments(DataTable? dt)
    {
        return dt == null || dt.Rows.Count == 0
            ? []
            : (from DataRow dr in dt.Rows
               select new CanceledDocument
               {
                   TipoComprobante = dr["tipoComprobante"].ToString() ?? string.Empty,
                   Establecimiento = dr["establecimiento"].ToString() ?? string.Empty,
                   PuntoEmision = dr["puntoEmision"].ToString() ?? string.Empty,
                   SecuencialInicio = dr["secuencialInicio"].ToString() ?? string.Empty,
                   SecuencialFin = dr["secuencialFin"].ToString() ?? string.Empty,
                   Autorizacion = dr["autorizacion"].ToString() ?? string.Empty
               }).ToList();
    }

    // Helper method to safely get a string value from a DataRow column
    // Handles cases where the column might not exist or the value is DBNull
    private static string? GetColumnValueOrDefault(DataRow dr, string columnName)
    {
        return dr.Table.Columns.Contains(columnName) && dr[columnName] != DBNull.Value ? dr[columnName].ToString() : null;
    }
}
