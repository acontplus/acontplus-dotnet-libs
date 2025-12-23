# GitHub Actions Setup Checklist

Use this checklist to ensure your GitHub Actions for NuGet publishing is properly configured.

## Pre-Deployment Checklist

### 1. NuGet.org Setup

- [ ] Have an active NuGet.org account
- [ ] Created API key at https://www.nuget.org/account/apikeys
- [ ] API key has "Push" permission
- [ ] API key scope set to `Acontplus.*` (or appropriate pattern)
- [ ] Copied API key to secure location

### 2. GitHub Repository Setup

- [ ] Added `NUGET_API_KEY` secret to repository
  - Path: Settings → Secrets and variables → Actions → New repository secret
- [ ] Verified secret name is exactly `NUGET_API_KEY` (case-sensitive)
- [ ] Workflows are enabled in repository settings
- [ ] GitHub Actions are allowed to create releases (if needed)

### 3. Code Configuration

- [ ] All package projects have `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>`
- [ ] All package projects have valid `<Version>X.Y.Z</Version>`
- [ ] All package projects have `<PackageId>` matching package name
- [ ] Package metadata is complete (Authors, Description, Tags, etc.)
- [ ] README.md files exist in package directories (if included in package)
- [ ] Package icons exist (if specified in .csproj)

### 4. Workflow Files

- [ ] `.github/workflows/nuget-publish.yml` exists
- [ ] `.github/workflows/build-test.yml` exists
- [ ] `.github/workflows/version-check.yml` exists
- [ ] Workflow files are valid YAML (no syntax errors)
- [ ] Workflow permissions are appropriate

## First-Time Testing Checklist

### Test 1: Build and Validation

- [ ] Create a test branch
- [ ] Make a trivial change to trigger build
- [ ] Push to test branch
- [ ] Verify "Build and Test" workflow runs successfully
- [ ] Check workflow logs for errors
- [ ] Verify packages are built correctly

### Test 2: Manual Publish (Dry Run)

- [ ] Increment version in one test package
- [ ] Go to Actions → Publish NuGet Packages → Run workflow
- [ ] Select manual trigger with package name
- [ ] Monitor workflow execution
- [ ] Check for successful completion
- [ ] Verify package appears on NuGet.org
- [ ] Verify GitHub release is created

### Test 3: Automatic Publish

- [ ] Create new branch from main
- [ ] Increment version in a package .csproj
- [ ] Commit with proper message (e.g., "feat(core): test publish")
- [ ] Create pull request
- [ ] Verify "Build and Test" runs on PR
- [ ] Merge PR to main
- [ ] Verify "Publish NuGet Packages" runs automatically
- [ ] Check package published to NuGet.org
- [ ] Verify GitHub release created

### Test 4: Version Check

- [ ] Go to Actions → Version Check → Run workflow
- [ ] Check workflow runs successfully
- [ ] Review version comparison report
- [ ] Verify report accuracy

## Post-Deployment Checklist

### Security

- [ ] API key is stored only in GitHub Secrets (not in code)
- [ ] No sensitive information in workflow files
- [ ] Branch protection rules enabled on main/master
- [ ] Require pull request reviews before merging
- [ ] Set API key expiration date (recommended: 1 year)
- [ ] Document API key rotation process

### Documentation

- [ ] Team knows how to publish packages
- [ ] Setup instructions are accessible
- [ ] Troubleshooting guide is available
- [ ] Contact person identified for issues

### Monitoring

- [ ] Subscribed to workflow failure notifications
- [ ] GitHub Actions usage limits understood
- [ ] NuGet.org account monitoring configured
- [ ] Regular review schedule established

### Maintenance

- [ ] Calendar reminder set for API key rotation
- [ ] Workflow review scheduled (quarterly)
- [ ] Team training completed
- [ ] Backup maintainers identified

## Validation Tests

### Package Quality Checks

- [ ] Download published package from NuGet.org
- [ ] Install package in test project: `dotnet add package Acontplus.Core`
- [ ] Verify all dependencies resolve correctly
- [ ] Check package metadata in NuGet Gallery
- [ ] Verify package icon displays correctly
- [ ] Confirm README.md is visible in gallery
- [ ] Test package functionality in sample app

### Workflow Quality Checks

- [ ] All jobs complete successfully
- [ ] Build time is reasonable (< 10 minutes typically)
- [ ] Artifacts are uploaded correctly
- [ ] Release notes are generated properly
- [ ] Parallel publishing works for multiple packages
- [ ] Error handling works (test with invalid version)

## Rollback Plan

In case of issues, document rollback procedures:

- [ ] Know how to disable workflows temporarily
- [ ] Can revert version changes in .csproj files
- [ ] Can manually publish packages if automation fails
- [ ] Have backup of API keys in secure location
- [ ] Know how to contact GitHub support
- [ ] Know how to contact NuGet.org support

## Common Issues Checklist

### If Workflow Fails

- [ ] Check workflow logs in Actions tab
- [ ] Verify `NUGET_API_KEY` secret is set
- [ ] Confirm API key hasn't expired
- [ ] Check NuGet.org status page
- [ ] Verify version number is incremented
- [ ] Ensure .NET SDK version matches workflow
- [ ] Check for build errors locally

### If Package Doesn't Appear on NuGet.org

- [ ] Wait 5-10 minutes for indexing
- [ ] Check workflow logs for push errors
- [ ] Verify API key permissions
- [ ] Check if version already exists
- [ ] Verify package ID is correct
- [ ] Check NuGet.org account status

## Periodic Maintenance Schedule

### Weekly

- [ ] Review failed workflows (if any)
- [ ] Check for unpublished versions

### Monthly

- [ ] Review GitHub Actions usage
- [ ] Check for workflow optimizations
- [ ] Verify all packages are up to date

### Quarterly

- [ ] Review and update workflows
- [ ] Check for new GitHub Actions features
- [ ] Update documentation
- [ ] Team training refresh

### Annually

- [ ] Rotate NuGet API keys
- [ ] Security audit of workflows
- [ ] Review and update this checklist
- [ ] Evaluate new publishing strategies

## Success Criteria

✅ **Setup is successful when**:

1. Pushing a version change to main automatically publishes to NuGet.org
2. Build and test workflow runs on all branches
3. GitHub releases are created automatically
4. Team can manually trigger publishes when needed
5. Version check runs daily without errors
6. Artifacts are available in workflow runs
7. No secrets are exposed in logs
8. Parallel publishing works for multiple packages

## Sign-Off

- [ ] Setup completed by: _________________ Date: _________
- [ ] Tested by: _________________ Date: _________
- [ ] Approved by: _________________ Date: _________

## Notes

Use this section for any additional notes or observations:

```
_________________________________________________________________

_________________________________________________________________

_________________________________________________________________

_________________________________________________________________
```

---

**Last Updated**: December 23, 2025
**Checklist Version**: 1.0.0
**Repository**: acontplus-dotnet-libs
