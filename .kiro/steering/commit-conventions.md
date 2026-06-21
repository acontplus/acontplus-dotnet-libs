---
inclusion: always
---

# Commit Message Conventions

When generating commit messages, follow these rules strictly.

## Format

```
<type>(<scope>): <short description>
```

Single line only — no body, no bullet points, no additional paragraphs. Maximum 72 characters total.

## Types

- `feat` — new features or significant additions
- `fix` — bug fixes
- `docs` — documentation changes only
- `style` — code style/formatting, no logic changes
- `refactor` — code restructuring, no functionality changes
- `test` — adding or modifying tests
- `chore` — maintenance, build, tooling updates
- `perf` — performance improvements
- `ci` — CI/CD configuration changes
- `build` — build system or dependency changes
- `revert` — reverting previous commits

## Rules

1. Use the **imperative mood**: "add feature" not "added feature"
2. Do NOT end with a period
3. Maximum 72 characters total
4. **No emoji** — plain text only
5. **No line breaks**
6. Description must be between 1–50 characters after type and scope
7. Breaking changes: use `feat!:` or include `BREAKING CHANGE:` in footer

## Scope

Use the relevant package or layer as the scope. See the Commit Scope Reference table in `project-overview.md`.

Key scopes: `core`, `utilities`, `billing`, `notifications`, `reports`, `persistence`, `persistence-sqlserver`, `persistence-postgresql`, `services`, `api-docs`, `logging`, `barcode`, `s3`, `analytics`, `infrastructure`, `demo-api`, `build`, `ci`, `docs`, `config`, `scripts`, `deps`

Omit scope only when the change spans the entire workspace.

## Validation Regex

```regex
^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\(.+\))?(!)?: .{1,50}
```

## Valid Examples

```
feat(core): add success message to Result type
feat(billing): add SRI electronic signature support
fix(persistence): correct transaction handling in SaveChanges
fix(notifications): handle null template variables
docs(reports): update RDLC usage examples
refactor(utilities): simplify encryption service interface
chore(build): upgrade to .NET 10.0
test(core): add unit tests for Result pattern
perf(persistence-sqlserver): optimize bulk insert operations
build(deps): update NuGet dependencies
ci: add automated package publishing workflow
```

## Invalid Examples (never generate these)

```
❌ Refactor workspace verification script and tidy TypeScript configuration
❌ feat(ng-auth): add JWT refresh 🔐
❌ fix: resolve issue

   - Updated logic
   - Fixed bug
❌ feat(Acontplus.Core): use full package name instead of scope
❌ chore(test_api): use kebab-case not snake_case
```
