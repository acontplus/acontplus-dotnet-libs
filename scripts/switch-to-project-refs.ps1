[CmdletBinding()]
param(
    [switch]$Restore = $false,
    [switch]$NoRestore = $false
)

# Do NOT change the caller's working directory.
$workspaceRoot = Split-Path -Parent $PSScriptRoot
$backupSuffix = ".packageref.backup"

# Map all internal libraries in src/: Name -> FullPath
$srcPath = Join-Path $workspaceRoot "src"
$projectMap = @{}
if (Test-Path $srcPath) {
    Get-ChildItem -Path $srcPath -Recurse -Filter "*.csproj" | ForEach-Object {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName)
        $projectMap[$name] = $_.FullName
    }
}

function Get-TargetCsprojFiles {
    # Scan src, apps, and tests for csproj files
    $targetDirs = @("src", "apps", "tests")
    $files = @()
    foreach ($dir in $targetDirs) {
        $fullPath = Join-Path $workspaceRoot $dir
        if (Test-Path $fullPath) {
            $files += Get-ChildItem -Path $fullPath -Recurse -Filter "*.csproj"
        }
    }
    return $files
}

function Switch-ToProjectReferences {
    $allCsprojFiles = Get-TargetCsprojFiles
    $convertedCount = 0

    foreach ($csprojFile in $allCsprojFiles) {
        $content = [System.IO.File]::ReadAllText($csprojFile.FullName)

        # Match: <PackageReference Include="Acontplus.X" ... />
        $pattern = '(?m)^([ \t]*)<PackageReference\s+Include=["''](Acontplus\.[^"'']+)["''](?:\s+Version=["''][^"'']*["''])?\s*/>'

        $newContent = [regex]::Replace($content, $pattern, {
            param($match)
            $indent = $match.Groups[1].Value
            $pkgName = $match.Groups[2].Value

            if ($projectMap.ContainsKey($pkgName)) {
                $targetProj = $projectMap[$pkgName]
                # Do not reference self
                if ($csprojFile.FullName -ne $targetProj) {
                    $relPath = [System.IO.Path]::GetRelativePath($csprojFile.Directory.FullName, $targetProj).Replace('\', '/')
                    return "${indent}<ProjectReference Include=""$relPath"" />"
                }
            }
            return $match.Value
        })

        if ($newContent -ne $content) {
            $backupFile = "$($csprojFile.FullName)$backupSuffix"
            # Only create backup if one does not already exist to preserve the pristine original
            if (-not (Test-Path $backupFile)) {
                Copy-Item -Path $csprojFile.FullName -Destination $backupFile -Force
            }

            [System.IO.File]::WriteAllText($csprojFile.FullName, $newContent, (New-Object System.Text.UTF8Encoding($false)))
            Write-Host "  [+] Converted: $($csprojFile.Name) -> ProjectReference" -ForegroundColor Green
            $convertedCount++
        }
    }

    if ($convertedCount -eq 0) {
        Write-Host "  No PackageReferences found that need switching." -ForegroundColor Yellow
    } else {
        Write-Host "  Total projects converted to project references: $convertedCount" -ForegroundColor Green
    }
}

function Restore-PackageReferences {
    $restoredCount = 0

    # 1. Restore from .backup files if any exist
    $targetDirs = @("src", "apps", "tests")
    $backupFiles = @()
    foreach ($dir in $targetDirs) {
        $fullPath = Join-Path $workspaceRoot $dir
        if (Test-Path $fullPath) {
            $backupFiles += Get-ChildItem -Path $fullPath -Recurse -Filter "*$backupSuffix"
        }
    }

    if ($backupFiles.Count -gt 0) {
        foreach ($backupFile in $backupFiles) {
            $originalFile = $backupFile.FullName.Substring(0, $backupFile.FullName.Length - $backupSuffix.Length)
            Move-Item -Path $backupFile.FullName -Destination $originalFile -Force
            Write-Host "  [+] Restored from backup: $([System.IO.Path]::GetFileName($originalFile))" -ForegroundColor Green
            $restoredCount++
        }
    }

    # 2. Fallback / Safety Net: Revert any remaining ProjectReference pointing to internal Acontplus packages
    #    (Ensures full restoration even if backup files were missing, overwritten, or deleted)
    $allCsprojFiles = Get-TargetCsprojFiles
    foreach ($csprojFile in $allCsprojFiles) {
        # Only revert in src/ and apps/ (tests keep ProjectReference if designed to)
        $isInSrcOrApps = $csprojFile.FullName.StartsWith((Join-Path $workspaceRoot "src")) -or
                         $csprojFile.FullName.StartsWith((Join-Path $workspaceRoot "apps"))

        if (-not $isInSrcOrApps) {
            continue
        }

        $content = [System.IO.File]::ReadAllText($csprojFile.FullName)

        # Match: <ProjectReference Include="...Acontplus.X.csproj" /> or <ProjectReference Include="Acontplus.X" />
        $pattern = '(?m)^([ \t]*)<ProjectReference\s+Include=["'']([^"'']+)["'']\s*/>'

        $newContent = [regex]::Replace($content, $pattern, {
            param($match)
            $indent = $match.Groups[1].Value
            $includeVal = $match.Groups[2].Value

            $candidateName = [System.IO.Path]::GetFileNameWithoutExtension($includeVal)
            if ($projectMap.ContainsKey($candidateName)) {
                return "${indent}<PackageReference Include=""$candidateName"" />"
            }
            return $match.Value
        })

        if ($newContent -ne $content) {
            [System.IO.File]::WriteAllText($csprojFile.FullName, $newContent, (New-Object System.Text.UTF8Encoding($false)))
            Write-Host "  [+] Reverted to package reference: $($csprojFile.Name)" -ForegroundColor Green
            $restoredCount++
        }
    }

    if ($restoredCount -eq 0) {
        Write-Host "  All projects are already using PackageReferences (no restorations needed)." -ForegroundColor Yellow
    } else {
        Write-Host "  Total projects restored: $restoredCount" -ForegroundColor Green
    }
}

# Execution
if ($Restore) {
    Write-Host "Restoring package references..." -ForegroundColor Cyan
    Restore-PackageReferences
} else {
    Write-Host "Switching to project references..." -ForegroundColor Cyan
    Switch-ToProjectReferences
}

# Run dotnet restore to update dependency graph
if (-not $NoRestore) {
    Write-Host "`nRunning dotnet restore to update dependency graph..." -ForegroundColor Cyan
    $slnxFile = Join-Path $workspaceRoot "acontplus-dotnet-libs.slnx"
    if (Test-Path $slnxFile) {
        dotnet restore $slnxFile
    } else {
        dotnet restore
    }

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Dependencies successfully restored!" -ForegroundColor Green
    } else {
        Write-Warning "dotnet restore exited with code $LASTEXITCODE"
    }
}

Write-Host "`nDone!" -ForegroundColor Green
