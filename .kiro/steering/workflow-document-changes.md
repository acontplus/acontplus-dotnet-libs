---
inclusion: auto
name: workflow-document-changes
description: Document package changes for Acontplus libraries. Use when asked to document changes, write a changelog, update release notes, or record what changed in a library.
---

# Workflow: Document New Changes

Interview the author about recent changes and produce a structured changelog entry plus updated `<PackageReleaseNotes>` in the affected `.csproj`.

## Step 1 — Interview the Author

Ask these questions **one at a time**, waiting for each answer before continuing:

1. **Who made these changes?**
   e.g. "Ivan Paz", "Team backend", "PR #42 by @contributor"

2. **Which library or libraries were changed?**
   e.g. `Acontplus.Notifications`, `Acontplus.Core`, multiple packages

3. **What is the new version number for each affected package?**
   e.g. `Acontplus.Notifications` → `2.1.0`

4. **What changed?** _(list each change separately)_
   - New features added
   - Bugs fixed
   - Breaking changes (if any)
   - Performance improvements
   - Deprecations

5. **Why were these changes made?** _(motivation or business reason)_
   e.g. "Required by SRI regulation update", "Performance bottleneck under load"

6. **Are there any breaking changes?**
   If yes: what is the migration path?

7. **Any related issues, PRs, or external references?**
   e.g. GitHub issue #45, external spec URL

---

## Step 2 — Generate Changelog Entry

**Where changelogs live — no duplication:**

- `CHANGELOG.md` (repo root) — full versioned history for all packages. **Only place** for change history.
- `src/Acontplus.<Name>/README.md` — current capabilities only, no version history.
- `docs/wiki/*.md` — cross-cutting guides only.

**Before writing anything**, read `CHANGELOG.md` to understand its structure and verify the version doesn't already exist.

Structure to match exactly:

```markdown
## Acontplus.<Name>

### [<Version>] — <YYYY-MM-DD> by <Author>

> Reason: <Why this change was made>

#### Added

- Description of new feature (#N)

#### Changed

- Description of behavioral change

#### Fixed

- Description of bug fix

#### Removed / Deprecated

- Description of removed/deprecated API

#### Breaking Changes

> ⚠️ **Breaking**: Description of breaking change.
> **Migration**: How to update consuming code.
```

**Insertion rules:**

- If the package section already exists: insert new `### [<Version>]` immediately after `## Acontplus.<Name>` (newest first)
- If the package section doesn't exist: create it at the top of the file, below the header
- Omit empty subsections
- Use today's date in `YYYY-MM-DD` format
- Each bullet starts with a past-tense verb: "Added", "Fixed", "Removed", "Improved"
- Reference issue/PR numbers where provided: `(#42)`

---

## Step 3 — Update `<PackageReleaseNotes>` in `.csproj`

```xml
<PackageReleaseNotes>
v<Version> (<YYYY-MM-DD>) by <Author>
Reason: <Why>

- Added: <feature>
- Fixed: <bug>
- Changed: <behavior>
- Breaking: <breaking change and migration note>
</PackageReleaseNotes>
```

Keep it concise — max ~10 bullet points. NuGet shows this on the package page.

---

## Step 4 — Update `<Version>` in `.csproj`

| Change type                      | Version bump                                  |
| -------------------------------- | --------------------------------------------- |
| Breaking change                  | MAJOR (x.0.0) — also update `AssemblyVersion` |
| New feature, backward-compatible | MINOR (x.y.0)                                 |
| Bug fix, no new API              | PATCH (x.y.z)                                 |

Also update `<PackageVersion Include="Acontplus.<Name>" Version="<new>" />` in `Directory.Packages.props`.

---

## Step 5 — Suggest Commit Message

Produce a single-line Conventional Commits message following `.kiro/steering/commit-conventions.md`:

```
feat(notifications): add WhatsApp template message support
```

---

## Step 6 — Summary Output

Present a summary table:

| Library                 | Old Version | New Version | Change Type | Author   | CHANGELOG updated |
| ----------------------- | ----------- | ----------- | ----------- | -------- | ----------------- |
| Acontplus.Notifications | 2.0.3       | 2.1.0       | feat        | Ivan Paz | ✅ yes            |

And list modified files:

- `src/Acontplus.<Name>/Acontplus.<Name>.csproj` — version + release notes
- `CHANGELOG.md` — new `### [<Version>]` block inserted
- `Directory.Packages.props` — `<PackageVersion>` entry updated (if applicable)

---

## Step 7 — Verify Build

Run `dotnet build acontplus-dotnet-libs.slnx` and confirm **0 errors** before finalising.
