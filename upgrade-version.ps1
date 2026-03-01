#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Upgrade version for a NuGet package and update all dependencies in the workspace.

.DESCRIPTION
    This script prompts the user to select a NuGet package from the workspace,
    allows selecting version bump type (patch, minor, major), updates the version
    in the .csproj file, updates Directory.Packages.props for central package management,
    and updates all PackageReference dependencies across other projects in the workspace.

.PARAMETER PackageName
    The name of the package to upgrade (optional). If not provided, an interactive menu will be shown.

.PARAMETER BumpType
    The type of version bump: 'patch', 'minor', or 'major'. Default is 'patch'.

.PARAMETER SkipBuild
    Skip the build step after updating versions.

.EXAMPLE
    .\upgrade-version.ps1
    Interactive mode - prompts for package and version bump type

.EXAMPLE
    .\upgrade-version.ps1 -PackageName Acontplus.Core -BumpType minor
    Upgrades Acontplus.Core with a minor version bump

.EXAMPLE
    .\upgrade-version.ps1 -PackageName Acontplus.Utilities -BumpType patch -SkipBuild
    Upgrades Acontplus.Utilities with a patch version bump and skips the build
#>

param(
    [Parameter()]
    [string]$PackageName,

    [Parameter()]
    [ValidateSet('patch', 'minor', 'major')]
    [string]$BumpType = 'patch',

    [Parameter()]
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Get workspace root
$workspaceRoot = $PSScriptRoot
Set-Location $workspaceRoot

# Function to parse semantic version
function Get-ParsedVersion {
    param([string]$Version)

    if ($Version -match '^(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
        return @{
            Major = [int]$matches[1]
            Minor = [int]$matches[2]
            Patch = [int]$matches[3]
            Prerelease = $matches[4]
        }
    }
    throw "Invalid version format: $Version"
}

# Function to increment version
function Get-IncrementedVersion {
    param(
        [string]$CurrentVersion,
        [string]$BumpType
    )

    $parsed = Get-ParsedVersion -Version $CurrentVersion

    switch ($BumpType) {
        'major' {
            $parsed.Major++
            $parsed.Minor = 0
            $parsed.Patch = 0
        }
        'minor' {
            $parsed.Minor++
            $parsed.Patch = 0
        }
        'patch' {
            $parsed.Patch++
        }
    }

    $newVersion = "$($parsed.Major).$($parsed.Minor).$($parsed.Patch)"
    if ($parsed.Prerelease) {
        $newVersion += "-$($parsed.Prerelease)"
    }

    return $newVersion
}

# Function to get all NuGet packages in the workspace
function Get-WorkspacePackages {
    $srcPath = Join-Path $workspaceRoot "src"
    $packages = @()

    if (Test-Path $srcPath) {
        Get-ChildItem -Path $srcPath -Directory | ForEach-Object {
            $csprojFiles = Get-ChildItem -Path $_.FullName -Filter "*.csproj"

            if ($csprojFiles.Count -gt 0) {
                $csprojPath = $csprojFiles[0].FullName
                [xml]$csprojContent = Get-Content $csprojPath

                $packageId = $csprojContent.Project.PropertyGroup.PackageId | Select-Object -First 1
                $version = $csprojContent.Project.PropertyGroup.Version | Select-Object -First 1

                if ($packageId -and $version) {
                    $packages += [PSCustomObject]@{
                        PackageId = $packageId
                        Path = $_.FullName
                        CsprojPath = $csprojPath
                        Version = $version
                        FolderName = $_.Name
                    }
                }
            }
        }
    }

    return $packages
}

# Function to update version in .csproj file
function Update-CsprojVersion {
    param(
        [string]$CsprojPath,
        [string]$NewVersion
    )

    [xml]$csproj = Get-Content $CsprojPath

    # Find the PropertyGroup that contains the Version element
    $updated = $false
    foreach ($propertyGroup in $csproj.Project.PropertyGroup) {
        if ($propertyGroup.Version) {
            $propertyGroup.Version = $NewVersion
            $updated = $true
            break
        }
    }

    if (-not $updated) {
        throw "Version node not found in $CsprojPath"
    }

    $csproj.Save($CsprojPath)
    Write-Host "  Updated $CsprojPath" -ForegroundColor Green
}





# Function to update PackageReference dependencies in all .csproj files
function Update-PackageReferences {
    param(
        [string]$PackageId,
        [string]$NewVersion
    )

    $updatedCount = 0
    $srcPath = Join-Path $workspaceRoot "src"
    $appsPath = Join-Path $workspaceRoot "apps"

    $allCsprojFiles = @()

    if (Test-Path $srcPath) {
        $allCsprojFiles += Get-ChildItem -Path $srcPath -Recurse -Filter "*.csproj"
    }

    if (Test-Path $appsPath) {
        $allCsprojFiles += Get-ChildItem -Path $appsPath -Recurse -Filter "*.csproj"
    }

    foreach ($csprojFile in $allCsprojFiles) {
        [xml]$csproj = Get-Content $csprojFile.FullName
        $needsUpdate = $false

        # Check all ItemGroup nodes
        foreach ($itemGroup in $csproj.Project.ItemGroup) {
            if ($itemGroup.PackageReference) {
                foreach ($packageRef in $itemGroup.PackageReference) {
                    if ($packageRef.Include -eq $PackageId) {
                        # Check if this is using Central Package Management (no Version attribute)
                        if (-not $packageRef.HasAttribute("Version")) {
                            # Central Package Management - version is in Directory.Packages.props
                            continue
                        }

                        # Update explicit version
                        Write-Host "  Updating PackageReference in $($csprojFile.Directory.Name)..." -ForegroundColor Yellow
                        $packageRef.Version = $NewVersion
                        $needsUpdate = $true
                    }
                }
            }
        }

        if ($needsUpdate) {
            $csproj.Save($csprojFile.FullName)
            $updatedCount++
        }
    }

    return $updatedCount
}

# Main script
Write-Host "`n=== NuGet Package Version Upgrader ===" -ForegroundColor Cyan
Write-Host ""

# Get all packages
$packages = Get-WorkspacePackages

if ($packages.Count -eq 0) {
    Write-Host "No NuGet packages found in workspace!" -ForegroundColor Red
    exit 1
}

# If package name not provided, prompt user
if (-not $PackageName) {
    Write-Host "Available NuGet packages:" -ForegroundColor Green
    Write-Host ""

    for ($i = 0; $i -lt $packages.Count; $i++) {
        Write-Host "  [$($i + 1)] $($packages[$i].PackageId) (v$($packages[$i].Version))" -ForegroundColor White
    }

    Write-Host ""
    $selection = Read-Host "Select a package number (1-$($packages.Count))"

    try {
        $selectedIndex = [int]$selection - 1
    } catch {
        Write-Host "Invalid selection!" -ForegroundColor Red
        exit 1
    }

    if ($selectedIndex -lt 0 -or $selectedIndex -ge $packages.Count) {
        Write-Host "Invalid selection!" -ForegroundColor Red
        exit 1
    }

    $selectedPackage = $packages[$selectedIndex]
} else {
    # Find package by name or folder name
    $selectedPackage = $packages | Where-Object {
        $_.PackageId -eq $PackageName -or $_.FolderName -eq $PackageName
    } | Select-Object -First 1

    if (-not $selectedPackage) {
        Write-Host "Package '$PackageName' not found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Available packages:" -ForegroundColor Yellow
        $packages | ForEach-Object { Write-Host "  - $($_.PackageId)" }
        exit 1
    }
}

Write-Host ""
Write-Host "Selected package: $($selectedPackage.PackageId) (v$($selectedPackage.Version))" -ForegroundColor Cyan
Write-Host ""

# If bump type not provided via parameter, prompt user
if (-not $PSBoundParameters.ContainsKey('BumpType')) {
    Write-Host "Select version bump type:" -ForegroundColor Green
    Write-Host "  [1] Patch (e.g., 1.0.0 -> 1.0.1)" -ForegroundColor White
    Write-Host "  [2] Minor (e.g., 1.0.0 -> 1.1.0)" -ForegroundColor White
    Write-Host "  [3] Major (e.g., 1.0.0 -> 2.0.0)" -ForegroundColor White
    Write-Host ""

    $bumpSelection = Read-Host "Select bump type (1-3, default: 1)"

    if ([string]::IsNullOrWhiteSpace($bumpSelection)) {
        $bumpSelection = "1"
    }

    switch ($bumpSelection) {
        "1" { $BumpType = "patch" }
        "2" { $BumpType = "minor" }
        "3" { $BumpType = "major" }
        default {
            Write-Host "Invalid selection! Using 'patch' by default." -ForegroundColor Yellow
            $BumpType = "patch"
        }
    }
}

Write-Host ""
Write-Host "Bump type: $BumpType" -ForegroundColor Cyan

# Calculate new version
try {
    $newVersion = Get-IncrementedVersion -CurrentVersion $selectedPackage.Version -BumpType $BumpType
} catch {
    Write-Host "Error calculating new version: $_" -ForegroundColor Red
    exit 1
}

Write-Host "New version will be: $newVersion" -ForegroundColor Green
Write-Host ""

# Confirm before proceeding
$confirm = Read-Host "Do you want to proceed with this version upgrade? (y/N)"
if ($confirm -notmatch '^[Yy]') {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "=== Starting Version Update ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Update .csproj version
Write-Host "Step 1: Updating .csproj version..." -ForegroundColor Green
try {
    Update-CsprojVersion -CsprojPath $selectedPackage.CsprojPath -NewVersion $newVersion
} catch {
    Write-Host "Error updating .csproj: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Update PackageReference dependencies in other projects
Write-Host "Step 2: Updating PackageReference dependencies in other projects..." -ForegroundColor Green
$updatedCount = Update-PackageReferences -PackageId $selectedPackage.PackageId -NewVersion $newVersion

if ($updatedCount -gt 0) {
    Write-Host "  Updated $updatedCount project(s) with new PackageReference version." -ForegroundColor Green
} else {
    Write-Host "  No other projects reference this package (or using central package management)." -ForegroundColor Yellow
}

Write-Host ""

# Step 3: Build the solution (optional)
if (-not $SkipBuild) {
    Write-Host "Step 3: Building solution to verify changes..." -ForegroundColor Green
    try {
        $solutionPath = Join-Path $workspaceRoot "acontplus-dotnet-libs.slnx"

        if (Test-Path $solutionPath) {
            Write-Host "  Running: dotnet build $solutionPath" -ForegroundColor Gray
            $buildOutput = dotnet build $solutionPath 2>&1

            if ($LASTEXITCODE -ne 0) {
                Write-Host ""
                Write-Host "Build failed! Output:" -ForegroundColor Red
                Write-Host $buildOutput
                Write-Host ""
                Write-Host "Version was updated but build failed. Please fix build errors." -ForegroundColor Yellow
            } else {
                Write-Host "  Build succeeded!" -ForegroundColor Green
            }
        } else {
            Write-Host "  Solution file not found, skipping build." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  Build error: $_" -ForegroundColor Red
    }
} else {
    Write-Host "Step 3: Skipping build (use -SkipBuild:$false to enable)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Done! ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "  Package: $($selectedPackage.PackageId)" -ForegroundColor White
Write-Host "  Old Version: $($selectedPackage.Version)" -ForegroundColor White
Write-Host "  New Version: $newVersion" -ForegroundColor Green
Write-Host "  Bump Type: $BumpType" -ForegroundColor White
Write-Host "  Projects Updated: $updatedCount" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Review the changes" -ForegroundColor White
Write-Host "  2. Test your changes" -ForegroundColor White
Write-Host "  3. Commit: git add . && git commit -m 'chore: bump $($selectedPackage.PackageId) to $newVersion'" -ForegroundColor White
Write-Host "  4. Create NuGet package: dotnet pack $($selectedPackage.CsprojPath) -c Release" -ForegroundColor White
Write-Host ""
