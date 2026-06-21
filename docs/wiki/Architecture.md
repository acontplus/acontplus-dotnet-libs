# Architecture

## Color Palette

All diagrams use the Acontplus brand palette:

| Role                      | Color                      | Hex       |
| ------------------------- | -------------------------- | --------- |
| Level 0 — Foundation      | Maroon (brand dark)        | `#831742` |
| Level 1 — Core dependents | Magenta (brand primary)    | `#d61572` |
| Level 2 — Application     | Amber (brand accent)       | `#b97800` |
| API / Host layer          | Sky blue (brand secondary) | `#0a7db5` |
| Success / positive        | Brand green                | `#0a8f64` |

---

## Package Dependency Map

How the 15 NuGet packages depend on each other — built from actual `.csproj` `<PackageReference Include="Acontplus.*">` entries.

```mermaid
---
config:
  htmlLabels: false
---
flowchart TD
  subgraph l0["`**Level 0** — no internal dependencies`"]
    direction LR
    Core["`**Acontplus.Core**
    Result&lt;T&gt;, DDD, Enums
    Specs, Validation`"]
    Barcode["`**Acontplus.Barcode**
    QR, Code128, ZXing
    SkiaSharp rendering`"]
    Logging["`**Acontplus.Logging**
    Serilog, OpenTelemetry
    Jaeger, Prometheus`"]
    ApiDocs["`**Acontplus.ApiDocumentation**
    Swagger, Versioning
    OpenAPI`"]
    S3["`**Acontplus.S3Application**
    AWS S3, Presigned URLs
    Polly resilience`"]
  end

  subgraph l1["`**Level 1** — depend on Core`"]
    direction LR
    Utilities["`**Acontplus.Utilities**
    Encryption, IO, Text
    Time, BCrypt`"]
    Infrastructure["`**Acontplus.Infrastructure**
    Caching, Redis, Resilience
    Middleware, HealthChecks`"]
    Services["`**Acontplus.Services**
    JWT Auth, User Context
    Security Headers`"]
    PersCommon["`**Acontplus.Persistence.Common**
    Repository abstractions
    EF Core, DbContextFactory`"]
  end

  subgraph l2["`**Level 2** — depend on Level 1`"]
    direction LR
    Analytics["`**Acontplus.Analytics**
    Metrics, KPIs
    Business Intelligence`"]
    Notifications["`**Acontplus.Notifications**
    Email SMTP/SES
    WhatsApp Cloud API`"]
    Billing["`**Acontplus.Billing**
    SRI Electronic Invoicing
    XAdES-BES Signature`"]
    Reports["`**Acontplus.Reports**
    RDLC, QuestPDF
    Excel MiniExcel/ClosedXML`"]
    PersSQL["`**Acontplus.Persistence
    .SqlServer**
    EF Core + ADO.NET`"]
    PersPG["`**Acontplus.Persistence
    .PostgreSQL**
    EF Core + Npgsql`"]
  end

  Core --> Utilities
  Core --> Infrastructure
  Core --> Services
  Core --> PersCommon
  Barcode --> Billing
  Barcode --> Reports
  Utilities --> Analytics
  Utilities --> Notifications
  Utilities --> Billing
  Utilities --> Reports
  PersCommon --> PersSQL
  PersCommon --> PersPG

  classDef l0 fill:#831742,color:#fff,stroke:#6a1235
  classDef l1 fill:#d61572,color:#fff,stroke:#b01260
  classDef l2 fill:#b97800,color:#fff,stroke:#9a6400
  class Core,Barcode,Logging,ApiDocs,S3 l0
  class Utilities,Infrastructure,Services,PersCommon l1
  class Analytics,Notifications,Billing,Reports,PersSQL,PersPG l2
```

### Key observations

- **Core** and **Barcode** are fully independent — zero internal deps. Safe to install in any host (console, worker, API).
- **Billing** depends on `Utilities + Barcode` — not Core directly (Utilities transitively brings Core).
- **Reports** depends on `Utilities + Barcode` — same level as Billing, not a deeper tier.
- **Logging, ApiDocumentation, S3Application** are standalone — install without pulling any other Acontplus package.
- **Persistence.SqlServer** and **Persistence.PostgreSQL** are parallel — never reference both in the same project.

---

## Demo Application — Clean Architecture Layers

How `apps/src/Demo.*` maps to DDD layers and which packages each layer consumes.

