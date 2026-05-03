# Acontplus .NET Libraries Wiki

Welcome to the documentation wiki for the Acontplus .NET Libraries solution. Here you'll find installation guides, usage examples, and best practices for each library in the suite.

## 📚 Wiki Navigation

### Package Documentation (READMEs)

- [Acontplus.Core](../../src/Acontplus.Core/README.md)
- [Acontplus.Infrastructure](../../src/Acontplus.Infrastructure/README.md)
- [Acontplus.Services](../../src/Acontplus.Services/README.md)
- [Acontplus.Analytics](../../src/Acontplus.Analytics/README.md)
- [Acontplus.Billing](../../src/Acontplus.Billing/README.md)
- [Acontplus.Notifications](../../src/Acontplus.Notifications/README.md)
- [Acontplus.Reports](../../src/Acontplus.Reports/README.md)
- [Acontplus.Persistence.SqlServer](../../src/Acontplus.Persistence.SqlServer/README.md)
- [Acontplus.Persistence.PostgreSQL](../../src/Acontplus.Persistence.PostgreSQL/README.md)
- [Acontplus.Utilities](../../src/Acontplus.Utilities/README.md)
- [Acontplus.ApiDocumentation](../../src/Acontplus.ApiDocumentation/README.md)
- [Acontplus.Logging](../../src/Acontplus.Logging/README.md)
- [Acontplus.Barcode](../../src/Acontplus.Barcode/README.md)
- [Acontplus.S3Application](../../src/Acontplus.S3Application/README.md)

## 🔄 Version Update Strategy (Cascade Order)

When bumping versions, always update packages from base to dependents. Never update a dependent before its dependency is published.

```
Level 1 (no internal deps):
  Core, Barcode, Logging, ApiDocumentation, S3Application

Level 2 (depend on Level 1):
  Utilities → Core
  Infrastructure → Core
  Services → Core
  Persistence.Common → Core

Level 3 (depend on Level 2):
  Analytics → Utilities
  Notifications → Utilities
  Billing → Utilities + Barcode
  Persistence.SqlServer → Persistence.Common
  Persistence.PostgreSQL → Persistence.Common

Level 4 (depend on Level 3):
  Reports → Utilities + Barcode
```

📖 Full automation guide: [CASCADE_PUBLISH_GUIDE.md](../CASCADE_PUBLISH_GUIDE.md)

---

## 🏢 About Acontplus

[Acontplus](https://www.acontplus.com) is a leading provider of software solutions in Ecuador, specializing in digital transformation, electronic invoicing, secure integrations, and business process automation.

---

For more information, visit the [main repository](https://github.com/acontplus/acontplus-dotnet-libs) or the [company website](https://www.acontplus.com).
