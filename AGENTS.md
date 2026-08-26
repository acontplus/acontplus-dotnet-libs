# Acontplus .NET Libraries — Agent Guide

## Repository purpose

This is a .NET 10 monorepo for versioned `Acontplus.*` NuGet libraries and the demo application that exercises them. The solution is [`acontplus-dotnet-libs.slnx`](acontplus-dotnet-libs.slnx). Libraries live in `src/`; the four demo layers live in `apps/src/`; tests belong in `tests/`; durable documentation is in `docs/wiki/` and package `README.md` files.

Before changing a library, read its `.csproj`, local README, and the closest relevant code. Treat public APIs, package metadata, and dependency choices as consumer-facing compatibility boundaries.

## Project boundaries

- `Acontplus.Core` contains framework-agnostic domain primitives, result/error types, abstractions, and shared DTOs. Keep it independent of infrastructure concerns.
- `Acontplus.Utilities`, `Infrastructure`, `Services`, and provider-specific persistence packages build on those abstractions; preserve the dependency direction.
- `Acontplus.Persistence.Common` owns shared persistence contracts; SQL Server and PostgreSQL implementations depend on it.
- `Acontplus.Billing` implements Ecuadorian SRI electronic-invoicing behavior. Do not casually change XML/signature semantics or tax-document compatibility.
- `apps/src/Demo.*` is sample/integration code, not a replacement for library APIs.

Internal package dependencies are declared as `PackageReference`s. Their versions are centrally pinned in `Directory.Packages.props`; do not add a `Version` attribute to a project `PackageReference`.

## Implementation conventions

- Target `net10.0`, enable nullable reference types, and follow `.editorconfig`: 4-space C#, file-scoped namespaces, braces, PascalCase types/members, and `I`-prefixed interfaces.
- Preserve the existing folder-oriented namespaces and `GlobalUsings.cs` pattern. Prefer focused public contracts and DI registration extensions (`AddXxx`) for services.
- Public libraries generate XML documentation. Add meaningful XML docs for new public APIs; do not add blanket warning suppressions to avoid documenting an API.
- Use async APIs end-to-end, accept and pass through `CancellationToken` when the surrounding API does, and avoid blocking async work.
- Do not add a `FrameworkReference` or third-party dependency without checking its effect on NuGet consumers. A framework reference makes ASP.NET Core a consumer requirement.
- Keep compatibility in mind: preserve public signatures and serialized/XML contracts unless the requested change explicitly permits a breaking release.

## Build, test, and package

CI uses .NET SDK `10.0.x` and Release configuration. Restore sources are intentionally limited to NuGet.org by `Nuget.config`.

```bash
dotnet restore
dotnet build acontplus-dotnet-libs.slnx --configuration Release --no-restore
dotnet test acontplus-dotnet-libs.slnx --configuration Release --no-build --verbosity normal
dotnet pack acontplus-dotnet-libs.slnx --configuration Release --no-build --output nupkgs
```

For a focused change, build and test the affected project first. Run the full solution build for cross-package, packaging, dependency, or solution changes. Do not hand-edit `bin/`, `obj/`, generated EF migrations/designer files, or package artifacts unless the task explicitly requires it.

Tests use xUnit. Put them under `tests/Acontplus.<Name>.Tests/`, mirror the production folder, name files `<ClassName>Tests.cs`, and name tests `<Method>_<Condition>_<ExpectedOutcome>`. Add a new test project to the `.slnx` and add test package versions centrally when needed.

## Packages, versions, and documentation

- Each distributable library owns its package `<Version>` in its `.csproj`; use SemVer. A major version bump also requires evaluating `AssemblyVersion`.
- When an internal package version changes, update its entry in `Directory.Packages.props` and evaluate downstream package references using the dependency order in the root `README.md` / `docs/wiki` publishing guides.
- Do not publish, push packages, trigger release workflows, or modify secrets without explicit user authorization.
- Record released changes under the appropriate package heading in root `CHANGELOG.md`; newest versions go first.
- READMEs are evergreen usage documentation. Do not add “What’s New,” version-stamped feature lists, or change history to them; use `CHANGELOG.md` instead. Follow `.github/instructions/readme.instructions.md` for README edits.

## Change discipline

- Inspect `git status` before editing and preserve unrelated user changes.
- Keep diffs scoped; do not reformat unrelated files or upgrade dependencies opportunistically.
- Update or add tests whenever behavior changes, especially public APIs, validation, security, persistence, billing, or serialization behavior.
- Workflows in `.github/workflows/` are authoritative for CI/release behavior. Consult `docs/wiki/Smart-Publish-Guide.md` before changing versions or release automation.
- Suggested commits follow Conventional Commits. Use the repository scopes and rules in `.github/instructions/commits.instructions.md`; format is `type(scope): description` and descriptions must be concise.

## OpenCode configuration

- `opencode.json` (checked in) sets project-level agent guardrails: read-only `git` and non-mutating `dotnet` CLI commands (restore/build/test/pack/format) run without approval; commits, pushes, `dotnet nuget push`, and `gh` always prompt; force-pushes and catastrophic `rm -rf` are denied.
- The `microsoft-learn` MCP server is configured for official Microsoft/.NET documentation. When uncertain about a .NET API, NuGet behavior, or framework guidance, query its tools instead of guessing.
- `.opencode/commands/verify.md` provides `/verify`, the CI-parity restore → Release build → test check.
- Config is loaded at startup: restart opencode after editing `opencode.json` or files under `.opencode/`.
