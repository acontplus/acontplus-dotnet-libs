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
- **Acontplus.Notifications**: Advanced notification system supporting email (MailKit, Amazon SES), WhatsApp, push, templates, and queueing.
- **Acontplus.Reports**: RDLC report generation, export (PDF/Excel), and template management for .NET apps.
- **Acontplus.Analytics**: Comprehensive analytics and statistics library with domain-agnostic metrics, trends, and business intelligence capabilities.
- **Acontplus.Persistence.SqlServer**: SQL Server persistence with ADO.NET and EF Core, repository/unit-of-work patterns, and advanced error handling.
- **Acontplus.Services**: API services, authentication, claims, JWT, middleware, and configuration for robust APIs.
- **Acontplus.Utilities**: Cross-cutting utilities with consolidated domain-to-API conversion extensions: encryption, IO, text, time, comprehensive API helpers, and clean architectural separation.
- **Acontplus.ApiDocumentation**: Standardized API versioning and OpenAPI/Swagger documentation for .NET APIs.
- **Acontplus.Logging**: Advanced logging with Serilog, supporting local, S3, and database sinks, with rich configuration.
- **Acontplus.Barcode**: Barcode and QR code generation utilities for .NET applications.
- **Acontplus.S3Application**: Simple, strongly-typed AWS S3 storage operations with async CRUD support.

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

We welcome contributions from the community! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on how to get involved.

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

---

**Built with ❤️ for the .NET community and Ecuadorian businesses.**
