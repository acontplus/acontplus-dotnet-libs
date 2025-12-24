# 🔄 Cascade Publishing Workflows

This guide explains how to use the automated cascade publishing workflows for Acontplus libraries.

## ⚠️ Important: Do I Need to Manually Change Versions?

### ❌ NO Manual Version Change Required

**cascade-publish.yml** - This workflow is **completely automatic**:
```yaml
Inputs you provide:
  root-package: Core          # ← Just specify the root package
  bump-type: patch            # ← Just specify the bump type
  cascade-bump: patch         # ← Bump for dependents
```

✅ **The workflow does EVERYTHING automatically**:
- Calculates the new version based on bump-type
- Updates `.csproj` of all affected packages
- Updates `Directory.Packages.props` with new versions
- Updates dependents in cascade
- Creates PR with all changes

**You don't need to touch code**, just run the workflow with parameters.

### ✅ YES Manual Version Change Required

**For normal PRs that trigger smart-publish.yml**, you must update versions locally:

```powershell
# Use the local script:
.\upgrade-version.ps1 -PackageName Core -BumpType patch
```

This script updates:
- ✅ Package `.csproj`
- ✅ `Directory.Packages.props`
- ✅ References in other projects
- ✅ Runs build to verify

Then commit, push and merge the PR → smart-publish detects the change and publishes.

---

## 📋 Table of Contents

