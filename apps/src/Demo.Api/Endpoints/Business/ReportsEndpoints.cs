namespace Demo.Api.Endpoints.Business;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/reports")
            .WithTags("Reports");

        group.MapGet("/sample-invoice", async (Microsoft.Extensions.Logging.ILogger<object> logger, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            var reportService = httpContext.RequestServices.GetRequiredService<IRdlcReportService>();
            try
            {
                logger.LogInformation("Generating sample invoice report");

                // Create parameters DataSet
                var parameters = new DataSet();

                // Add ReportProps table
                var reportPropsTable = new DataTable("ReportProps");
                reportPropsTable.Columns.Add("ReportPath", typeof(string));
                reportPropsTable.Columns.Add("ReportName", typeof(string));
                reportPropsTable.Columns.Add("ReportFormat", typeof(string));
                reportPropsTable.Rows.Add("SampleInvoice.rdlc", "Sample_Invoice", "PDF");
                parameters.Tables.Add(reportPropsTable);

                // Add ReportParams table for logo and other parameters
                var reportParamsTable = new DataTable("ReportParams");
                reportParamsTable.Columns.Add("paramName", typeof(string));
                reportParamsTable.Columns.Add("paramValue", typeof(byte[]));
                reportParamsTable.Columns.Add("isPicture", typeof(bool));
                reportParamsTable.Columns.Add("isCompressed", typeof(bool));

                // Add company name parameter
                reportParamsTable.Rows.Add("CompanyName", System.Text.Encoding.UTF8.GetBytes("Acontplus Demo Company"), false, false);
                reportParamsTable.Rows.Add("InvoiceTitle", System.Text.Encoding.UTF8.GetBytes("SAMPLE INVOICE"), false, false);
                parameters.Tables.Add(reportParamsTable);

                // Create data DataSet
                var data = new DataSet();

                // Add Invoice Header data
                var invoiceHeaderTable = new DataTable("InvoiceHeader");
                invoiceHeaderTable.Columns.Add("InvoiceNumber", typeof(string));
                invoiceHeaderTable.Columns.Add("InvoiceDate", typeof(string));
                invoiceHeaderTable.Columns.Add("CustomerName", typeof(string));
                invoiceHeaderTable.Columns.Add("CustomerAddress", typeof(string));
                invoiceHeaderTable.Columns.Add("CustomerTaxId", typeof(string));
                invoiceHeaderTable.Columns.Add("Subtotal", typeof(decimal));
                invoiceHeaderTable.Columns.Add("Tax", typeof(decimal));
                invoiceHeaderTable.Columns.Add("Total", typeof(decimal));

                invoiceHeaderTable.Rows.Add(
                    "INV-2024-001",
                    DateTime.Now.ToString("yyyy-MM-dd"),
                    "ABC Corporation",
                    "123 Business St, Suite 100, City, Country",
                    "1234567890001",
                    1000.00m,
                    120.00m,
                    1120.00m
                );
                data.Tables.Add(invoiceHeaderTable);

                // Add Invoice Items data
                var invoiceItemsTable = new DataTable("InvoiceItems");
                invoiceItemsTable.Columns.Add("ItemNumber", typeof(int));
                invoiceItemsTable.Columns.Add("Description", typeof(string));
                invoiceItemsTable.Columns.Add("Quantity", typeof(int));
                invoiceItemsTable.Columns.Add("UnitPrice", typeof(decimal));
                invoiceItemsTable.Columns.Add("Amount", typeof(decimal));

                invoiceItemsTable.Rows.Add(1, "Professional Services - Web Development", 20, 25.00m, 500.00m);
                invoiceItemsTable.Rows.Add(2, "Consulting Services - System Architecture", 10, 30.00m, 300.00m);
                invoiceItemsTable.Rows.Add(3, "Software License - Enterprise Edition", 1, 200.00m, 200.00m);

                data.Tables.Add(invoiceItemsTable);

                // Generate the report
                var report = await reportService.GetReportAsync(parameters, data, false, cancellationToken);

                logger.LogInformation("Sample invoice report generated successfully");

                return Results.File(report.FileContents, report.ContentType, report.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating sample invoice report");
                return Results.Problem("Failed to generate report", statusCode: 500);
            }
        });

        group.MapGet("/sample-customers", async (Microsoft.Extensions.Logging.ILogger<object> logger, HttpContext httpContext, string format = "PDF", CancellationToken cancellationToken = default) =>
        {
            var reportService = httpContext.RequestServices.GetRequiredService<IRdlcReportService>();
            try
            {
                logger.LogInformation("Generating sample customer list report in {Format} format", format);

                // Create parameters DataSet
                var parameters = new DataSet();

                var reportPropsTable = new DataTable("ReportProps");
                reportPropsTable.Columns.Add("ReportPath", typeof(string));
                reportPropsTable.Columns.Add("ReportName", typeof(string));
                reportPropsTable.Columns.Add("ReportFormat", typeof(string));
                reportPropsTable.Rows.Add("CustomerList.rdlc", "Customer_List", format.ToUpper());
                parameters.Tables.Add(reportPropsTable);

                // Add report parameters
                var reportParamsTable = new DataTable("ReportParams");
                reportParamsTable.Columns.Add("paramName", typeof(string));
                reportParamsTable.Columns.Add("paramValue", typeof(byte[]));
                reportParamsTable.Columns.Add("isPicture", typeof(bool));
                reportParamsTable.Columns.Add("isCompressed", typeof(bool));

                reportParamsTable.Rows.Add("ReportDate", System.Text.Encoding.UTF8.GetBytes(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), false, false);
                reportParamsTable.Rows.Add("ReportTitle", System.Text.Encoding.UTF8.GetBytes("Customer List Report"), false, false);
                parameters.Tables.Add(reportParamsTable);

                // Create data DataSet
                var data = new DataSet();

                // Add Customers data
                var customersTable = new DataTable("Customers");
                customersTable.Columns.Add("CustomerId", typeof(int));
                customersTable.Columns.Add("CustomerName", typeof(string));
                customersTable.Columns.Add("Email", typeof(string));
                customersTable.Columns.Add("Phone", typeof(string));
                customersTable.Columns.Add("City", typeof(string));
                customersTable.Columns.Add("TotalPurchases", typeof(decimal));
                customersTable.Columns.Add("Status", typeof(string));

                // Add sample data
                customersTable.Rows.Add(1, "ABC Corporation", "contact@abc.com", "+1-555-0101", "New York", 15000.50m, "Active");
                customersTable.Rows.Add(2, "XYZ Industries", "info@xyz.com", "+1-555-0102", "Los Angeles", 23500.75m, "Active");
                customersTable.Rows.Add(3, "Tech Solutions LLC", "hello@techsol.com", "+1-555-0103", "San Francisco", 8900.00m, "Active");
                customersTable.Rows.Add(4, "Global Trading Co", "sales@global.com", "+1-555-0104", "Chicago", 45000.25m, "Premium");
                customersTable.Rows.Add(5, "Smart Systems Inc", "contact@smart.com", "+1-555-0105", "Boston", 12300.00m, "Active");
                customersTable.Rows.Add(6, "Future Enterprises", "info@future.com", "+1-555-0106", "Seattle", 5600.80m, "Inactive");
                customersTable.Rows.Add(7, "Digital Dynamics", "hello@digital.com", "+1-555-0107", "Miami", 19800.50m, "Active");
                customersTable.Rows.Add(8, "Innovative Partners", "contact@innov.com", "+1-555-0108", "Denver", 31200.00m, "Premium");

                data.Tables.Add(customersTable);

                // Generate the report
                var report = await reportService.GetReportAsync(parameters, data, false, cancellationToken);

                logger.LogInformation("Customer list report generated successfully");

                return Results.File(report.FileContents, report.ContentType, report.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating customer list report");
                return Results.Problem("Failed to generate report", statusCode: 500);
            }
        });

        group.MapGet("/test-configuration", () =>
        {
            return Results.Ok(new
            {
                message = "Report service is configured and ready",
                timestamp = DateTime.UtcNow,
                supportedFormats = new[] { "PDF", "EXCEL", "EXCELOPENXML", "WORDOPENXML", "HTML5", "IMAGE" }
            });
        });

        group.MapPost("/test-print", async (Microsoft.Extensions.Logging.ILogger<object> logger, HttpContext httpContext, string? printerName = null, CancellationToken cancellationToken = default) =>
        {
            var printerService = httpContext.RequestServices.GetRequiredService<IRdlcPrinterService>();
            try
            {
                logger.LogInformation("Testing thermal printer with sample invoice");

                // Create data sources matching the SampleInvoice.rdlc schema
                var dataSources = new Dictionary<string, List<Dictionary<string, string>>>
                {
                    ["InvoiceHeader"] = new List<Dictionary<string, string>>
                    {
                        new Dictionary<string, string>
                        {
                            ["InvoiceNumber"] = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                            ["InvoiceDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ["CustomerName"] = "Acontplus Demo Customer",
                            ["CustomerAddress"] = "123 Main St, City, State 12345",
                            ["CustomerTaxId"] = "TAX-123456789",
                            ["Subtotal"] = "13.50",
                            ["Tax"] = "1.62",
                            ["Total"] = "15.12"
                        }
                    },
                    ["InvoiceItems"] = new List<Dictionary<string, string>>
                    {
                        new Dictionary<string, string>
                        {
                            ["ItemNumber"] = "1",
                            ["Description"] = "Coffee - Latte",
                            ["Quantity"] = "2",
                            ["UnitPrice"] = "3.50",
                            ["Amount"] = "7.00"
                        },
                        new Dictionary<string, string>
                        {
                            ["ItemNumber"] = "2",
                            ["Description"] = "Croissant",
                            ["Quantity"] = "1",
                            ["UnitPrice"] = "2.50",
                            ["Amount"] = "2.50"
                        },
                        new Dictionary<string, string>
                        {
                            ["ItemNumber"] = "3",
                            ["Description"] = "Orange Juice",
                            ["Quantity"] = "1",
                            ["UnitPrice"] = "4.00",
                            ["Amount"] = "4.00"
                        }
                    }
                };

                // Create report parameters (can add logo or other params here)
                var reportParams = new Dictionary<string, string>
                {
                    ["CompanyName"] = "Acontplus Demo Store",
                    ["InvoiceTitle"] = "RECEIPT"
                };

                // Create printer configuration
                var rdlcPrinter = new RdlcPrinterDto
                {
                    PrinterName = printerName ?? "Microsoft Print to PDF", // Default to PDF printer for testing
                    FileName = "SampleInvoice.rdlc", // Reuse existing invoice template for testing
                    Format = "IMAGE", // Use IMAGE format for printing
                    ReportsDirectory = Path.Combine(AppContext.BaseDirectory, "Reports"),
                    LogoDirectory = Path.Combine(AppContext.BaseDirectory, "Reports", "Images"),
                    LogoName = "logo",
                    DeviceInfo = "<DeviceInfo><OutputFormat>EMF</OutputFormat></DeviceInfo>",
                    Copies = 1
                };

                // Create print request
                var printRequest = new RdlcPrintRequestDto
                {
                    DataSources = dataSources,
                    ReportParams = reportParams
                };

                // Execute the print
                var success = await printerService.PrintAsync(rdlcPrinter, printRequest, cancellationToken);

                if (success)
                {
                    logger.LogInformation("Print test completed successfully to printer: {PrinterName}", rdlcPrinter.PrinterName);
                    return Results.Ok(new
                    {
                        message = "Print job sent successfully",
                        printerName = rdlcPrinter.PrinterName,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    logger.LogWarning("Print test failed for printer: {PrinterName}", rdlcPrinter.PrinterName);
                    return Results.Problem("Print job failed", statusCode: 500);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during print test");
                return Results.Problem("Print test failed", statusCode: 500);
            }
        });
    }
}
