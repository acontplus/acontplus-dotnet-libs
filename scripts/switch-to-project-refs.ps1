param(
    [switch]$Restore = $false
)

$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$backupSuffix = ".packageref.backup"

function Switch-ToProjectReferences {
    $srcPath = Join-Path $workspaceRoot "src"
    $appsPath = Join-Path $workspaceRoot "apps"
    
    $allCsprojFiles = @()
    if (Test-Path $srcPath) { $allCsprojFiles += Get-ChildItem -Path $srcPath -Recurse -Filter "*.csproj" }
    if (Test-Path $appsPath) { $allCsprojFiles += Get-ChildItem -Path $appsPath -Recurse -Filter "*.csproj" }
    
    foreach ($csprojFile in $allCsprojFiles) {
        # Backup original
        Copy-Item $csprojFile.FullName "$($csprojFile.FullName)$backupSuffix" -Force
        
        [xml]$csproj = Get-Content $csprojFile.FullName
        $modified = $false
        
        foreach ($itemGroup in $csproj.Project.ItemGroup) {
            if ($itemGroup.PackageReference) {
                $toRemove = @()
                $toAdd = @()
                
                foreach ($packageRef in $itemGroup.PackageReference) {
                    if ($packageRef.Include -like "Acontplus.*") {
                        # Find corresponding project
                        $projectPath = Get-ChildItem -Path $srcPath -Recurse -Filter "*.csproj" | 
                            Where-Object { 
                                [xml]$proj = Get-Content $_.FullName
                                $proj.Project.PropertyGroup.PackageId -eq $packageRef.Include
                            } | Select-Object -First 1
                        
                        if ($projectPath) {
                            $relativePath = [System.IO.Path]::GetRelativePath($csprojFile.Directory.FullName, $projectPath.FullName)
                            $toRemove += $packageRef
                            $toAdd += $relativePath
                            $modified = $true
                        }
                    }
                }
                
                # Remove PackageReferences and add ProjectReferences
                foreach ($ref in $toRemove) {
                    $itemGroup.RemoveChild($ref) | Out-Null
                }
                
                foreach ($projPath in $toAdd) {
                    $projectRef = $csproj.CreateElement("ProjectReference")
                    $projectRef.SetAttribute("Include", $projPath)
                    $itemGroup.AppendChild($projectRef) | Out-Null
                }
            }
        }
        
        if ($modified) {
            $csproj.Save($csprojFile.FullName)
            Write-Host "Converted $($csprojFile.Name) to project references" -ForegroundColor Green
        }
    }
}

function Restore-PackageReferences {
    $srcPath = Join-Path $workspaceRoot "src"
    $appsPath = Join-Path $workspaceRoot "apps"
    
    $backupFiles = @()
    if (Test-Path $srcPath) { $backupFiles += Get-ChildItem -Path $srcPath -Recurse -Filter "*$backupSuffix" }
    if (Test-Path $appsPath) { $backupFiles += Get-ChildItem -Path $appsPath -Recurse -Filter "*$backupSuffix" }
    
    foreach ($backupFile in $backupFiles) {
        $originalFile = $backupFile.FullName -replace [regex]::Escape($backupSuffix), ""
        Move-Item $backupFile.FullName $originalFile -Force
        Write-Host "Restored $([System.IO.Path]::GetFileName($originalFile))" -ForegroundColor Green
    }
}

if ($Restore) {
    Write-Host "Restoring package references..." -ForegroundColor Cyan
    Restore-PackageReferences
} else {
    Write-Host "Switching to project references..." -ForegroundColor Cyan
    Switch-ToProjectReferences
}

Write-Host "Done!" -ForegroundColor Green