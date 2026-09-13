---
name: dotnet-skills-sync
description: >-
  Sync and update the locally adapted dotnet/skills from the upstream
  Microsoft dotnet/skills repository. Use when Microsoft publishes new
  versions of their .NET agent skills and you want to pull updates into
  the Acontplus .NET Libraries workspace while preserving Acontplus-specific adaptations.
---

# Sync dotnet/skills from Upstream

## When to Use

Activate this skill when:

* Microsoft has published updates to [dotnet/skills](https://github.com/dotnet/skills).
* You want to check for new skills or updated content.
* A new .NET version is released and skills may have been updated with new patterns.

## Mapping: Upstream → Local

| Upstream Plugin / Skill | Local Skill |
|:------------------------|:------------|
| `plugins/dotnet/skills/setup-local-sdk` | `.agents/skills/dotnet-setup-sdk` |
| `plugins/dotnet-data/skills/optimizing-ef-core-queries` | `.agents/skills/dotnet-ef-optimization` |
| `plugins/dotnet-data/skills/create-datadriven-aspnetcore` | `.agents/skills/dotnet-data-driven-api` |
| `plugins/dotnet-test/skills/run-tests` | `.agents/skills/dotnet-test-execution` |
| `plugins/dotnet-test/skills/dotnet-test-generation` | `.agents/skills/dotnet-test-generation` |
| `plugins/dotnet-msbuild/skills/binlog-failure-analysis` | `.agents/skills/dotnet-build-diagnosis` |
| `plugins/dotnet-nuget/skills/convert-to-cpm` | `.agents/skills/dotnet-nuget-management` |
| `plugins/dotnet-advanced/skills/resolve-aspnetcore-startup-failures` | `.agents/skills/dotnet-aspnetcore-diagnosis` |
| `plugins/dotnet-upgrade/skills/dotnet-aot-compat` + `upgrade-assistant` | `.agents/skills/dotnet-upgrade-guide` |

## Sync Workflow

### Step 1: Fetch Upstream Changes

For each mapped skill, fetch the latest SKILL.md from GitHub raw content:

```
https://raw.githubusercontent.com/dotnet/skills/main/plugins/<plugin>/skills/<skill>/SKILL.md
```

### Step 2: Diff and Merge

For each upstream SKILL.md:
1. Read the upstream content.
2. Read the local adapted version.
3. Identify **new sections, updated code examples, or new strategies** in the upstream.
4. Merge new content into the local version while **preserving Acontplus-specific adaptations**:
   - Project paths (`acontplus-dotnet-libs.slnx`, `tests/Acontplus.*.Tests.Unit/`, etc.)
   - Architecture patterns (`Result<T, DomainError>`, `IError`, NuGet library boundaries)
   - Database specifics (`Acontplus.Persistence.Common`, PostgreSQL, SQL Server)
   - Testing stack (xUnit, Moq, Microsoft.Testing.Platform)
   - Package management (`Directory.Packages.props`, `dotnet pack`, SemVer)
   - Distribution rules (`Nuget.config`, `apps/src/Demo.*`)

### Step 3: Check for New Skills

Also check for **new skills** that don't yet exist locally:

```
https://api.github.com/repos/dotnet/skills/contents/plugins/<plugin>/skills
```

Relevant plugins to monitor:
- `dotnet`, `dotnet-data`, `dotnet-test`, `dotnet-msbuild`
- `dotnet-nuget`, `dotnet-advanced`, `dotnet-upgrade`

If a new skill is found that's relevant to Acontplus .NET Libraries, create a new
`.agents/skills/<name>/SKILL.md` adapted to Acontplus conventions.

### Step 4: Verify

After updating, confirm skills load correctly by checking the skill list
in Antigravity and verifying frontmatter (`name` and `description` fields).
