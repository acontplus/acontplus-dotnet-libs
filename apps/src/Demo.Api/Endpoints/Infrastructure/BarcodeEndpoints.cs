namespace Demo.Api.Endpoints.Infrastructure;

public static class BarcodeEndpoints
{
    public static void MapBarcodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/barcode")
            .WithTags("Barcode");

        group.MapGet("/", (string? text, bool includeLabel = false) =>
        {
            var barcodeConfig = new BarcodeConfig
            {
                Text = text ?? "0605202201030150819800120010030000012904948150712",
                Format = ZXing.BarcodeFormat.CODE_128,
                IncludeLabel = includeLabel
            };
            var barcode = BarcodeGen.Create(barcodeConfig);

            return Results.File(barcode, "image/png", "barcode.png");
        });
    }
}
