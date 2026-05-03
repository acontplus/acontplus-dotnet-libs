# NuGet Publishing Workflow Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│              AUTOMATED PUBLISHING FLOW (smart-publish.yml)                  │
└─────────────────────────────────────────────────────────────────────────────┘

    Developer Actions                GitHub Actions                NuGet.org
    ─────────────────                ──────────────                ─────────

┌──────────────────┐
│ 1. Update Version│
│ in .csproj       │
│   <Version>      │
│     2.1.0        │
│   </Version>     │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ 2. Commit + PR   │
│ git commit -m    │
│ "feat: xyz"      │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ 3. Merge PR to   │              ┌─────────────────────┐
│    main          │──────────────▶│  TRIGGER DETECTED   │
└──────────────────┘              │  PR merged to main  │
                                  │  with .csproj change│
                                  └──────────┬──────────┘
                                            │
                                            ▼
                                  ┌─────────────────────┐
                                  │ ANALYZE CHANGES     │
                                  │ ─────────────────   │
                                  │ • Scan all .csproj  │
                                  │ • Compare NuGet.org │
                                  │ • Build dep graph   │
                                  └──────────┬──────────┘
                                            │
                          ┌─────────────────┴─────────────────┐
                          │       Has dependents?              │
                          └────┬──────────────────────┬────────┘
                          NO   │                      │  YES
                               ▼                      ▼
                    ┌──────────────────┐   ┌──────────────────┐
                    │ BUILD & TEST     │   │ OPEN ISSUE       │
                    │ PUBLISH directly │   │ recommend        │
                    │ to NuGet.org     │   │ cascade-         │
                    │ CREATE RELEASE   │   │ publish.yml      │
                    └──────────────────┘   └──────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                         PARALLEL PROCESSING                                  │
└─────────────────────────────────────────────────────────────────────────────┘

When multiple packages have version changes:

    ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
    │ Package A    │     │ Package B    │     │ Package C    │
    │ v2.1.0       │     │ v1.5.3       │     │ v3.0.0       │
    └──────┬───────┘     └──────┬───────┘     └──────┬───────┘
           │                    │                    │
           ▼                    ▼                    ▼
    ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
    │ Build & Pack │     │ Build & Pack │     │ Build & Pack │
    └──────┬───────┘     └──────┬───────┘     └──────┬───────┘
           │                    │                    │
           ▼                    ▼                    ▼
    ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
    │ Publish      │     │ Publish      │     │ Publish      │
    └──────┬───────┘     └──────┬───────┘     └──────┬───────┘
           │                    │                    │
           └────────────────────┴────────────────────┘
                                │
                                ▼
                        ┌──────────────┐
                        │ Create       │
                        │ Single       │
                        │ Release      │
                        └──────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                         VERSION DETECTION LOGIC                              │
└─────────────────────────────────────────────────────────────────────────────┘

For each .csproj file:

    Read Local Version
           │
           ▼
    ┌──────────────────┐
    │ Query NuGet.org  │
    │ API for package  │
    └────────┬─────────┘
             │
             ▼
    ┌─────────────────────────────┐
    │ Does package exist?         │
    └────┬───────────────┬────────┘
         │ No            │ Yes
         ▼               ▼
    ┌─────────┐    ┌─────────────────────┐
    │ NEW     │    │ Compare version     │
    │ PACKAGE │    │ with published list │
    └────┬────┘    └──────────┬──────────┘
         │                    │
         │         ┌──────────┴───────────┐
         │         │ Exists?              │
         │         ├──────────┬───────────┤
         │         │ No       │ Yes       │
         │         ▼          ▼           │
         │    ┌────────┐  ┌───────┐     │
         │    │ NEW    │  │ SKIP  │     │
         │    │ VERSION│  └───────┘     │
         │    └────┬───┘                │
         │         │                    │
         └─────────┴────────────────────┘
                   │
                   ▼
            ┌─────────────┐
            │ PUBLISH     │
            └─────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                         DAILY VERSION CHECK                                  │
