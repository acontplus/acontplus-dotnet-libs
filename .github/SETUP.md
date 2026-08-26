# GitHub Actions and NuGet Setup

## Prerequisites

- GitHub Actions is enabled for the repository.
- `main` and `develop` are protected and changes are merged through pull requests.
- The NuGet.org owner has a Trusted Publishing policy for this repository.

## Configure NuGet Trusted Publishing

In NuGet.org, create a GitHub Trusted Publishing policy with:

| Setting | Value |
| --- | --- |
| Repository owner | `acontplus` |
| Repository | `acontplus-dotnet-libs` |
| Workflow file | `smart-publish.yml` |
| Environment | `production` |

Add the repository secret `NUGET_USER` with the NuGet.org profile name that owns the policy. The workflow uses OIDC through `NuGet/login@v1`; no persistent `NUGET_API_KEY` is required.

Follow [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) for the authoritative setup instructions.

## Release a Package Set

1. Update each released package's `<Version>` in its `.csproj`.
2. Update the corresponding internal version in `Directory.Packages.props`.
3. Include every changed Acontplus dependency reference and bump every consumer released with it.
4. Run the local Release restore, build, and tests.
5. Open and merge a PR to `main`.

`build-test.yml` validates pull requests to `main` and `develop`; it is intentionally not triggered by direct pushes. After merge to `main`, `smart-publish.yml` publishes changed package versions, verifies indexing, and creates a GitHub Release.

## Workflow Inventory

| File | Purpose | Trigger |
| --- | --- | --- |
| `smart-publish.yml` | Publish the merged, versioned release set through OIDC. | Merged PR to `main` changing a package project |
| `build-test.yml` | CI build, test, pack validation, and Cobertura artifacts. | Pull requests to `main`/`develop` and manual runs |
| `version-check.yml` | Detect local versions that are not published on NuGet.org. | Daily and manual |
| `publish-wiki.yml` | Publish the documentation wiki. | Wiki documentation changes |

## Verification

For a normal change, use:

```bash
dotnet restore
dotnet build acontplus-dotnet-libs.slnx --configuration Release --no-restore
dotnet test --solution acontplus-dotnet-libs.slnx --configuration Release --no-build --verbosity normal
```

For coverage of a test project, add `--coverage --coverage-output <path>.cobertura.xml --coverage-output-format cobertura`.

## Troubleshooting

| Problem | Resolution |
| --- | --- |
| OIDC authentication fails | Verify the NuGet policy matches `smart-publish.yml`, repository, owner, and `production` environment. |
| A package is not published | Confirm the merged PR changed its package `.csproj` version. |
| Consumer restore fails | Update the consumer's central package reference and release version in the same PR. |
| Version exists already | Use a new SemVer version; NuGet versions are immutable. |
