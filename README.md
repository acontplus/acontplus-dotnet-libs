# Acontplus .NET Libraries

**Acontplus (Ecuador) – .NET Libraries for Business Solutions**

---

Welcome to the official monorepo for Acontplus .NET libraries and applications. This solution provides a comprehensive set of modular, production-ready libraries and sample applications for building robust, scalable business software, with a focus on the Ecuadorian market and global best practices.

## 🏢 About Acontplus

[Acontplus](https://www.acontplus.com) is a leading provider of software solutions in Ecuador, specializing in digital transformation, electronic invoicing, secure integrations, and business process automation. Our libraries are designed to accelerate development, ensure compliance, and promote maintainable architectures for .NET.

---

## 📦 Solution Structure

This repository contains multiple libraries and sample applications, each in its own directory. All projects target .NET and are distributed as NuGet packages.

### Main Libraries (src/)

- **Acontplus.Core**: Foundational DDD components, error handling, specification pattern, DTOs, and C# features for business apps.
- **Acontplus.Billing**: Electronic invoicing and SRI (Ecuadorian Tax Authority) integration. Models, XML, validation, and web service support for Ecuadorian digital documents.
- **Acontplus.Notifications**: Advanced notification system supporting email (MailKit, Amazon SES), templates, and queueing.
- **Acontplus.Reports**: Enterprise report generation: RDLC (Windows), QuestPDF code-first PDF, MiniExcel streaming Excel, and ClosedXML richly-formatted Excel workbooks.
- **Acontplus.Analytics**: Comprehensive analytics and statistics library with domain-agnostic metrics, trends, and business intelligence capabilities.
- **Acontplus.Persistence.Common**: Persistence abstractions, generic repository pattern, context factory, and connection string providers for multi-provider support.
- **Acontplus.Persistence.SqlServer**: SQL Server persistence with ADO.NET, Dapper, and EF Core, repository/unit-of-work patterns, and advanced error handling.
- **Acontplus.Persistence.PostgreSQL**: PostgreSQL persistence with ADO.NET, Dapper, and EF Core, including COPY-based bulk inserts and JSON/JSONB support.
- **Acontplus.Infrastructure**: Caching (in-memory and Redis), resilience patterns (circuit breaker, retry), rate limiting, health checks, response compression, and event bus.
- **Acontplus.Services**: JWT authentication, security headers, authorization policies, device detection, exception handling, and ASP.NET Core middleware.
- **Acontplus.Utilities**: Cross-cutting utilities with consolidated domain-to-API conversion extensions: encryption, IO, text, time, comprehensive API helpers, and clean architectural separation.
- **Acontplus.ApiDocumentation**: Standardized API versioning and OpenAPI/Swagger documentation for .NET APIs.
- **Acontplus.Logging**: Advanced logging with Serilog, supporting console, file, SQL Server, and Elasticsearch sinks, with rich configuration.
- **Acontplus.Barcode**: Barcode and QR code generation utilities for .NET applications.
- **Acontplus.S3Application**: Production-ready AWS S3 storage operations with connection pooling, resilience, and async CRUD support.

> **🏗️ Architecture Note**: The libraries follow clean architecture principles with clear separation between domain logic (`Acontplus.Core`) and API conversion logic (`Acontplus.Utilities`), enabling maintainable and testable code.

### Sample Applications (apps/)

- **Demo.Api**: Example ASP.NET Core Web API demonstrating integration of all libraries, including authentication, reporting, notifications, and more.
- **Demo.Application**: Application layer sample with DTOs, services, and domain logic.
- **Demo.Domain**: Domain models/entities for sample/demo scenarios.
- **Demo.Infrastructure**: Infrastructure and persistence for demo/sample apps.

---

## 🚀 Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/acontplus/acontplus-dotnet-libs.git
   cd acontplus-dotnet-libs
   ```
2. **Restore and build:**
   ```bash
   dotnet restore
   dotnet build
   ```
3. **Explore individual library READMEs** in `src/` for detailed usage, features, and API documentation.
4. **Run sample applications** in `apps/` to see real-world integration examples.

---

## 📚 Documentation

- Each library includes a detailed README and XML API docs.
- Centralized documentation and guides: [Documentation Home](docs/wiki/Home.md)
- For Ecuadorian electronic invoicing, see `Acontplus.Billing` and its [README](src/Acontplus.Billing/README.md).

---

## 🤝 Contributing

We welcome contributions from the community! Open an issue or submit a pull request on [GitHub](https://github.com/acontplus/acontplus-dotnet-libs).

---

## 🆘 Support

- 📧 Email: proyectos@acontplus.com
- 🐛 Issues: [GitHub Issues](https://github.com/acontplus/acontplus-dotnet-libs/issues)
- 📖 Documentation: [Wiki](https://github.com/acontplus/acontplus-dotnet-libs/wiki)

---

## 👨‍💻 Author & Maintainer

**Ivan Paz** – [@iferpaz7](https://linktr.ee/iferpaz7)

## 🏢 Company

**[Acontplus](https://www.acontplus.com)** – Software solutions, Ecuador