- [Important: Do I Need to Manually Change Versions?](#important-do-i-need-to-manually-change-versions)
- [Available Workflows](#available-workflows)
- [Recommended Flow](#recommended-flow)
- [Use Cases](#use-cases)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)

---

## 🔧 Available Workflows

### 1. **cascade-publish.yml** - Manual Cascade Publishing

**Purpose**: Update a root package and all its dependents in the correct order.

**Trigger**: Manual (workflow_dispatch)

**Requires manual version change?** ❌ **NO** - The workflow calculates and updates versions automatically.

**Parameters**:
- `root-package`: Initial package (Core, Utilities, Services, etc.)
- `bump-type`: Bump type for root package (major/minor/patch)
- `cascade-bump`: Bump type for dependents (major/minor/patch/none)
- `create-pr`: Create PR instead of publishing directly (default: true) ✅
- `run-tests`: Run tests before publishing (default: true) ✅
- `dry-run`: Simulate without publishing (default: false)

**What it updates automatically**:
- ✅ Version in `.csproj` of each package
- ✅ `Directory.Packages.props` with new versions
- ✅ Dependencies in affected packages
- ✅ Creates commit and PR with all changes

### 2. **pr-cascade-publish.yml** - Auto Publish on PR Merge

**Purpose**: Automatically publish when a cascade update PR is merged.

**Trigger**: Automatic when merging PR with `cascade-update/` prefix

**Features**:
- Automatically detects modified packages
- Runs tests before publishing
- **Waits 30s + 10 retries** to verify availability on NuGet.org
- **Stops the cascade** if a package is not indexed (prevents failures in dependents)
- **Clears NuGet cache** before restore to ensure correct versions
- Automatically creates GitHub Release
- Error handling with automatic issues

**Important note about Directory.Packages.props**:
- This workflow **DOES NOT update** Directory.Packages.props
- Versions were already updated by `cascade-publish.yml` when it created the PR
- Only publishes packages with versions already in the merged PR

### 3. **nuget-publish.yml** - Individual Publishing

**Purpose**: Publish individual packages when their version changes.

**Trigger**: Push to `main` or workflow_dispatch

---

## 🎯 Recommended Flow

### **Option A: Safe Flow (PR Review)** ⭐ RECOMMENDED

```mermaid
graph LR
    A[Update Core] --> B[Run cascade-publish.yml]
    B --> C[create-pr: true]
    C --> D[Review PR]
    D --> E[Merge PR]
    E --> F[pr-cascade-publish.yml]
    F --> G[Published on NuGet]
```

**Steps**:

1. **Run Cascade Update with PR**:
   ```
   GitHub → Actions → Cascade Publish
   - Root Package: Core
   - Bump Type: minor
   - Cascade Bump: patch
   - Create PR: ✅ true (IMPORTANT)
   - Run Tests: ✅ true
   - Dry Run: ❌ false
   ```

2. **Automatically created**:
   - ✅ Branch `cascade-update/Core-<timestamp>`
   - ✅ Commits with version changes in `.csproj` files
   - ✅ **Automatic update of `Directory.Packages.props`** with new versions
   - ✅ PR with detailed changelog
   - ✅ Labels: automated, version-bump, dependencies

3. **PR Review**:
   - Verify updated versions
   - Review changelog
   - Validate that Directory.Packages.props is correct
   - Approve and merge

4. **On merge → Automatic Publishing**:
   - Workflow `pr-cascade-publish.yml` detects the merge
   - Runs final tests
   - Publishes to NuGet.org in sequential order
   - **Verifies availability** of each package (30s + 10 retries)
   - **Stops the cascade** if a package is not indexed correctly
   - **Clears NuGet cache** to ensure dependents use correct versions
   - Creates GitHub Release

### **Option B: Direct Flow (For Urgent Hotfixes Only)** ⚠️

```
GitHub → Actions → Cascade Publish
- Create PR: ❌ false
- Run Tests: ✅ true
```

⚠️ **Warning**: Publishes directly without review. Only use in emergencies.

---

## 📝 Use Cases

### Case 1: Minor Update to Core

**Scenario**: You added new global enums to Core.

```
Root Package: Core
Bump Type: minor (2.1.0 → 2.2.0)
Cascade Bump: patch
Create PR: true ✅
```

**Result**:
```
Acontplus.Core:              2.1.0 → 2.2.0
Acontplus.Utilities:         2.0.4 → 2.0.5
Acontplus.Services:          2.1.5 → 2.1.6
Acontplus.Persistence.*:     2.0.6 → 2.0.7
Acontplus.Infrastructure:    1.3.2 → 1.3.3
Acontplus.Billing:           1.2.0 → 1.2.1
Acontplus.Reports:           1.3.15 → 1.3.16
...
```

### Case 2: Bugfix in Utilities Without Cascade

**Scenario**: Minor fix in Utilities that doesn't affect dependents.

```
Root Package: Utilities
Bump Type: patch (2.0.5 → 2.0.6)
Cascade Bump: none ← IMPORTANT
Create PR: true
```

**Result**:
```
Acontplus.Utilities: 2.0.5 → 2.0.6
(No other package is updated)
```

### Case 3: Breaking Change in Core

**Scenario**: Incompatible changes in Core API.

```
Root Package: Core
Bump Type: major (2.2.0 → 3.0.0)
Cascade Bump: major ← IMPORTANT for SemVer
Create PR: true
```

**Result**:
```
Acontplus.Core:         2.2.0 → 3.0.0
Acontplus.Utilities:    2.0.6 → 3.0.0
Acontplus.Services:     2.1.6 → 3.0.0
...
```

### Case 4: Dry Run for Testing

**Scenario**: You want to see what would be updated without making changes.

```
Root Package: Core
Bump Type: minor
Cascade Bump: patch
Dry Run: true ✅
```

**Result**: Only logs in workflow, no real changes.

---

## ⚙️ Configuration

### Required Secrets

```yaml
# .github/workflows needs:
NUGET_API_KEY  # To publish on NuGet.org
GITHUB_TOKEN   # Automatic, no configuration required
```

### Configure NUGET_API_KEY

1. Go to [NuGet.org API Keys](https://www.nuget.org/account/apikeys)
2. Create new API Key:
   - **Name**: `GitHub Actions - Acontplus`
   - **Glob Pattern**: `Acontplus.*`
   - **Select Scopes**: ✅ Push, ✅ Push new packages
3. Copy the key
4. GitHub Repo → Settings → Secrets → Actions → New secret
   - Name: `NUGET_API_KEY`
   - Value: `<paste key>`

---

## 🐛 Troubleshooting

### Problem: "Tests Failed"

**Solution**:
```bash
# Run tests locally first
dotnet test
# Fix tests
# Commit and push
# Re-run workflow
```

### Problem: "Package Already Published"

**Cause**: The version already exists on NuGet.org

**Solution**:
```powershell
# Increment version manually
.\upgrade-version.ps1 -PackageName Core -BumpType patch
```

### Problem: "NuGet Indexing Timeout"

**Cause**: NuGet.org is slow indexing

**Prevention**: The workflow now has improved protection:
- Initial wait of **30 seconds**
- **10 automatic retries** (total ~130 seconds)
- **Stops the cascade** if it cannot verify (prevents failures in dependents)
- **Clears NuGet cache** before each restore to ensure correct versions

**If it fails after 10 retries**:
1. Manually verify on NuGet.org after 5-10 minutes
2. The workflow stopped to prevent problems in dependent packages
3. Once manually verified that the package is available:
   - Re-run from the next package in the cascade
   - Or wait longer and re-run everything

### Problem: PR Merge Doesn't Trigger Publish

**Cause**: Branch doesn't have `cascade-update/` prefix

**Solution**: The automatic workflow only works with PRs created by `cascade-publish.yml`. For manual publishing, use `nuget-publish.yml`.

### Problem: Build Failed in Cascade

**Symptoms**: A package failed to compile during the cascade

**Solution**:
1. The workflow automatically creates an **Issue** with details
2. Review workflow logs
3. Fix the error
4. Re-run from the failed package:
   ```
   Root Package: <failed-package>
   Bump Type: patch
   ```

### Problem: Circular Dependency Detected

**Symptoms**: Error "Circular dependency detected involving X"

**Solution**:
```bash
# Verify dependencies
.\batch-upgrade-version.ps1 -BumpType patch
# If cycle detected, review .csproj files
```

---

## 📊 Best Practices

### ✅ DO's

- ✅ **Always use `create-pr: true`** for important changes
- ✅ **Run tests locally** before cascade publish
- ✅ **Use `dry-run: true`** to verify changes first
- ✅ **Follow Semantic Versioning** strictly
- ✅ **Review the PR** before merging
- ✅ **Wait for completion** of entire workflow before new executions
- ✅ **Trust automatic verification** - the workflow waits 30s + 10 retries before continuing
- ✅ **If verification fails** - check NuGet.org manually before retrying

### ❌ DON'Ts

- ❌ **Don't use `create-pr: false`** except for emergencies
- ❌ **Don't skip tests** (`run-tests: false`)
- ❌ **Don't run multiple cascades** simultaneously
- ❌ **Don't modify manually** during active workflow
- ❌ **Don't merge PR** if there are conflicts

---

## 🔗 Useful Links

- [Semantic Versioning](https://semver.org/)
- [NuGet Best Practices](https://docs.microsoft.com/nuget/create-packages/package-authoring-best-practices)
- [GitHub Actions Docs](https://docs.github.com/actions)
- [Directory.Packages.props](../../Directory.Packages.props)

---

## 📞 Support

If you encounter problems:

1. 🔍 Review workflow logs in GitHub Actions
2. 🐛 Search for similar issues in the repo
3. ❓ Create new issue with `workflow` label if necessary
4. 📧 Contact: proyectos@acontplus.com

---

**Last updated**: December 2025
