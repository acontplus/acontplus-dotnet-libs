---
applyTo: "**/README.md"
---

# README Authoring Guidelines

Apply these rules whenever creating or editing any `README.md` in this repository.

## Core Principle: READMEs Describe, CHANGELOGs Record

A README is **evergreen documentation** — it describes what the library _is_ and _does_ today.
A CHANGELOG records _what changed and when_. Keep them separate.

## Prohibited Content in READMEs

- **"What's New" sections** — belongs in `CHANGELOG.md`, not README.
- **Version-stamped headings** — never write `### Feature X (NEW in v1.2.1+)` or `## Added in v2`.
- **Breaking changes lists** — migration notes belong in `CHANGELOG.md`. Upgrade guides (how to migrate, not what changed) may remain.
- **Version-specific bullet lists** at the top of the file listing recent additions.
- **"Latest version" sections** — no `## What's New (Latest Version)`.

## Correct Approach

### Features Section

Describe current capabilities without referencing which version introduced them.

```markdown
## Features <!-- or  ## 🚀 Features -->

### Core Capabilities

- ✅ **Feature A** - What it does
- ✅ **Feature B** - What it does
```

### CHANGELOG pointer

When removing a "What's New" block, replace it with a single CHANGELOG link:

```markdown
> Version history: [CHANGELOG.md](../../CHANGELOG.md)
```

### CHANGELOG entry (root `CHANGELOG.md`)

Add the removed version-specific content there instead:

```markdown
## Acontplus.PackageName

### [X.Y.Z]

- **Added** ...
- **Changed** ...
- **Fixed** ...
```

## Section Heading Rules

| ❌ Avoid                            | ✅ Use instead                    |
| ----------------------------------- | --------------------------------- |
| `## 🚀 What's New (Latest Version)` | `## 🚀 Features`                  |
| `## .NET 10 Features`               | `## Features` or `## 🚀 Features` |
| `### Event Bus (NEW in v1.2.1+)`    | `### Event Bus`                   |
| `## Breaking Changes` (top-level)   | Entry in `CHANGELOG.md`           |

## Standard README Structure

Every package README should follow this order:

1. Title + badges
2. One-paragraph description
3. `> Version history: [CHANGELOG.md](../../CHANGELOG.md)` _(optional short link)_
4. `## Features` — current capabilities, no version references
5. `## Installation`
6. `## Quick Start`
7. `## Configuration`
8. `## Usage Examples`
9. `## Requirements`
10. `## Troubleshooting` _(optional)_
11. `## License`

> Upgrade guides (`## Upgrade Guide / Migrating from vX to vY`) are acceptable in the README
> because they describe _how_ to migrate, not _what_ changed — but keep them brief and
> move detailed change lists to CHANGELOG.
