# GitHub Actions Setup Guide

This guide will help you configure GitHub Actions for automated NuGet package publishing.

## Quick Start (5 minutes)

### Step 1: Get Your NuGet API Key

1. Go to https://www.nuget.org/account/apikeys
2. Click **"Create"**
3. Configure:
   - **Key Name**: `GitHub Actions - Acontplus Libs`
   - **Scopes**: Select "Push"
   - **Packages**: Select "All Packages" or specific packages
   - **Glob Pattern**: `Acontplus.*` (recommended)
   - **Expiration**: 365 days (or as per your policy)
4. Click **"Create"**
5. **Copy the API key immediately** (you won't see it again!)

### Step 2: Add Secret to GitHub

1. Go to your repository on GitHub
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **"New repository secret"**
4. Enter:
   - **Name**: `NUGET_API_KEY`
   - **Secret**: Paste your NuGet API key
5. Click **"Add secret"**

### Step 3: Enable GitHub Actions

1. Go to **Actions** tab in your repository
2. If prompted, click **"I understand my workflows, go ahead and enable them"**
3. GitHub Actions is now ready!

### Step 4: Test the Setup

#### Option A: Manual Test (Recommended for First Time)

1. Go to **Actions** → **Publish NuGet Packages**
2. Click **"Run workflow"**
3. Leave options empty (will detect changed versions)
4. Click **"Run workflow"**
5. Monitor the workflow execution

#### Option B: Automatic Test

1. Update a package version in any `.csproj` file:
   ```xml
   <Version>2.0.4</Version> <!-- Increment this -->
   ```

2. Commit and push:
   ```bash
   git add src/Acontplus.Core/Acontplus.Core.csproj
   git commit -m "feat(core): bump version for testing"
   git push origin main
   ```

3. GitHub Actions will automatically start

## What Happens Next?

### Automatic Workflow Triggers

✅ **On Push to Main**:
- Detects which packages have new versions
- Builds and tests all packages
- Publishes changed packages to NuGet.org
- Creates a GitHub release with download links

✅ **On Pull Requests**:
- Builds and validates the code
- Checks version format
- Generates package files (but doesn't publish)

✅ **Daily at 9 AM UTC**:
- Compares local vs published versions
- Creates summary report
- Opens GitHub issue if unpublished versions exist

### Manual Controls

You can manually trigger workflows:

1. **Publish NuGet Packages**: Publish specific packages or force-publish existing versions
2. **Build and Test**: Run full build and validation
3. **Version Check**: Generate version comparison report

## Workflow Files

| File | Purpose | Trigger |
|------|---------|---------|
| `nuget-publish.yml` | Publishes packages to NuGet.org | Push to main, manual |
| `build-test.yml` | Validates build and tests | All branches, PRs |
| `version-check.yml` | Monitors version status | Daily, manual |

## Common Workflows

### Publishing a New Version

```bash
# 1. Use the version upgrade script
.\upgrade-version.ps1 -PackageName Acontplus.Core -BumpType minor

# 2. Commit the change
git add src/Acontplus.Core/Acontplus.Core.csproj
git commit -m "feat(core): add new feature xyz"

# 3. Push to trigger auto-publish
git push origin main

# 4. Monitor in GitHub Actions tab
```

### Publishing Multiple Packages

```bash
# 1. Upgrade multiple packages
.\upgrade-version.ps1 -PackageName Acontplus.Core -BumpType minor
.\upgrade-version.ps1 -PackageName Acontplus.Utilities -BumpType patch

# 2. Commit all changes
git add src/**/*.csproj
git commit -m "chore(build): bump multiple package versions"

# 3. Push once (all will publish in parallel)
git push origin main
```

### Emergency Republish

If you need to republish an existing version (use sparingly):

1. Go to **Actions** → **Publish NuGet Packages** → **Run workflow**
2. Enter package name: `Acontplus.Core`
3. Check **Force** checkbox
4. Click **"Run workflow"**

⚠️ **Warning**: NuGet.org usually doesn't allow overwriting versions. Force publish should only be used for truly exceptional circumstances.

## Monitoring

### Check Publish Status

- **GitHub Actions**: Repository → Actions tab
- **NuGet.org**: https://www.nuget.org/packages/[PackageName]
- **GitHub Releases**: Repository → Releases tab

### View Workflow Logs

1. Go to **Actions** tab
2. Click on workflow run
3. Click on job name
4. Expand steps to see detailed logs

### Download Published Packages

- **From GitHub**: Actions → Workflow run → Artifacts section
- **From NuGet**: `dotnet add package Acontplus.Core`

## Troubleshooting

### ❌ "401 Unauthorized"

**Problem**: Invalid or missing API key

**Solution**:
1. Verify `NUGET_API_KEY` secret exists in repository settings
2. Check API key hasn't expired on NuGet.org
3. Regenerate API key if necessary

### ❌ "409 Conflict"

**Problem**: Package version already exists

**Solution**: NuGet doesn't allow republishing. Increment the version number:
```xml
<Version>2.0.5</Version> <!-- Was 2.0.4 -->
```

### ❌ Workflow Not Triggering

**Problem**: Push to main doesn't start workflow

**Solutions**:
- Ensure changes are in `src/**/*.csproj` files
- Check branch name is `main`
- Verify workflows are enabled in Settings → Actions

### ❌ Build Failures

**Problem**: Workflow fails during build step

**Solution**:
1. Test locally: `dotnet build`
2. Check error logs in Actions tab
3. Verify all dependencies are available
4. Ensure .NET SDK version matches (10.0.x)

## Security Best Practices

✅ **DO**:
- Keep API keys in GitHub Secrets (never in code)
- Use minimal API key permissions (Push only)
- Set API key expiration dates
- Rotate keys regularly (annually)
- Use branch protection on `main`
- Require PR reviews before merging

❌ **DON'T**:
- Commit API keys to repository
- Use overly permissive API keys
- Share API keys between projects
- Publish from feature branches

## Advanced Topics

### Custom Package Sources

To publish to private feeds, modify `nuget-publish.yml`:

```yaml
- name: Publish to private feed
  run: |
    dotnet nuget push "nupkgs/*.nupkg" \
      --api-key ${{ secrets.PRIVATE_FEED_KEY }} \
      --source https://your-feed.com/nuget
```

### Pre-release Versions

Use semantic versioning for pre-releases:

```xml
<Version>2.1.0-beta.1</Version>
<Version>2.1.0-rc.2</Version>
<Version>2.1.0-alpha.3</Version>
```

Workflow automatically handles pre-release versions.

### Notifications

Add Slack/Teams notifications by adding steps to workflows:

```yaml
- name: Notify Slack
  uses: slackapi/slack-github-action@v1
  with:
    webhook-url: ${{ secrets.SLACK_WEBHOOK }}
    payload: |
      {
        "text": "Package published: ${{ matrix.package.Name }}"
      }
```

## Getting Help

- **Workflow Issues**: Check Actions tab → Workflow run → Logs
- **NuGet Issues**: Visit https://status.nuget.org
- **GitHub Actions Docs**: https://docs.github.com/actions

## Next Steps

After setup is complete:

1. ✅ Test with a version bump
2. ✅ Verify package appears on NuGet.org
3. ✅ Check GitHub release was created
4. ✅ Set up branch protection rules
5. ✅ Document your release process
6. ✅ Schedule API key rotation

---

**Setup Complete!** 🎉

Your repository is now configured for automated NuGet publishing. Every time you push a version change to `main`, your packages will automatically be published to NuGet.org.

For detailed documentation, see [workflows/README.md](workflows/README.md)
