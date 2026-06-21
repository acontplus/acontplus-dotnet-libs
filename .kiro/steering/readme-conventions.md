---
inclusion: fileMatch
fileMatchPattern: "**/README.md"
---

# README Authoring Guidelines

Apply these rules whenever creating or editing any `README.md` in this repository.

## Core Principle: READMEs Describe, CHANGELOGs Record

A README is **evergreen documentation** — it describes what the library _is_ and _does_ today.
A CHANGELOG records _what changed and when_. Keep them strictly separate.

## Prohibited Content in READMEs

- **"What's New" sections** stamped with version numbers — belongs in `CHANGELOG.md`
- **Version-stamped headings** — never write `### Feature X (NEW in v1.2.1+)` or `## Added in v2`
- **Breaking changes lists** — migration notes belong in `CHANGELOG.md`
- **Version-specific bullet lists** at the top listing recent additions
- **"Latest version" sections** — no `## What's New (Latest Version)`

## Correct Approach

### Features Section

Describe current capabilities without referencing which version introduced them.

```markdown
## Features

### Core Capabilities

- ✅ **Feature A** - What it does
- ✅ **Feature B** - What it does
```

### CHANGELOG pointer

When removing a "What's New" block, replace it with:

```markdown
> Version history: [CHANGELOG.md](../../CHANGELOG.md)
```

## Section Heading Rules

| ❌ Avoid                            | ✅ Use instead          |
| ----------------------------------- | ----------------------- |
| `## 🚀 What's New (Latest Version)` | `## 🚀 Features`        |
| `## .NET 10 Features`               | `## Features`           |
| `### Event Bus (NEW in v1.2.1+)`    | `### Event Bus`         |
| `## Breaking Changes` (top-level)   | Entry in `CHANGELOG.md` |

## Standard README Structure

Every package README must follow this order:

1. Title + badges (NuGet, .NET, License)
2. One-paragraph description
3. `> Version history: [CHANGELOG.md](../../CHANGELOG.md)` _(optional)_
4. `## Features` — current capabilities, no version references
5. `## Installation`
6. `## Quick Start`
7. `## Configuration`
8. `## Usage Examples`
9. `## Requirements`
10. `## Troubleshooting` _(optional)_
11. `## License`

> Upgrade guides (`## Upgrade Guide / Migrating from vX to vY`) are acceptable because they
> describe _how_ to migrate, not _what_ changed. Keep them brief and move change details to CHANGELOG.

## Badge Template

```markdown
[![NuGet](https://img.shields.io/nuget/v/Acontplus.<Name>.svg)](https://www.nuget.org/packages/Acontplus.<Name>)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
```
