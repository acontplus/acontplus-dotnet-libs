namespace Demo.Api.Endpoints.Business;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this IEndpointRouteBuilder app)
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

        // ── QuestPDF endpoints ────────────────────────────────────────────────────

        group.MapGet("/questpdf/invoice", async (
            Microsoft.Extensions.Logging.ILogger<object> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var pdf = httpContext.RequestServices.GetRequiredService<IQuestPdfReportService>();
            try
            {
                logger.LogInformation("Generating QuestPDF sample invoice");

                // ── Line items DataTable ──────────────────────────────────────────
                var lineItems = new DataTable("LineItems");
                lineItems.Columns.Add("LineNo", typeof(int));
                lineItems.Columns.Add("Description", typeof(string));
                lineItems.Columns.Add("Qty", typeof(decimal));
                lineItems.Columns.Add("UnitPrice", typeof(decimal));
                lineItems.Columns.Add("Amount", typeof(decimal));

                lineItems.Rows.Add(1, "Professional Services – Web Development", 20m, 25.00m, 500.00m);
                lineItems.Rows.Add(2, "Consulting – System Architecture", 10m, 30.00m, 300.00m);
                lineItems.Rows.Add(3, "Software License – Enterprise Edition", 1m, 200.00m, 200.00m);

                // ── Build the document request ────────────────────────────────────
                var request = new QuestPdfReportRequest
                {
                    Title = "Invoice #INV-2026-0042",
                    SubTitle = "Issued: 2026-03-01  |  Due: 2026-03-31",
                    Author = "Acontplus ERP",
                    Subject = "Commercial Invoice",
                    FileDownloadName = "Invoice_INV-2026-0042",

                    Settings = new QuestPdfDocumentSettings
                    {
                        PageSize = QuestPdfPageSize.A4,
                        Orientation = QuestPdfPageOrientation.Portrait,
                        FontFamily = "Helvetica",
                        FontSize = 9f,
                        ShowPageNumbers = true,
                        ShowTimestamp = true,
                        ColorTheme = QuestPdfColorThemes.AcontplusDefault()
                    },

                    GlobalHeader = new QuestPdfHeaderFooterOptions
                    {
                        LeftText = "Acontplus Demo Company",
                        RightText = "RUC: 1792123456001",
                        BackgroundColor = "#d61672",
                        ShowBorderBottom = false,
                        FontSize = 9f
                    },

                    Sections =
                    [
                        // 1. Client & invoice summary
                        new QuestPdfSection
                        {
                            SectionTitle = "Invoice Details",
                            Type         = QuestPdfSectionType.KeyValueSummary,
                            KeyValues    = new Dictionary<string, string>
                            {
                                ["Invoice No."]    = "INV-2026-0042",
                                ["Issue Date"]     = "2026-03-01",
                                ["Due Date"]       = "2026-03-31",
                                ["Customer"]       = "ABC Corporation",
                                ["Tax ID"]         = "1234567890001",
                                ["Address"]        = "123 Business St, Suite 100, New York, USA",
                                ["Payment Terms"]  = "Net 30"
                            }
                        },

                        // 2. Line-items grid with totals row
                        new QuestPdfSection
                        {
                            SectionTitle  = "Line Items",
                            Type          = QuestPdfSectionType.DataTable,
                            Data          = lineItems,
                            ShowTotalsRow = true,
                            Columns       =
                            [
                                new QuestPdfTableColumn { ColumnName = "LineNo",      Header = "#",          RelativeWidth = 0.5f },
                                new QuestPdfTableColumn { ColumnName = "Description", Header = "Description", RelativeWidth = 5f   },
                                new QuestPdfTableColumn { ColumnName = "Qty",         Header = "Qty",         RelativeWidth = 0.8f,
                                    Alignment = QuestPdfColumnAlignment.Right, Format = "N2" },
                                new QuestPdfTableColumn { ColumnName = "UnitPrice",   Header = "Unit Price",  RelativeWidth = 1.5f,
                                    Alignment = QuestPdfColumnAlignment.Right, Format = "C2" },
                                new QuestPdfTableColumn { ColumnName = "Amount",      Header = "Amount",      RelativeWidth = 1.5f,
                                    Alignment = QuestPdfColumnAlignment.Right, Format = "C2",
                                    AggregateType = QuestPdfAggregateType.Sum, IsBold = true }
                            ]
                        },

                        // 3. Financial summary text
                        new QuestPdfSection
                        {
                            Type       = QuestPdfSectionType.Text,
                            TextBlocks =
                            [
                                new QuestPdfTextBlock { Content = "Subtotal:  $1,000.00", Bold = false, PaddingBottom = 2f  },
                                new QuestPdfTextBlock { Content = "VAT (12%): $  120.00", Bold = false, PaddingBottom = 2f  },
                                new QuestPdfTextBlock { Content = "TOTAL DUE: $1,120.00", Bold = true,  FontSize = 12f, Color = "#831843", PaddingBottom = 0f }
                            ]
                        },

                        // 4. Bank-transfer payment instructions (key-value keeps Demo.Api free of QuestPDF.Fluent dependency)
                        new QuestPdfSection
                        {
                            SectionTitle = "Payment Instructions",
                            Type         = QuestPdfSectionType.KeyValueSummary,
                            KeyValues    = new Dictionary<string, string>
                            {
                                ["Bank"]      = "Banco Pichincha",
                                ["Account"]   = "2200123456789",
                                ["SWIFT"]     = "PICHECEQ",
                                ["Reference"] = "INV-2026-0042"
                            }
                        }
                    ]
                };

                var response = await pdf.GenerateAsync(request, cancellationToken);

                logger.LogInformation("QuestPDF invoice generated — {Bytes} bytes", response.FileContents.Length);

                return Results.File(response.FileContents, response.ContentType, response.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating QuestPDF invoice");
                return Results.Problem("Failed to generate QuestPDF invoice", statusCode: 500);
            }
        })
        .WithName("QuestPdfSampleInvoice")
        .WithDescription("Generates a full multi-section A4 invoice PDF using QuestPDF (key-value summary, data table with aggregate totals, text summary, custom payment block)");

        group.MapGet("/questpdf/sales-report", async (
            Microsoft.Extensions.Logging.ILogger<object> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var pdf = httpContext.RequestServices.GetRequiredService<IQuestPdfReportService>();
            try
            {
                logger.LogInformation("Generating QuestPDF sales report");

                // ── Customers DataTable ───────────────────────────────────────────
                var customers = new DataTable("Customers");
                customers.Columns.Add("Id", typeof(int));
                customers.Columns.Add("CustomerName", typeof(string));
                customers.Columns.Add("City", typeof(string));
                customers.Columns.Add("Segment", typeof(string));
                customers.Columns.Add("Revenue", typeof(decimal));
                customers.Columns.Add("Orders", typeof(int));

                customers.Rows.Add(1, "ABC Corporation", "New York", "Enterprise", 45000.50m, 12);
                customers.Rows.Add(2, "XYZ Industries", "Los Angeles", "SMB", 23500.75m, 8);
                customers.Rows.Add(3, "Tech Solutions LLC", "San Francisco", "Startup", 8900.00m, 5);
                customers.Rows.Add(4, "Global Trading Co", "Chicago", "Enterprise", 78200.25m, 20);
                customers.Rows.Add(5, "Smart Systems Inc", "Boston", "SMB", 12300.00m, 6);
                customers.Rows.Add(6, "Future Enterprises", "Seattle", "Startup", 5600.80m, 3);
                customers.Rows.Add(7, "Digital Dynamics", "Miami", "SMB", 19800.50m, 9);
                customers.Rows.Add(8, "Innovative Partners", "Denver", "Enterprise", 31200.00m, 11);
                customers.Rows.Add(9, "CloudFirst Inc", "Austin", "Startup", 6450.00m, 4);
                customers.Rows.Add(10, "Data Architects", "Portland", "SMB", 14700.00m, 7);

                // ── Top monthly sales DataTable ───────────────────────────────────
                var monthlySales = new DataTable("MonthlySales");
                monthlySales.Columns.Add("Month", typeof(string));
                monthlySales.Columns.Add("Revenue", typeof(decimal));
                monthlySales.Columns.Add("Orders", typeof(int));
                monthlySales.Columns.Add("Avg", typeof(decimal));

                monthlySales.Rows.Add("Oct 2025", 38500.00m, 21, 1833.33m);
                monthlySales.Rows.Add("Nov 2025", 51200.00m, 28, 1828.57m);
                monthlySales.Rows.Add("Dec 2025", 62300.00m, 34, 1832.35m);
                monthlySales.Rows.Add("Jan 2026", 44100.00m, 25, 1764.00m);
                monthlySales.Rows.Add("Feb 2026", 57800.00m, 31, 1864.52m);
                monthlySales.Rows.Add("Mar 2026", 18500.00m, 11, 1681.82m);

                var request = new QuestPdfReportRequest
                {
                    Title = "Sales Report — Q1 2026",
                    SubTitle = "Generated: 2026-03-01  |  Period: October 2025 – March 2026",
                    FileDownloadName = "SalesReport_Q1_2026",

                    Settings = new QuestPdfDocumentSettings
                    {
                        PageSize = QuestPdfPageSize.A4,
                        Orientation = QuestPdfPageOrientation.Landscape,
                        FontSize = 9f,
                        ShowPageNumbers = true,
                        ColorTheme = QuestPdfColorThemes.Corporate()
                    },

                    Sections =
                    [
                        // KPI summary
                        new QuestPdfSection
                        {
                            SectionTitle = "Period KPIs",
                            Type         = QuestPdfSectionType.KeyValueSummary,
                            KeyValues    = new Dictionary<string, string>
                            {
                                ["Total Revenue"]        = "$245,652.80",
                                ["Total Orders"]         = "85",
                                ["Average Order Value"]  = "$2,890.03",
                                ["New Customers"]        = "4",
                                ["Top Segment"]          = "Enterprise ($154,400.75)",
                                ["Top City"]             = "Chicago ($78,200.25)"
                            }
                        },

                        // Monthly breakdown table
                        new QuestPdfSection
                        {
                            SectionTitle  = "Monthly Revenue Breakdown",
                            Type          = QuestPdfSectionType.DataTable,
                            Data          = monthlySales,
                            ShowTotalsRow = true,
                            Columns       =
                            [
                                new QuestPdfTableColumn { ColumnName = "Month",   Header = "Month",           RelativeWidth = 2f },
                                new QuestPdfTableColumn { ColumnName = "Revenue", Header = "Revenue (USD)",   RelativeWidth = 2f,
                                    Alignment = QuestPdfColumnAlignment.Right, Format = "C2",
                                    AggregateType = QuestPdfAggregateType.Sum, IsBold = true },
                                new QuestPdfTableColumn { ColumnName = "Orders",  Header = "Orders",          RelativeWidth = 1f,
                                    Alignment = QuestPdfColumnAlignment.Right,
                                    AggregateType = QuestPdfAggregateType.Sum },
                                new QuestPdfTableColumn { ColumnName = "Avg",     Header = "Avg. Order (USD)",RelativeWidth = 2f,
                                    Alignment = QuestPdfColumnAlignment.Right, Format = "C2",
                                    AggregateType = QuestPdfAggregateType.Average }
                            ]
                        },

                        // Customer breakdown table
                        new QuestPdfSection
                        {
                            SectionTitle  = "Top Customers",
                            Type          = QuestPdfSectionType.DataTable,
                            Data          = customers,
                            ShowTotalsRow = true,
                            Columns       =
                            [
                                new QuestPdfTableColumn { ColumnName = "Id",           Header = "#",            RelativeWidth = 0.4f },
                                new QuestPdfTableColumn { ColumnName = "CustomerName", Header = "Customer",     RelativeWidth = 3f   },
                                new QuestPdfTableColumn { ColumnName = "City",         Header = "City",         RelativeWidth = 1.5f },
                                new QuestPdfTableColumn { ColumnName = "Segment",      Header = "Segment",      RelativeWidth = 1.2f },
                                new QuestPdfTableColumn { ColumnName = "Revenue",      Header = "Revenue (USD)",RelativeWidth = 1.5f,
                                    Alignment = QuestPdfColumnAlignment.Right, Format = "C2",
                                    AggregateType = QuestPdfAggregateType.Sum, IsBold = true },
                                new QuestPdfTableColumn { ColumnName = "Orders",       Header = "Orders",       RelativeWidth = 0.8f,
                                    Alignment = QuestPdfColumnAlignment.Right,
                                    AggregateType = QuestPdfAggregateType.Sum }
                            ]
                        }
                    ]
                };

                var response = await pdf.GenerateAsync(request, cancellationToken);

                logger.LogInformation("QuestPDF sales report generated — {Bytes} bytes", response.FileContents.Length);

                return Results.File(response.FileContents, response.ContentType, response.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating QuestPDF sales report");
                return Results.Problem("Failed to generate QuestPDF sales report", statusCode: 500);
            }
        })
        .WithName("QuestPdfSalesReport")
        .WithDescription("Generates a landscape A4 quarterly sales report PDF using QuestPDF with two data-table sections and a KPI key-value panel");

        group.MapGet("/questpdf/quick-table", async (
            Microsoft.Extensions.Logging.ILogger<object> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var pdf = httpContext.RequestServices.GetRequiredService<IQuestPdfReportService>();
            try
            {
                logger.LogInformation("Generating QuestPDF quick-table report");

                // Minimal usage: pass a plain DataTable — service auto-derives all columns
                var products = new DataTable("Products");
                products.Columns.Add("SKU", typeof(string));
                products.Columns.Add("Product", typeof(string));
                products.Columns.Add("Category", typeof(string));
                products.Columns.Add("Stock", typeof(int));
                products.Columns.Add("Price", typeof(decimal));
                products.Columns.Add("LastUpdated", typeof(string));

                products.Rows.Add("PRD-001", "Laptop Pro 15\"", "Electronics", 45, 1299.99m, "2026-02-28");
                products.Rows.Add("PRD-002", "Wireless Keyboard", "Accessories", 120, 49.99m, "2026-02-27");
                products.Rows.Add("PRD-003", "4K Monitor 27\"", "Electronics", 30, 399.99m, "2026-02-28");
                products.Rows.Add("PRD-004", "USB-C Hub 7-Port", "Accessories", 200, 29.99m, "2026-02-25");
                products.Rows.Add("PRD-005", "Noise-Cancel Headphones", "Electronics", 80, 149.99m, "2026-02-28");
                products.Rows.Add("PRD-006", "Ergonomic Mouse", "Accessories", 150, 39.99m, "2026-02-26");
                products.Rows.Add("PRD-007", "SSD 1TB NVMe", "Storage", 60, 89.99m, "2026-02-28");
                products.Rows.Add("PRD-008", "Webcam 4K", "Accessories", 55, 79.99m, "2026-02-27");

                // Uses GenerateFromDataTableAsync — the simplest one-liner API
                var columns = new List<QuestPdfTableColumn>
                {
                    new() { ColumnName = "SKU",         Header = "SKU",           RelativeWidth = 1.2f },
                    new() { ColumnName = "Product",     Header = "Product Name",  RelativeWidth = 3f   },
                    new() { ColumnName = "Category",    Header = "Category",      RelativeWidth = 1.5f },
                    new() { ColumnName = "Stock",       Header = "Stock",         RelativeWidth = 0.8f,
                            Alignment = QuestPdfColumnAlignment.Right,
                            AggregateType = QuestPdfAggregateType.Sum },
                    new() { ColumnName = "Price",       Header = "Price (USD)",   RelativeWidth = 1.2f,
                            Alignment = QuestPdfColumnAlignment.Right, Format = "C2",
                            AggregateType = QuestPdfAggregateType.Sum, IsBold = true },
                    new() { ColumnName = "LastUpdated", Header = "Last Updated",  RelativeWidth = 1.5f }
                };

                var response = await pdf.GenerateFromDataTableAsync(
                    "Product Inventory Snapshot — 2026-03-01",
                    products,
                    columns,
                    new QuestPdfDocumentSettings
                    {
                        PageSize = QuestPdfPageSize.A4,
                        Orientation = QuestPdfPageOrientation.Landscape,
                        ShowPageNumbers = true
                    },
                    cancellationToken);

                logger.LogInformation("QuestPDF quick-table generated — {Bytes} bytes", response.FileContents.Length);

                return Results.File(response.FileContents, response.ContentType, response.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating QuestPDF quick-table");
                return Results.Problem("Failed to generate QuestPDF quick-table report", statusCode: 500);
            }
        })
        .WithName("QuestPdfQuickTable")
        .WithDescription("Demonstrates GenerateFromDataTableAsync — the minimal single-DataTable API. Returns a landscape A4 product inventory PDF.");

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

        // ── MiniExcel endpoints ───────────────────────────────────────────────────

        /// <summary>
        /// Demonstrates MiniExcel streaming bulk export from a DataTable (single sheet).
        /// Low memory overhead — ideal for large datasets.
        /// </summary>
        group.MapGet("/miniexcel/customers", async (
            Microsoft.Extensions.Logging.ILogger<object> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var excel = httpContext.RequestServices.GetRequiredService<IMiniExcelReportService>();
            try
            {
                logger.LogInformation("Generating MiniExcel customer list export");

                var customers = BuildSampleCustomersTable();

                var response = await excel.GenerateFromDataTableAsync(
                    fileDownloadName: "customers-export",
                    data: customers,
                    columns:
                    [
                        new ExcelColumnDefinition { ColumnName = "CustomerId",     Header = "ID" },
                        new ExcelColumnDefinition { ColumnName = "CustomerName",   Header = "Customer" },
                        new ExcelColumnDefinition { ColumnName = "Email",          Header = "E-mail" },
                        new ExcelColumnDefinition { ColumnName = "Phone",          Header = "Phone" },
                        new ExcelColumnDefinition { ColumnName = "City",           Header = "City" },
                        new ExcelColumnDefinition { ColumnName = "TotalPurchases", Header = "Total Purchases", Format = "N2" },
                        new ExcelColumnDefinition { ColumnName = "Status",         Header = "Status" },
                        new ExcelColumnDefinition { ColumnName = "RegisteredAt",   Header = "Registered",      Format = "yyyy-MM-dd" }
                    ],
                    worksheetName: "Customers",
                    cancellationToken: cancellationToken);

                logger.LogInformation("MiniExcel customer export generated ({Size:N0} bytes)", response.FileContents.Length);
                return Results.File(response.FileContents, response.ContentType, response.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating MiniExcel customer export");
                return Results.Problem("Failed to generate Excel export", statusCode: 500);
            }
        })
        .WithName("GetCustomersMiniExcel")
        .WithDescription("Exports customer list as a streaming Excel workbook via MiniExcel (low memory)");

        /// <summary>
        /// Demonstrates MiniExcel multi-sheet workbook: Customers + Orders in one file.
        /// </summary>
        group.MapGet("/miniexcel/multi-sheet", async (
            Microsoft.Extensions.Logging.ILogger<object> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var excel = httpContext.RequestServices.GetRequiredService<IMiniExcelReportService>();
            try
            {
                logger.LogInformation("Generating MiniExcel multi-sheet workbook");

                var request = new ExcelReportRequest
                {
                    FileDownloadName = "business-summary",
                    Worksheets =
                    [
                        new ExcelWorksheetDefinition
                        {
                            Name    = "Customers",
                            Data    = BuildSampleCustomersTable(),
                            Columns =
                            [
                                new ExcelColumnDefinition { ColumnName = "CustomerId",     Header = "ID" },
                                new ExcelColumnDefinition { ColumnName = "CustomerName",   Header = "Customer" },
                                new ExcelColumnDefinition { ColumnName = "City",           Header = "City" },
                                new ExcelColumnDefinition { ColumnName = "TotalPurchases", Header = "Purchases", Format = "N2" },
                                new ExcelColumnDefinition { ColumnName = "Status",         Header = "Status" }
                            ]
                        },
                        new ExcelWorksheetDefinition
                        {
                            Name    = "Orders",
                            Data    = BuildSampleOrdersTable(),
                            Columns =
                            [
                                new ExcelColumnDefinition { ColumnName = "OrderId",    Header = "Order #" },
                                new ExcelColumnDefinition { ColumnName = "Customer",   Header = "Customer" },
                                new ExcelColumnDefinition { ColumnName = "OrderDate",  Header = "Date",   Format = "yyyy-MM-dd" },
                                new ExcelColumnDefinition { ColumnName = "Amount",     Header = "Amount", Format = "N2" },
                                new ExcelColumnDefinition { ColumnName = "Status",     Header = "Status" }
                            ]
                        }
                    ]
                };

                var response = await excel.GenerateAsync(request, cancellationToken);

                logger.LogInformation("MiniExcel multi-sheet workbook generated ({Size:N0} bytes)", response.FileContents.Length);
                return Results.File(response.FileContents, response.ContentType, response.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating MiniExcel multi-sheet workbook");
                return Results.Problem("Failed to generate multi-sheet workbook", statusCode: 500);
            }
        })
        .WithName("GetMultiSheetMiniExcel")
        .WithDescription("Exports a multi-sheet Excel workbook (Customers + Orders) via MiniExcel");

        // ── ClosedXML endpoints ───────────────────────────────────────────────────

        /// <summary>
        /// Demonstrates ClosedXML richly formatted single-sheet report: corporate styles,
        /// freeze pane, AutoFilter, alternating rows, and a SUM totals row.
        /// </summary>
        group.MapGet("/closedxml/sales", async (
            Microsoft.Extensions.Logging.ILogger<object> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var excel = httpContext.RequestServices.GetRequiredService<IClosedXmlReportService>();
            try
            {
                logger.LogInformation("Generating ClosedXML formatted sales report");

                var response = await excel.GenerateFromDataTableAsync(
                    fileDownloadName: "sales-report",
                    data: BuildSampleOrdersTable(),
                    columns:
                    [
                        new AdvancedExcelColumnDefinition { ColumnName = "OrderId",   Header = "Order #",  Width = 12,  Alignment = ExcelHorizontalAlignment.Center },
                        new AdvancedExcelColumnDefinition { ColumnName = "Customer",  Header = "Customer", Width = 28 },
                        new AdvancedExcelColumnDefinition { ColumnName = "OrderDate", Header = "Date",     Width = 14,  NumberFormat = "yyyy-MM-dd", Alignment = ExcelHorizontalAlignment.Center },
                        new AdvancedExcelColumnDefinition { ColumnName = "Amount",    Header = "Amount",   Width = 14,  NumberFormat = "$#,##0.00",  Alignment = ExcelHorizontalAlignment.Right, AggregateType = ExcelAggregateType.Sum },
                        new AdvancedExcelColumnDefinition { ColumnName = "Status",    Header = "Status",   Width = 12,  Alignment = ExcelHorizontalAlignment.Center }
                    ],
                    autoFilter: true,
                    freezeHeaderRow: true,
                    headerStyle: AdvancedExcelHeaderStyle.CorporateBlue(),
                    cancellationToken: cancellationToken);

                logger.LogInformation("ClosedXML sales report generated ({Size:N0} bytes)", response.FileContents.Length);
                return Results.File(response.FileContents, response.ContentType, response.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating ClosedXML sales report");
                return Results.Problem("Failed to generate formatted Excel report", statusCode: 500);
            }
        })
        .WithName("GetSalesClosedXml")
        .WithDescription("Exports a richly formatted sales report via ClosedXML with corporate styles and SUM totals");

        /// <summary>
        /// Demonstrates ClosedXML full multi-sheet annual report workbook with workbook metadata,
        /// multiple header styles, aggregate rows, and alternating shading per sheet.
        /// </summary>
        group.MapGet("/closedxml/annual-report", async (
            Microsoft.Extensions.Logging.ILogger<object> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var excel = httpContext.RequestServices.GetRequiredService<IClosedXmlReportService>();
            try
            {
                logger.LogInformation("Generating ClosedXML annual report workbook");

                var request = new AdvancedExcelReportRequest
                {
                    FileDownloadName = $"annual-report-{DateTime.UtcNow.Year}",
                    Author = "Acontplus ERP",
                    Company = "Acontplus Demo",
                    Subject = "Annual Business Report",
                    Keywords = "sales,customers,orders",
                    Worksheets =
                    [
                        new AdvancedExcelWorksheetDefinition
                        {
                            Name                 = "Sales Summary",
                            Data                 = BuildSampleOrdersTable(),
                            AutoFilter           = true,
                            FreezeHeaderRow      = true,
                            AlternatingRowShading = true,
                            AlternatingRowColor   = "EBF3FB",
                            IncludeAggregateRow   = true,
                            HeaderStyle           = AdvancedExcelHeaderStyle.CorporateBlue(),
                            Columns              =
                            [
                                new() { ColumnName = "OrderId",   Header = "Order #",  Width = 12,  Alignment = ExcelHorizontalAlignment.Center },
                                new() { ColumnName = "Customer",  Header = "Customer", Width = 28 },
                                new() { ColumnName = "OrderDate", Header = "Date",     Width = 14,  NumberFormat = "yyyy-MM-dd",  Alignment = ExcelHorizontalAlignment.Center },
                                new() { ColumnName = "Amount",    Header = "Amount",   Width = 14,  NumberFormat = "$#,##0.00",   Alignment = ExcelHorizontalAlignment.Right, AggregateType = ExcelAggregateType.Sum },
                                new() { ColumnName = "Status",    Header = "Status",   Width = 12,  Alignment = ExcelHorizontalAlignment.Center }
                            ]
                        },
                        new AdvancedExcelWorksheetDefinition
                        {
                            Name                 = "Customers",
                            Data                 = BuildSampleCustomersTable(),
                            AutoFilter           = true,
                            FreezeHeaderRow      = true,
                            AlternatingRowShading = true,
                            AlternatingRowColor   = "E9F5E9",
                            IncludeAggregateRow   = true,
                            HeaderStyle           = AdvancedExcelHeaderStyle.DarkGreen(),
                            Columns              =
                            [
                                new() { ColumnName = "CustomerId",     Header = "ID",         Width = 8,   Alignment = ExcelHorizontalAlignment.Center },
                                new() { ColumnName = "CustomerName",   Header = "Customer",   Width = 28 },
                                new() { ColumnName = "Email",          Header = "E-mail",     Width = 30 },
                                new() { ColumnName = "City",           Header = "City",       Width = 16 },
                                new() { ColumnName = "TotalPurchases", Header = "Purchases",  Width = 14,  NumberFormat = "$#,##0.00", Alignment = ExcelHorizontalAlignment.Right, AggregateType = ExcelAggregateType.Sum },
                                new() { ColumnName = "Status",         Header = "Status",     Width = 12,  Alignment = ExcelHorizontalAlignment.Center },
                                new() { ColumnName = "Phone",          IsHidden = true }
                            ]
                        }
                    ]
                };

                var response = await excel.GenerateAsync(request, cancellationToken);

                logger.LogInformation("ClosedXML annual report generated ({Size:N0} bytes)", response.FileContents.Length);
                return Results.File(response.FileContents, response.ContentType, response.FileDownloadName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating ClosedXML annual report");
                return Results.Problem("Failed to generate annual report workbook", statusCode: 500);
            }
        })
        .WithName("GetAnnualReportClosedXml")
        .WithDescription("Exports a multi-sheet annual report workbook via ClosedXML with full corporate styling");
    }

    // ── Sample data helpers ───────────────────────────────────────────────────

    private static DataTable BuildSampleCustomersTable()
    {
        var t = new DataTable("Customers");
        t.Columns.Add("CustomerId", typeof(int));
        t.Columns.Add("CustomerName", typeof(string));
        t.Columns.Add("Email", typeof(string));
        t.Columns.Add("Phone", typeof(string));
        t.Columns.Add("City", typeof(string));
        t.Columns.Add("TotalPurchases", typeof(decimal));
        t.Columns.Add("Status", typeof(string));
        t.Columns.Add("RegisteredAt", typeof(DateTime));

        t.Rows.Add(1, "ABC Corporation", "contact@abc.com", "+1-555-0101", "New York", 15000.50m, "Active", new DateTime(2022, 3, 15));
        t.Rows.Add(2, "XYZ Industries", "info@xyz.com", "+1-555-0102", "Los Angeles", 23500.75m, "Active", new DateTime(2021, 7, 22));
        t.Rows.Add(3, "Tech Solutions LLC", "hello@techsol.com", "+1-555-0103", "San Francisco", 8900.00m, "Active", new DateTime(2023, 1, 10));
        t.Rows.Add(4, "Global Trading Co", "sales@global.com", "+1-555-0104", "Chicago", 45000.25m, "Premium", new DateTime(2020, 11, 5));
        t.Rows.Add(5, "Smart Systems Inc", "contact@smart.com", "+1-555-0105", "Boston", 12300.00m, "Active", new DateTime(2022, 8, 30));
        t.Rows.Add(6, "Future Enterprises", "info@future.com", "+1-555-0106", "Seattle", 5600.80m, "Inactive", new DateTime(2021, 4, 18));
        t.Rows.Add(7, "Digital Dynamics", "hello@digital.com", "+1-555-0107", "Miami", 19800.50m, "Active", new DateTime(2023, 5, 2));
        t.Rows.Add(8, "Innovative Partners", "contact@innov.com", "+1-555-0108", "Denver", 31200.00m, "Premium", new DateTime(2019, 12, 14));

        return t;
    }

    private static DataTable BuildSampleOrdersTable()
    {
        var t = new DataTable("Orders");
        t.Columns.Add("OrderId", typeof(string));
        t.Columns.Add("Customer", typeof(string));
        t.Columns.Add("OrderDate", typeof(DateTime));
        t.Columns.Add("Amount", typeof(decimal));
        t.Columns.Add("Status", typeof(string));

        t.Rows.Add("ORD-2026-001", "ABC Corporation", new DateTime(2026, 1, 5), 1500.00m, "Delivered");
        t.Rows.Add("ORD-2026-002", "XYZ Industries", new DateTime(2026, 1, 12), 3200.50m, "Delivered");
        t.Rows.Add("ORD-2026-003", "Tech Solutions LLC", new DateTime(2026, 1, 20), 850.00m, "Delivered");
        t.Rows.Add("ORD-2026-004", "Global Trading Co", new DateTime(2026, 2, 3), 7500.00m, "In Transit");
        t.Rows.Add("ORD-2026-005", "Smart Systems Inc", new DateTime(2026, 2, 14), 2100.75m, "Delivered");
        t.Rows.Add("ORD-2026-006", "Future Enterprises", new DateTime(2026, 2, 18), 450.00m, "Cancelled");
        t.Rows.Add("ORD-2026-007", "Digital Dynamics", new DateTime(2026, 2, 25), 4800.00m, "In Transit");
        t.Rows.Add("ORD-2026-008", "Innovative Partners", new DateTime(2026, 3, 1), 9200.00m, "Processing");

        return t;
    }
}
