---
name: document-changes
description: Document package changes by interviewing the author, then produce a CHANGELOG entry, updated PackageReleaseNotes in .csproj, and a conventional commit message. Use when finishing work on an Acontplus library.
---

## Process

### Step 1 — Interview

Ask these questions **one at a time**, waiting for each answer:

1. **Who made these changes?** e.g. "Ivan Paz", "PR #42"
2. **Which library or libraries were changed?** e.g. `Acontplus.Notifications`
3. **New version number for each affected package?** e.g. `2.1.0`
4. **What changed?** New features / Bug fixes / Breaking changes / Performance / Deprecations
5. **Why?** The motivation or business reason
6. **Breaking changes?** If yes: migration path?
7. **Related issues or PRs?** e.g. `#45`

---

### Step 2 — Update CHANGELOG.md

Read `CHANGELOG.md` first. Match this exact structure:

```markdown
## Acontplus.<Name>

### [<Version>] — <YYYY-MM-DD> by <Author>

> Reason: <Why>

#### Added

- Past-tense description (#N)

#### Changed / Fixed / Removed

- ...

#### Breaking Changes

> ⚠️ **Breaking**: description.
> **Migration**: how to update consuming code.
```

Rules:

- If `## Acontplus.<Name>` exists → insert new `### [<Version>]` immediately after it (newest first)
- If section doesn't exist → create it at the top of the file, below the header
- Omit empty subsections
- Use today's date `YYYY-MM-DD`

---

### Step 3 — Update `.csproj`

Update `<PackageReleaseNotes>` (max ~10 bullets):

```xml
<PackageReleaseNotes>
v<Version> (<YYYY-MM-DD>) by <Author>
Reason: <Why>

- Added: <feature>
- Fixed: <bug>
- Breaking: <change and migration>
</PackageReleaseNotes>
```

Semver rules for `<Version>`:

- Breaking change → MAJOR (also update `<AssemblyVersion>`)
- New feature → MINOR
- Bug fix → PATCH

---

### Step 4 — Update Directory.Packages.props

Update `<PackageVersion Include="Acontplus.<Name>" Version="<new>" />` to match.

---

### Step 5 — Suggest Commit Message

Single line, ≤ 72 chars, Conventional Commits per `commit-conventions.md` steering:

```
feat(notifications): add WhatsApp template message support
```

---

### Step 6 — Summary

| Library     | Old → New     | Type | Author   | CHANGELOG |
| ----------- | ------------- | ---- | -------- | --------- |
| Acontplus.X | 2.0.3 → 2.1.0 | feat | Ivan Paz | ✅        |

---

### Step 7 — Verify

```bash
dotnet build acontplus-dotnet-libs.slnx
```

Confirm **0 errors** before finishing.
