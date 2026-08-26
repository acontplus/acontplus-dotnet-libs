# Release Automation Checklist

## NuGet Trusted Publishing

- [ ] NuGet.org Trusted Publishing policy is active for `acontplus/acontplus-dotnet-libs`.
- [ ] The policy workflow file is `smart-publish.yml`.
- [ ] The policy environment is `production`.
- [ ] Repository secret `NUGET_USER` contains the NuGet.org profile name.
- [ ] No long-lived `NUGET_API_KEY` is required by repository workflows.

## GitHub Repository

- [ ] GitHub Actions has permission to publish releases and upload artifacts.
- [ ] The `production` environment has the required protection rules.
- [ ] `main` and `develop` require reviewed pull requests.
- [ ] Workflow files are valid YAML.

## Per-Release Pull Request

- [ ] Each released package `.csproj` contains its intended SemVer `<Version>`.
- [ ] `Directory.Packages.props` contains matching internal package versions.
- [ ] Every consumer affected by a changed Acontplus dependency has an updated reference and release version.
- [ ] Release notes are recorded in `CHANGELOG.md` when required by package policy.
- [ ] `dotnet restore`, Release build, and tests succeed.
- [ ] Package artifacts build successfully.

## CI and Coverage

- [ ] Test projects follow the `*.Tests.Unit.csproj` convention.
- [ ] `Microsoft.Testing.Extensions.CodeCoverage` is centrally versioned and referenced by test projects.
- [ ] Cobertura XML artifacts are available from `build-test.yml` when test projects run.

## After Merge

- [ ] `smart-publish.yml` completed successfully.
- [ ] Published versions are visible on NuGet.org.
- [ ] The GitHub Release contains the expected packages.
- [ ] `version-check.yml` does not report unexpected unpublished versions.
