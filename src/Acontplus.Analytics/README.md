# Acontplus.Analytics

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Analytics.svg)](https://www.nuget.org/packages/Acontplus.Analytics)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

A comprehensive analytics and statistics library for .NET applications, providing domain-agnostic metrics, trends, and business intelligence capabilities. Built with modern .NET 10 features and designed for cross-domain reusability.

## 🚀 Features

### 📊 Comprehensive Analytics DTOs

- **Dashboard Statistics**: Complete business metrics (transactions, revenue, entities, growth rates)
- **Real-Time Metrics**: Live operational data (current activity, processing metrics, capacity utilization)
- **Aggregated Analytics**: Time-series data with statistical aggregates (min, max, avg, percentiles)
- **Trend Analysis**: Advanced trend tracking with moving averages, forecasting, and anomaly detection

### 🎯 Domain-Agnostic Design

Works across **any business domain**:
- 🛒 E-commerce (orders, products, customers)
- 🏥 Healthcare (patients, appointments, treatments)
- 💰 Finance (transactions, accounts, investments)
- 🏭 Manufacturing (production, inventory, quality)
- 📚 Education (students, courses, assessments)
- 🍽️ Hospitality (reservations, orders, guests)

### 💡 Key Capabilities

- **Generic interface** for consistent analytics implementation
- **SQL template library** for building reusable stored procedures
- **Flexible DTO structure** with optional label dictionaries for localization
- **Comparison metrics** (period-over-period, year-over-year)
- **Statistical functions** (percentiles, standard deviation, variance)
- **Performance tracking** (moving averages, trend detection)

## 📦 Installation

```bash
dotnet add package Acontplus.Analytics
```

### NuGet Package Manager

```bash
Install-Package Acontplus.Analytics
```

### PackageReference

```xml
<PackageReference Include="Acontplus.Analytics" Version="x.x.x" />
```

## 🎯 Quick Start

### 1. Define Your Domain-Specific DTOs

Extend the base DTOs with your domain-specific properties:

```csharp
using Acontplus.Analytics.Dtos;

// Your custom dashboard stats
public class SalesDashboardDto : BaseDashboardStatsDto
{
    public decimal AverageOrderSize { get; set; }
    public int TopSellingProductId { get; set; }
    public string TopSellingProductName { get; set; } = string.Empty;
}

// Your custom real-time stats
public class SalesRealTimeDto : BaseRealTimeStatsDto
{
    public int ActiveCheckouts { get; set; }
    public decimal PendingPaymentsTotal { get; set; }
}

// Use base classes directly or extend them
public class SalesAggregatedDto : BaseAggregatedStatsDto { }
public class SalesTrendDto : BaseTrendDto { }
```

### 2. Register the Service

Use the extension method to register the generic service with your specific DTOs and stored procedure names.

```csharp
using Acontplus.Analytics.Extensions;

// In your Program.cs or DependencyInjection setup
services.AddStatisticsService<SalesDashboardDto, SalesRealTimeDto, SalesAggregatedDto, SalesTrendDto>(
    dashboardSpName: "Sales.GetDashboardStats",
    realTimeSpName: "Sales.GetRealTimeStats",
    aggregatedSpName: "Sales.GetAggregatedStats",
    trendsSpName: "Sales.GetTrendStats"
);

// OR use the module-based convention
// This assumes SPs are named: Sales.AnalyticsGetDashboard, Sales.AnalyticsGetRealTime, etc.
services.AddStatisticsService<SalesDashboardDto, SalesRealTimeDto, SalesAggregatedDto, SalesTrendDto>(
    moduleName: "Sales.Analytics"
);
```

### 3. Use the Service

Inject the generic interface into your controllers or endpoints.

```csharp
using Acontplus.Analytics.Interfaces;
using Acontplus.Core.Dtos.Requests;

public class SalesAnalyticsEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/analytics/dashboard", async (
            IStatisticsService<SalesDashboardDto, SalesRealTimeDto, SalesAggregatedDto, SalesTrendDto> statsService,
            FilterRequest filter) =>
        {
            var result = await statsService.GetDashboardStatsAsync(filter);

            if (result.IsFailure)
            {
                return Results.BadRequest(result.Error);
            }

            return Results.Ok(result.Value);
        });
    }
}
```

### 4. Create Stored Procedures with SQL Templates

```sql
-- Example stored procedure using the SQL templates
CREATE PROCEDURE [dbo].[sp_GetSalesDashboardStats]
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Use the date range defaults from StatsSqlTemplates
    SET @StartDate = ISNULL(@StartDate, DATEADD(DAY, -30, GETUTCDATE()));
    SET @EndDate = ISNULL(@EndDate, GETUTCDATE());

    -- Calculate previous period for comparison
    DECLARE @PeriodDays INT = DATEDIFF(DAY, @StartDate, @EndDate);
    DECLARE @PrevStartDate DATETIME2 = DATEADD(DAY, -@PeriodDays, @StartDate);
    DECLARE @PrevEndDate DATETIME2 = @StartDate;

    SELECT
        -- Transaction Metrics
        COUNT(*) AS TotalTransactions,
        SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedTransactions,
        SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledTransactions,
        SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS ActiveTransactions,

        -- Revenue Metrics
        SUM(TotalAmount) AS TotalRevenue,
        SUM(TotalAmount - TaxAmount) AS NetRevenue,
        SUM(TaxAmount) AS TotalTax,
        SUM(DiscountAmount) AS TotalDiscounts,
        AVG(TotalAmount) AS AverageTransactionValue,

        -- Entity Metrics
        COUNT(DISTINCT CustomerId) AS UniqueEntities,
        SUM(CASE WHEN CustomerCreatedDate >= @StartDate THEN 1 ELSE 0 END) AS NewEntities

    FROM Orders
    WHERE CreatedAt BETWEEN @StartDate AND @EndDate;
END
```

## 📚 Usage & Configuration

### DTO Reference

#### BaseDashboardStatsDto

Comprehensive business metrics for executive dashboards:

| Property | Type | Description |
|----------|------|-------------|
| `TotalTransactions` | int | Total count of operations |
| `CompletedTransactions` | int | Successfully completed |
| `TotalRevenue` | decimal | Gross revenue |
| `NetRevenue` | decimal | Revenue after deductions |
| `GrowthRate` | decimal | Growth percentage vs previous period |
| `UniqueEntities` | int | Unique customers/clients |
| `CompletionRate` | decimal | Success rate percentage |

**40+ properties** covering transactions, revenue, volumes, entities, and comparisons.

#### BaseRealTimeStatsDto

Live operational metrics:

| Property | Type | Description |
|----------|------|-------------|
| `ActiveOperations` | int | Current active operations |
| `OperationsLast5Min` | int | Activity in last 5 minutes |
| `CurrentHourRevenue` | decimal | Revenue this hour |
| `ItemsInQueue` | int | Pending items |
| `AverageProcessingTime` | decimal | Processing time in minutes |
| `CapacityUtilizationRate` | decimal | Resource usage percentage |

**25+ properties** for real-time monitoring and capacity planning.

#### BaseAggregatedStatsDto

Time-series aggregations with statistical analysis:

| Property | Type | Description |
|----------|------|-------------|
| `Period` | DateTime | Time period for this data point |
| `Value` | decimal | Primary metric value |
| `MinValue` / `MaxValue` / `AvgValue` | decimal | Statistical aggregates |
| `PreviousPeriodValue` | decimal? | Comparison value |
| `ChangePercent` | decimal? | Period-over-period change |
| `Percentile25` / `50` / `75` | decimal? | Distribution metrics |

**30+ properties** for comprehensive statistical analysis.

#### BaseTrendDto

Advanced trend analysis with forecasting:

| Property | Type | Description |
|----------|------|-------------|
| `Date` | DateTime | Data point timestamp |
| `Value` | decimal | Actual observed value |
| `Forecast` | decimal? | Predicted value |
| `MovingAverage7` / `30` / `90` | decimal? | Trend smoothing |
| `SamePeriodLastYear` | decimal? | Year-over-year comparison |
| `IsAnomaly` | bool | Outlier detection |

**35+ properties** for deep trend analysis and forecasting.

### SQL Templates

Use `StatsSqlTemplates` for consistent SQL patterns:

```csharp
using Acontplus.Analytics.Models;

// Date range parameters
StatsSqlTemplates.DateRangeParams
StatsSqlTemplates.DateRangeDefaults
StatsSqlTemplates.PreviousPeriodCalc

// Aggregation helpers
StatsSqlTemplates.GroupByCase       // Dynamic time grouping
StatsSqlTemplates.PeriodLabelFormat // Human-readable labels

// Statistical functions
StatsSqlTemplates.MovingAverage7    // 7-period moving average
StatsSqlTemplates.MovingAverage30   // 30-period moving average
StatsSqlTemplates.PercentChange     // Percentage change calculation
StatsSqlTemplates.TrendDirection    // up/down/stable classification

// Special functions
StatsSqlTemplates.AnomalyDetection  // Outlier detection
StatsSqlTemplates.IsWeekend         // Weekend classification
```

### Localization

All DTOs include an optional `Labels` dictionary property that your application can populate with localized strings:

```csharp
// In your application's localization service
public class SalesStatisticsLocalization
{
    public static Dictionary<string, string> GetSpanishLabels() => new()
    {
        { "TotalRevenue", "Ingresos Totales" },
        { "NetRevenue", "Ingresos Netos" },
        { "GrowthRate", "Tasa de Crecimiento" },
        { "AverageOrderValue", "Valor Promedio del Pedido" }
    };

    public static Dictionary<string, string> GetEnglishLabels() => new()
    {
        { "TotalRevenue", "Total Revenue" },
        { "NetRevenue", "Net Revenue" },
        { "GrowthRate", "Growth Rate" },
        { "AverageOrderValue", "Average Order Value" }
    };
}

// Populate labels in your service
var dashboard = await GetDashboardStatsAsync(filter);
if (dashboard.IsSuccess)
{
    dashboard.Value.Labels = language == "es"
        ? SalesStatisticsLocalization.GetSpanishLabels()
        : SalesStatisticsLocalization.GetEnglishLabels();
}
```

> **Note**: Localization is intentionally left to the consuming application, allowing you to integrate with your preferred localization system (RESX files, database, JSON, or any i18n framework).

## 🏗️ Architecture & Clean Architecture Guide

### Clean Architecture Implementation

```
├── API Layer (Demo.Api)
│   └── Endpoints/Business/Analytics/SalesAnalyticsEndpoints.cs
│       → Maps HTTP requests to service calls
│       → Applies localization at presentation layer
│
├── Application Layer (Demo.Application)
│   ├── Interfaces/ISalesAnalyticsService.cs
│   │   → Domain-specific analytics contract
│   ├── Services/SalesAnalyticsService.cs
│   │   → Inherits from StatisticsService<T1,T2,T3,T4>
│   │   → Configures stored procedure names
│   ├── Dtos/Analytics/
│   │   ├── SalesDashboardDto.cs (extends BaseDashboardStatsDto)
│   │   └── SalesRealTimeDto.cs (extends BaseRealTimeStatsDto)
│   └── Helpers/SalesAnalyticsLocalization.cs
│       → Application-specific label provider (Spanish/English)
│
├── Domain Layer (Acontplus.TestDomain)
│   └── Entities/Sale.cs
│       → Business entity with analytics-relevant properties
│
└── Infrastructure Layer (Database)
    └── StoredProcedures/SalesAnalytics.sql
        → SQL procedures implementing analytics logic
```

### Implementation Example

#### 1. Domain Layer - Sale Entity

**File**: `apps/src/Acontplus.TestDomain/Entities/Sale.cs`

```csharp
public class Sale : BaseEntity
{
    public int CustomerId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Status { get; set; } // Pending, Completed, Cancelled
    public string PaymentMethod { get; set; }
    public int ItemCount { get; set; }
}
```

#### 2. Application Layer - DTOs

**File**: `apps/src/Acontplus.TestApplication/Dtos/Analytics/SalesDashboardDto.cs`

```csharp
public class SalesDashboardDto : BaseDashboardStatsDto
{
    // Extends base DTO with sales-specific properties
    public decimal AverageOrderValue { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal CashSales { get; set; }
    public decimal CreditCardSales { get; set; }
    public int NewCustomers { get; set; }
    public decimal CustomerRetentionRate { get; set; }
}
```

#### 3. Application Layer - Service Implementation

**File**: `apps/src/Acontplus.TestApplication/Services/SalesAnalyticsService.cs`

```csharp
public class SalesAnalyticsService : StatisticsService<
    SalesDashboardDto,
    SalesRealTimeDto,
    BaseAggregatedStatsDto,
    BaseTrendDto>,
    ISalesAnalyticsService
{
    public SalesAnalyticsService(IAdoRepository adoRepository)
        : base(
            adoRepository,
            dashboardSpName: "Sales.GetDashboardStats",
            realTimeSpName: "Sales.GetRealTimeStats",
            aggregatedSpName: "Sales.GetAggregatedStats",
            trendsSpName: "Sales.GetTrendStats")
    {
    }
}
```

#### 4. API Layer - Endpoints

**File**: `apps/src/Demo.Api/Endpoints/Business/Analytics/SalesAnalyticsEndpoints.cs`

```csharp
public static class SalesAnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapSalesAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics/sales")
            .WithTags("Sales Analytics");

        // Dashboard endpoint
        group.MapGet("/dashboard", async (
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? language,
            [FromServices] ISalesAnalyticsService analyticsService,
            CancellationToken cancellationToken) =>
        {
            var filter = new FilterRequest
            {
                Filters = new Dictionary<string, object>
                {
                    { "StartDate", startDate ?? DateTime.UtcNow.AddDays(-30) },
                    { "EndDate", endDate ?? DateTime.UtcNow }
                }
            };

            var result = await analyticsService.GetDashboardStatsAsync(filter, cancellationToken);

            return result.Match(
                success: data =>
                {
                    data.Labels = SalesAnalyticsLocalization.GetLabels(language ?? "en");
                    return Results.Ok(data);
                },
                failure: error => Results.BadRequest(new { error = error.Message, code = error.Code }));
        });

        return app;
    }
}
```

#### 5. Infrastructure Layer - Stored Procedures

**File**: `apps/database/StoredProcedures/SalesAnalytics.sql`

```sql
CREATE OR ALTER PROCEDURE [Sales].[GetDashboardStats]
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET @StartDate = ISNULL(@StartDate, DATEADD(DAY, -30, GETUTCDATE()));
    SET @EndDate = ISNULL(@EndDate, GETUTCDATE());

    SELECT
        -- Base properties
        COUNT(*) AS TotalTransactions,
        SUM(TotalAmount) AS TotalRevenue,
        AVG(TotalAmount) AS AverageTransactionValue,

        -- Sales-specific properties
        AVG(TotalAmount) AS AverageOrderValue,
        SUM(DiscountAmount) AS TotalDiscounts,
        SUM(CASE WHEN PaymentMethod = 'Cash' THEN TotalAmount ELSE 0 END) AS CashSales,
        COUNT(DISTINCT CustomerId) AS NewCustomers

    FROM Sales
    WHERE SaleDate BETWEEN @StartDate AND @EndDate;
END
```

### Architecture Benefits

#### ✅ Clean Separation of Concerns
- **Domain**: Defines business entities (Sale)
- **Application**: Orchestrates business logic, DTOs, and localization
- **Infrastructure**: Data access via stored procedures
- **API**: HTTP presentation layer with minimal logic

#### ✅ Reusability
- `StatisticsService<T1,T2,T3,T4>` can be reused for any domain
- Just change SP names and DTO types
- Same pattern works for Products, Orders, Customers, etc.

#### ✅ Type Safety
- Generic constraints ensure compile-time safety
- DTOs extend base classes for consistency
- Interface contracts prevent implementation drift

#### ✅ Testability
- Services can be mocked via interfaces
- DTOs are simple POCOs
- Stored procedures can be tested independently

## 🔗 Dependencies

### Required Packages

- **Acontplus.Core**: Result pattern, FilterRequest, domain abstractions
- **Acontplus.Utilities**: API conversion extensions
- **Acontplus.Persistence.Common**: Repository patterns (IAdoRepository)

### Optional Integrations

- **Acontplus.Services**: Expose analytics via API controllers
- **Acontplus.Infrastructure**: Caching for dashboard data

## 📖 Best Practices

1. **Extend base DTOs** for domain-specific properties
2. **Use SQL templates** for consistent stored procedures
3. **Implement caching** for frequently accessed dashboard data
4. **Return Result<T>** for consistent error handling
5. **Add localization labels** for international applications
6. **Use aggregations** for large datasets instead of real-time calculations

## 📚 Use Cases

### E-Commerce Analytics

```csharp
public class OrderDashboardDto : BaseDashboardStatsDto
{
    public decimal AverageOrderValue { get; set; }
    public int AbandonedCarts { get; set; }
    public decimal ConversionRate { get; set; }
}
```

### Healthcare Analytics

```csharp
public class PatientDashboardDto : BaseDashboardStatsDto
{
    public int TotalAppointments { get; set; }
    public int CompletedTreatments { get; set; }
    public decimal PatientSatisfactionScore { get; set; }
}
```

### Financial Analytics

```csharp
public class TransactionDashboardDto : BaseDashboardStatsDto
{
    public decimal ProcessingFees { get; set; }
    public int ChargebackCount { get; set; }
    public decimal ApprovalRate { get; set; }
}
```
