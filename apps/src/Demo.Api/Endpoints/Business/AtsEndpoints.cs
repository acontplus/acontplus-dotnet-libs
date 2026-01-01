namespace Demo.Api.Endpoints.Business;

public static class AtsEndpoints
{
    public static void MapAtsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ats")
            .WithTags("ATS");

        group.MapGet("/download", DownloadAts)
            .WithName("DownloadAts")
            .WithSummary("Download ATS XML file")
            .WithDescription("Generates and downloads an ATS XML file based on the provided JSON parameters")
            .Produces<FileContentResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> DownloadAts(
        string json,
        IAtsService atsService,
        IAtsXmlService atsXmlService)
    {
        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "userRoleId", 428 },
                { "json", SqlStringParam.Sanitize(json) }
            };
            var mapper = new AtsDataSetMapper();

            var ds = await atsService.GetAsync(parameters);
            var atsData = mapper.MapDataSetToAtsData(ds);
            var xmlBytes = await atsXmlService.CreateAtsXmlAsync(atsData);
            var fileName = "ATS" + "_" + atsData.Header.IdInformante
                           + "_" + atsData.Header.NumEstabRuc
                           + "_" + atsData.Header.Anio
                           + "_" + atsData.Header.Mes;

            return Results.File(xmlBytes, "text/xml", fileName + ".xml");
        }
        catch (Exception)
        {
            // Log the exception if needed
            return Results.Problem("Error generating ATS XML", statusCode: 500);
        }
    }
}