```mermaid
---
config:
  htmlLabels: false
---
flowchart TD
  subgraph api["`**API Layer**
  Demo.Api`"]
    ep[Minimal API Endpoints]
    prog[Program.cs / DI]
  end

  subgraph app["`**Application Layer**
  Demo.Application`"]
    svc[Application Services]
    dtos[DTOs / Interfaces]
  end

  subgraph domain["`**Domain Layer**
  Demo.Domain`"]
    ent[Entities / Aggregates]
    ev[Domain Events]
  end

  subgraph infra["`**Infrastructure Layer**
  Demo.Infrastructure`"]
    repo[EF Core Repositories]
    evh[Event Handlers]
  end

  subgraph pkgs["`**Acontplus Packages**`"]
    direction LR
    p1[Core]
    p2[Services]
    p3[Infrastructure]
    p4[Logging]
    p5[ApiDocumentation]
    p6[Persistence.SqlServer]
    p7[Notifications]
    p8[Reports]
    p9[Billing]
  end

  api --> app
  app --> domain
  infra --> app
  infra --> domain

  api -.->|uses| p2
  api -.->|uses| p3
  api -.->|uses| p4
  api -.->|uses| p5
  api -.->|uses| p8
  api -.->|uses| p9
  app -.->|uses| p1
  app -.->|uses| p7
  infra -.->|uses| p6
  domain -.->|uses| p1

  classDef layer fill:#831742,color:#fff,stroke:#6a1235
  classDef pkg fill:#d61572,color:#fff,stroke:#b01260

  class api,app,domain,infra layer
  class p1,p2,p3,p4,p5,p6,p7,p8,p9 pkg
```

---

## SRI Billing — Authorization Flow

The async document authorization flow required by SRI Ecuador. See [[SRI-Electronic-Billing-Spec]] for full protocol details.

```mermaid
sequenceDiagram
  autonumber
  participant App as Acontplus.Billing
  participant Sign as XAdES-BES Signer
  participant SRI_R as SRI Reception WS
  participant SRI_A as SRI Authorization WS

  App->>Sign: Sign XML with PKCS12 certificate
  Sign-->>App: Signed XML (XAdES-BES embedded)

  App->>SRI_R: validarComprobante(signedXml)
  SRI_R-->>App: RECIBIDA or DEVUELTA

  alt DEVUELTA (schema/signature error)
    App->>App: Parse error messages, fix XML, retry
  end

  loop Poll with configurable delay
    App->>SRI_A: autorizacionComprobante(claveAcceso)
    SRI_A-->>App: PPR / AUTORIZADO / RECHAZADO
  end

  alt AUTORIZADO
    App->>App: Store authorized XML, notify recipient
  else RECHAZADO
    App->>App: Reuse same claveAcceso, fix, resubmit
  end
```

---

## Infrastructure — Subsystem Map

`Acontplus.Infrastructure` bundles 5 distinct subsystems. Install it when you need any of them.

```mermaid
---
config:
  htmlLabels: false
---
flowchart LR
  subgraph infra["`**Acontplus.Infrastructure**`"]
    direction TB
    cache["`**Caching**
    IMemoryCache + Redis
    StackExchange`"]
    resilience["`**Resilience**
    Circuit Breaker
    Retry, Timeout (Polly)`"]
    http["`**HTTP Client**
    IHttpClientFactory
    Resilience policies`"]
    middleware["`**Middleware**
    Request context
    Exception handling
    Rate limiting, CSP`"]
    health["`**Health Checks**
    DB, Redis, HTTP
    Custom checks`"]
  end

  app(["`ASP.NET Core App`"]) --> infra

  classDef sub fill:#d61572,color:#fff,stroke:#b01260
  classDef host fill:#831742,color:#fff,stroke:#6a1235
  class cache,resilience,http,middleware,health sub
  class app host
```

---

## Persistence — Dual Access Pattern

Both `Persistence.SqlServer` and `Persistence.PostgreSQL` expose two access patterns. Choose based on the operation type.

```mermaid
---
config:
  htmlLabels: false
---
flowchart TD
  app(["`Application Layer`"])

  subgraph provider["`**Persistence Provider**
  SqlServer or PostgreSQL`"]
    direction TB
    subgraph ef["`**EF Core Path**`"]
      dbctx[DbContext]
      repo[Generic Repository]
      uow[Unit of Work]
    end
    subgraph ado["`**ADO.NET Path**`"]
      adorepo["`ADO Repository
      Stored Procs / Raw SQL`"]
      polly["`Polly Resilience
      Retry + Circuit Breaker`"]
    end
  end

  db[("`SQL Server
  PostgreSQL`")]

  app -->|"`CRUD, Specs
  Pagination`"| ef
  app -->|"`Bulk ops, Stored Procs
  Reports, High-throughput`"| ado
  ef --> db
  ado --> db

  classDef path fill:#d61572,color:#fff,stroke:#b01260
  classDef host fill:#831742,color:#fff,stroke:#6a1235
  classDef db fill:#b97800,color:#fff,stroke:#9a6400
  class ef,ado path
  class app host
  class db db
```

Use **EF Core** for: standard CRUD, specification queries, pagination, domain queries.
Use **ADO.NET** for: stored procedures, bulk inserts, raw SQL reports, high-throughput operations.
