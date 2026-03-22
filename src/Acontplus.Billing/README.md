# Acontplus.Billing

[![NuGet](https://img.shields.io/nuget/v/Acontplus.Billing.svg)](https://www.nuget.org/packages/Acontplus.Billing)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive .NET library for electronic invoicing and digital document handling in Ecuador, fully compliant with SRI (Servicio de Rentas Internas) normatives v2.32. Provides complete support for all electronic document types, XML generation, validation, parsing, and web service integration.

## 🚀 Features

### 📋 Complete SRI Document Type Support

All 6 electronic document types according to SRI Ficha Técnica v2.32:

- ✅ **Factura (01)** - Invoice with embedded XSD schemas (v1.0.0, v1.1.0, v2.0.0, v2.1.0)
- ✅ **Liquidación de Compra (03)** - Purchase Settlement with XSD validation (v1.0.0)
- ✅ **Nota de Crédito (04)** - Credit Note with schemas (v1.0.0, v1.1.0)
- ✅ **Nota de Débito (05)** - Debit Note with XSD validation (v1.0.0)
- ✅ **Guía de Remisión (06)** - Delivery Guide with schema support (v1.0.0)
- ✅ **Comprobante de Retención (07)** - Withholding Receipt with schemas (v1.0.0, v2.0.0)

### 🔧 Core Capabilities

- **XML Generation**: Complete XML document generation for all SRI document types
- **XML Parsing**: Robust parsing and deserialization of SRI-authorized documents
- **XSD Validation**: Embedded XSD schemas for all document types and versions
- **SRI Web Services**: Full integration with SRI authentication and submission endpoints
- **Document Conversion**: XML to HTML/PDF rendering for all document types
- **ATS Support**: Anexo Transaccional Simplificado (ATS) generation and parsing
- **Identity Validation**: Ecuadorian ID card (cédula) and RUC format/checksum verification
- **Reimbursement Support**: Complete reimbursement (reembolsos) handling
- **Payment Methods**: Multi-payment method support with installment tracking
- **Additional Info**: Flexible additional information fields (up to 15 custom fields)

## 📦 Installation

### NuGet Package Manager

```bash
Install-Package Acontplus.Billing
```

### .NET CLI

```bash
dotnet add package Acontplus.Billing
```

### PackageReference

```xml
<ItemGroup>
  <PackageReference Include="Acontplus.Billing" Version="1.2.0" />
</ItemGroup>
```

## 🎯 Quick Start

### 1. Register Services

```csharp
// In Program.cs - register Billing services
services.AddSingleton<IWebServiceSri, WebServiceSri>();
services.AddSingleton<IDocumentConverter, DocumentConverter>();
services.AddSingleton<IElectronicDocumentService, ElectronicDocumentService>();
// Add other services as needed
```

### 2. Configuration in appsettings.json

```json
{
  "Billing": {
    "Environment": "Development",
    "ValidateBeforeSend": true,
    "DefaultTimeoutSeconds": 30,
    "DocumentStoragePath": "Documents",
    "CompanyRuc": "0991234567001",
    "CompanyLegalName": "ACME COMPANY S.A.",
    "CompanyCommercialName": "ACME",
    "EnableCaching": true,
    "CacheDurationMinutes": 60,
    "SriConnection": {
      "BaseUrl": "https://celcer.sri.gob.ec/",
      "TimeoutSeconds": 30,
      "MaxRetryAttempts": 3,
      "ValidateSslCertificate": true
    }
  }
}
```

### 3. Usage Examples

#### Parse an SRI-Authorized XML Document

```csharp
public class DocumentParser
{
    private readonly IElectronicDocumentService _documentService;
    public DocumentParser(IElectronicDocumentService documentService) => _documentService = documentService;

    public ComprobanteElectronico? Parse(string xmlContent)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);
        return _documentService.TryParseDocument(xmlDoc, out var comprobante, out _) ? comprobante : null;
    }
}
```

#### Send Document to SRI

```csharp
public class DocumentSender
{
    private readonly IWebServiceSri _sriService;
    public DocumentSender(IWebServiceSri sriService) => _sriService = sriService;

    public async Task<ResponseSri> ReceiveAsync(string xmlSigned, string receptionUrl)
        => await _sriService.ReceptionAsync(xmlSigned, receptionUrl);

    public async Task<ResponseSri> AuthorizeAsync(string claveAcceso, string authorizationUrl)
        => await _sriService.AuthorizationAsync(claveAcceso, authorizationUrl);
}
```

#### Convert XML to HTML for Display

```csharp
public class DocumentRenderer
{
    private readonly IDocumentConverter _converter;
    public DocumentRenderer(IDocumentConverter converter) => _converter = converter;
    public string RenderDocument(string xmlContent) => _converter.ConvertToHtml(xmlContent);
}
```

## 📚 API Documentation

### Document Type Constants

```csharp
using Acontplus.Billing.Constants;

// Access document type codes
DocumentTypes.Factura;                  // "01"
DocumentTypes.LiquidacionCompra;        // "03"
DocumentTypes.NotaCredito;              // "04"
DocumentTypes.NotaDebito;               // "05"
DocumentTypes.GuiaRemision;             // "06"
DocumentTypes.ComprobanteRetencion;     // "07"

// Get document name
var name = DocumentTypes.GetDocumentName("01"); // "Factura"

// Validate document code
bool isValid = DocumentTypes.IsValidDocumentCode("01"); // true
```

### Core Services

- **`IWebServiceSri`** - SRI SOAP web service: document reception, authorization, and XML retrieval
- **`IDocumentConverter`** - XML to HTML/PDF conversion for all document types
- **`IXmlDocumentParser`** - Parse SRI-authorized XML documents
- **`IAtsXmlService`** - Generate ATS (Anexo Transaccional Simplificado) XML
- **`IElectronicDocumentService`** - High-level document management
- **`IDocumentValidator`** - XSD schema validation

### Document Models

All document types include complete model classes:

- **`ComprobanteElectronico`** - Base electronic document container
- **`InfoFactura`** - Invoice information
- **`InfoLiquidacionCompra`** - Purchase settlement information
- **`InfoNotaCredito`** - Credit note information
- **`InfoNotaDebito`** - Debit note information with motivos
- **`InfoGuiaRemision`** - Delivery guide information
- **`InfoCompRetencion`** - Withholding information
- **`Destinatario`** - Delivery guide recipients with details
- **`DocSustento`** - Supporting documents with retentions and reimbursements

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Development Setup

```bash
git clone https://github.com/acontplus/acontplus-dotnet-libs.git
cd acontplus-dotnet-libs
dotnet restore
dotnet build
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

- 📧 Email: proyectos@acontplus.com
- 🐛 Issues: [GitHub Issues](https://github.com/acontplus/acontplus-dotnet-libs/issues)
- 📖 Documentation: [Wiki](https://github.com/acontplus/acontplus-dotnet-libs/wiki)

## 👨‍💻 Author

**Ivan Paz** - [@iferpaz7](https://linktr.ee/iferpaz7)

## 🏢 Company

**[Acontplus](https://www.acontplus.com)** - Software solutions

---

**Built with ❤️ for the .NET community**
