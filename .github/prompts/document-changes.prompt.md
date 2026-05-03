---
agent: agent
description: Document new changes by interviewing the author about what changed and why, then produce changelog and release notes
tools:
  - read_file
  - replace_string_in_file
  - create_file
  - file_search
  - get_changed_files
---

# Document New Changes

Interview the author about recent changes and produce a structured changelog entry plus updated `<PackageReleaseNotes>` in the affected `.csproj`.

---

## Step 1 — Interview the Author

Ask the following questions **one at a time** and wait for the answer before continuing:

1. **Who made these changes?**
   - e.g. "Ivan Paz", "Team backend", "PR #42 by @contributor"

2. **Which library or libraries were changed?**
   - e.g. `Acontplus.Notifications`, `Acontplus.Core`, multiple packages

3. **What is the new version number for each affected package?**
   - e.g. `Acontplus.Notifications` → `2.1.0`

4. **What changed?** _(list each change separately)_
   - New features added
   - Bugs fixed
   - Breaking changes (if any)
   - Performance improvements
   - Deprecations

5. **Why were these changes made?** _(the motivation or business reason)_
   - e.g. "Required by SRI regulation update", "Performance bottleneck under load", "User request from #issue-123"

6. **Are there any breaking changes?**
   - If yes: what is the migration path?

7. **Any related issues, PRs, or external references?**
   - e.g. GitHub issue #45, external spec URL

---

## Step 2 — Generate Changelog Entry

> **Where changelogs live — no duplication:**
>
> - `CHANGELOG.md` (repo root) — full versioned history for all packages. **This is the only place** for change history.
> - `src/Acontplus.<Name>/README.md` — only a short "What's New" section for the **current** version. Do NOT accumulate history there.
> - `docs/wiki/Home.md` — navigation index only; never add changelog content here.
> - `docs/wiki/*.md` — cross-cutting guides (architecture, resilience, migrations). Add a new guide here only for significant architectural or migration changes.

**Before writing anything**, read `CHANGELOG.md` to understand its current structure and check whether:

1. A section `## Acontplus.<Name>` already exists for the affected package.
2. The version being added does not already exist as `### [<Version>]` under that section (skip if it does).

The file uses this structure — match it exactly:

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

- If the package section (`## Acontplus.<Name>`) already exists: insert the new `### [<Version>]` block immediately after the `## Acontplus.<Name>` heading (newest version first within the section).
- If the package section does not exist yet: create it at the top of the file, just below the file header (before any other `## Acontplus.*` sections).
- Omit empty subsections (Added / Changed / Fixed, etc.).
- Use today's date in `YYYY-MM-DD` format.
- Each bullet starts with a past-tense verb: "Added", "Fixed", "Removed", "Improved".
- Reference issue/PR numbers where provided: `(#42)`.
- Mark breaking changes with the `> ⚠️ **Breaking**:` callout.

---

## Step 3 — Update `<PackageReleaseNotes>` in `.csproj`

For each affected library, update the `<PackageReleaseNotes>` element in `src/Acontplus.<Name>/Acontplus.<Name>.csproj`:

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

Keep it concise — NuGet shows this on the package page. Max ~10 bullet points.

---

## Step 4 — Update `<Version>` in `.csproj`

For each affected library, update the `<Version>` tag following Semantic Versioning:

| Change type                      | Version bump  |
| -------------------------------- | ------------- |
| Breaking change                  | MAJOR (x.0.0) |
| New feature, backward-compatible | MINOR (x.y.0) |
| Bug fix, no new API              | PATCH (x.y.z) |

Also update `AssemblyVersion` if the **major** version changed.

---

## Step 5 — Suggest Commit Message

Using the information collected, produce a commit message that:

- Follows Conventional Commits format from `.github/instructions/commits.instructions.md`
- Is a single line, ≤ 72 characters
- Uses the correct scope from the instructions file

Example output:

```
feat(notifications): add WhatsApp template message support
```

---

## Step 6 — Summary Output

After all edits, present a summary table:

| Library                 | Old Version | New Version | Change Type | Author   | CHANGELOG updated |
| ----------------------- | ----------- | ----------- | ----------- | -------- | ----------------- |
| Acontplus.Notifications | 2.0.3       | 2.1.0       | feat        | Ivan Paz | ✅ yes            |

And list the files modified:

- `src/Acontplus.<Name>/Acontplus.<Name>.csproj` — version + release notes updated
- `CHANGELOG.md` — new `### [<Version>]` block inserted under `## Acontplus.<Name>` (or "skipped — version already present")
- `src/Acontplus.<Name>/README.md` — "What's New" section updated to current version only
- `docs/wiki/*.md` — only if a new cross-cutting guide was needed (breaking changes, migrations)
