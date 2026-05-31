using System.Text;

namespace Demo.Api.Endpoints.Business;

public static class SriSignerEndpoints
{
    public static void MapSriSignerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/billing/sign")
            .WithTags("Billing - XAdES-BES Signature")
            .DisableAntiforgery();

        group.MapPost("/", SignComprobante)
            .WithName("SignComprobante")
            .WithSummary("Sign an XML comprobante with XAdES-BES for SRI Ecuador")
            .WithDescription("""
                Accepts a multipart form with the unsigned comprobante XML and the PFX/P12
                certificate, signs it using XAdES-BES (RSA-SHA1) and returns the signed XML
                ready to submit to the SRI reception web service.
                The certificate bytes never leave memory — EphemeralKeySet is used so no key
                material is written to disk.
                """)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<IResult>(StatusCodes.Status200OK, "text/xml")
            .Produces<ApiResponse>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AllowAnonymous();
    }

    private static async Task<IResult> SignComprobante(
        IFormFile xmlFile,
        IFormFile pfxFile,
        [Microsoft.AspNetCore.Mvc.FromForm] string pfxPassword,
        [Microsoft.AspNetCore.Mvc.FromForm] string claveAcceso,
        ISriSigner signer,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        // Basic file validation
        if (xmlFile is null || xmlFile.Length == 0)
            return Results.BadRequest(ApiResponse.Failure(
                [new ApiError("INVALID_XML_FILE", "The XML comprobante file is required and must not be empty.")]));

        if (pfxFile is null || pfxFile.Length == 0)
            return Results.BadRequest(ApiResponse.Failure(
                [new ApiError("INVALID_PFX_FILE", "The PFX/P12 certificate file is required and must not be empty.")]));

        if (string.IsNullOrWhiteSpace(pfxPassword))
            return Results.BadRequest(ApiResponse.Failure(
                [new ApiError("MISSING_PASSWORD", "The certificate password is required.")]));

        if (string.IsNullOrWhiteSpace(claveAcceso) || claveAcceso.Length != 49)
            return Results.BadRequest(ApiResponse.Failure(
                [new ApiError("INVALID_CLAVE_ACCESO", "La clave de acceso debe tener exactamente 49 dígitos.")]));

        try
        {
            // Read both files concurrently
            using var xmlStream = new MemoryStream();
            using var pfxStream = new MemoryStream();

            await Task.WhenAll(
                xmlFile.CopyToAsync(xmlStream, ct),
                pfxFile.CopyToAsync(pfxStream, ct));

            string xmlUnsigned = Encoding.UTF8.GetString(xmlStream.ToArray());
            byte[] pfxBytes = pfxStream.ToArray();

            logger.LogInformation(
                "Signing comprobante. ClaveAcceso={ClaveAcceso}, XmlSize={XmlSize}B, PfxSize={PfxSize}B",
                claveAcceso, xmlStream.Length, pfxStream.Length);

            string xmlSigned = await signer.SignAsync(xmlUnsigned, pfxPassword, pfxBytes, claveAcceso, ct);

            var signedBytes = Encoding.UTF8.GetBytes(xmlSigned);
            var fileName = $"signed_{claveAcceso}.xml";

            logger.LogInformation(
                "Comprobante signed successfully. ClaveAcceso={ClaveAcceso}, SignedSize={Size}B",
                claveAcceso, signedBytes.Length);

            return Results.File(signedBytes, "text/xml", fileName);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid input for SRI signing. ClaveAcceso={ClaveAcceso}", claveAcceso);
            return Results.BadRequest(ApiResponse.Failure(
                [new ApiError("INVALID_INPUT", ex.Message)]));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Certificate or XML error during signing. ClaveAcceso={ClaveAcceso}", claveAcceso);
            return Results.BadRequest(ApiResponse.Failure(
                [new ApiError("SIGNING_ERROR", ex.Message)]));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error signing comprobante. ClaveAcceso={ClaveAcceso}", claveAcceso);
            return Results.Problem(
                detail: "An unexpected error occurred while signing the comprobante.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
