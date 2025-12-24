using Acontplus.Billing.Constants;
using System.Xml;
using System.Xml.Linq;

namespace Demo.Api.Endpoints.Business;

public static class DocumentoElectronicoEndpoints
{
    public static void MapDocumentoElectronicoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/documento-electronico")
            .WithTags("Documento Electronico");

        group.MapPost("/", ValidateXml)
            .WithName("ValidateXml")
            .DisableAntiforgery()
            .Produces<ApiResponse<List<ValidationError>>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> ValidateXml(IFormFile file, HttpContext httpContext)
    {
        try
        {
            var xmlSriFileService = httpContext.RequestServices.GetRequiredService<IXmlSriFileService>();
            var xmlSriFile = await xmlSriFileService.GetAsync(file);

            if (xmlSriFile?.XmlSri == null)
            {
                return Results.BadRequest(ApiResponse<List<ValidationError>>.Failure(
                    new ApiError("INVALID_XML", "Invalid XML document")));
            }

            // Auto-detect document type and version from XML
            var xsdFileName = GetXsdFileName(xmlSriFile.XmlSri);
            Console.WriteLine($"Detected schema: {xsdFileName}");

            var xsdStream = ResourceHelper.GetXsdStream($"Schemas.{xsdFileName}");
            var errors = XmlValidator.Validate(xmlSriFile.XmlSri, xsdStream);

            // Handle validation results
            if (errors.Count == 0)
            {
                var docName = string.IsNullOrEmpty(xmlSriFile.CodDoc)
                    ? "Unknown"
                    : DocumentTypes.GetDocumentName(xmlSriFile.CodDoc);
                Console.WriteLine($"✓ XML is valid ({docName})");
            }
            else
            {
                Console.WriteLine($"✗ XML validation failed. {errors.Count} error(s) found:");
                foreach (var error in errors)
                {
                    Console.WriteLine(
                        $"  [{error.Severity}] Line {error.LineNumber}:{error.LinePosition} - {error.Message}");
                }
            }

            return Results.Ok(ApiResponse<List<ValidationError>>.Success(errors));
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"✗ Schema not found: {ex.Message}");
            return Results.BadRequest(ApiResponse<List<ValidationError>>.Failure(
                new ApiError("SCHEMA_NOT_FOUND", "Schema file not found for this document type or version")));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Validation error: {ex.Message}");
            return Results.BadRequest(ApiResponse<List<ValidationError>>.Failure(
                new ApiError("VALIDATION_ERROR", ex.Message)));
        }
    }

    /// <summary>
    /// Determines the XSD schema file name based on document type code and version
    /// </summary>
    private static string GetXsdFileName(XmlDocument xmlDocument)
    {
        // Convert XmlDocument to XDocument for easier querying
        using var nodeReader = new XmlNodeReader(xmlDocument);
        var xDoc = XDocument.Load(nodeReader);
        var root = xDoc.Root;

        if (root == null)
            throw new InvalidOperationException("Invalid XML: no root element found");

        // Get version attribute
        var version = root.Attribute("version")?.Value ?? "1.0.0";

        // Determine document type from root element name and map to schema file
        var schemaName = root.Name.LocalName switch
        {
            "factura" => $"factura_V{version}.xsd",
            "notaCredito" => $"NotaCredito_V{version}.xsd",
            "notaDebito" => $"NotaDebito_V{version}.xsd",
            "comprobanteRetencion" => $"comprobanteRetencion_V{version}.xsd",
            "guiaRemision" => $"GuiaRemision_V{version}.xsd",
            "liquidacionCompra" => $"LiquidacionCompra_V{version}.xsd",
            _ => throw new NotSupportedException($"Unsupported document type: {root.Name.LocalName}")
        };

        return schemaName;
    }
}