└─────────────────────────────────────────────────────────────────────────────┘

    Every day at 9 AM UTC
           │
           ▼
    ┌──────────────────┐
    │ Scan all .csproj │
    └────────┬─────────┘
             │
             ▼
    ┌──────────────────┐
    │ Compare versions │
    │ with NuGet.org   │
    └────────┬─────────┘
             │
             ▼
    ┌──────────────────────┐
    │ Generate report:     │
    │ • Published ✅       │
    │ • Unpublished 📦     │
    │ • New packages 🆕    │
    └────────┬─────────────┘
             │
             ▼
    ┌──────────────────────┐
    │ Unpublished found?   │
    └────┬───────────┬─────┘
         │ Yes       │ No
         ▼           ▼
    ┌─────────┐  ┌────────┐
    │ Create  │  │ Report │
    │ GitHub  │  │ only   │
    │ Issue   │  └────────┘
    └─────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                         ERROR HANDLING                                       │
└─────────────────────────────────────────────────────────────────────────────┘

Build Failure                Package A Success
     │                            │
     ▼                            ▼
┌─────────┐                  ┌─────────┐
│ STOP    │                  │ PUBLISH │
│ No      │                  └─────────┘
│ packages│                       │
│ published                       ▼
└─────────┘                  Package B Failure
                                  │
                                  ▼
                             ┌─────────────┐
                             │ CONTINUE    │
                             │ (fail-fast: │
                             │ false)      │
                             └──────┬──────┘
                                    ▼
                             Package C Success
                                    │
                                    ▼
                               ┌─────────┐
                               │ PUBLISH │
                               └─────────┘

Result: A ✅, B ❌, C ✅ (Partial success)
```

## Key Features

### 🎯 Automatic Detection
- Compares local `.csproj` versions with NuGet.org
- Only publishes new versions
- Skips already-published packages

### 🚀 Parallel Processing
- Multiple packages publish simultaneously
- Independent failure handling
- Faster deployment

### 🔒 Security
- API keys stored in GitHub Secrets
- Never exposed in logs
- Scoped permissions

### 📊 Monitoring
- GitHub Actions logs
- Workflow summaries
- Automatic issue creation

### 🎛️ Control
- Manual cascade publishing via `cascade-publish.yml`
- Selective package publishing with dependency ordering

## Workflow States

```
┌─────────────┐
│ IDLE        │ ← No version changes
└─────────────┘

┌─────────────┐
│ TRIGGERED   │ ← Push detected or manual start
└─────────────┘

┌─────────────┐
│ DETECTING   │ ← Scanning for changes
└─────────────┘

┌─────────────┐
│ BUILDING    │ ← Compiling packages
└─────────────┘

┌─────────────┐
│ PUBLISHING  │ ← Pushing to NuGet.org
└─────────────┘

┌─────────────┐
│ RELEASING   │ ← Creating GitHub release
└─────────────┘

┌─────────────┐
│ ✅ SUCCESS  │ ← All packages published
└─────────────┘

┌─────────────┐
│ ⚠️ PARTIAL  │ ← Some packages failed
└─────────────┘

┌─────────────┐
│ ❌ FAILED   │ ← Build or critical error
└─────────────┘
```

## Integration Points

```
┌──────────────────────────────────────────────────┐
│                                                  │
│  ┌────────────┐         ┌───────────────┐      │
│  │ GitHub     │────────▶│ GitHub Actions│      │
│  │ Repository │         │ Workflows     │      │
│  └────────────┘         └───────┬───────┘      │
│                                 │               │
│                                 │               │
│                         ┌───────┴────────┐     │
│                         │                │     │
│                         ▼                ▼     │
│                 ┌──────────────┐  ┌─────────┐ │
│                 │ NuGet.org    │  │ GitHub  │ │
│                 │ Package Feed │  │ Releases│ │
│                 └──────────────┘  └─────────┘ │
│                                                │
└──────────────────────────────────────────────────┘
```
