param(
    [string]$BumpType = "patch",
    [switch]$SkipBuild = $false
)

# Get workspace root
$workspaceRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

# Function to get dependency graph
function Get-PackageDependencies {
    $dependencies = @{}
    $srcPath = Join-Path $workspaceRoot "src"

    if (Test-Path $srcPath) {
        $csprojFiles = Get-ChildItem -Path $srcPath -Recurse -Filter "*.csproj"

        foreach ($csprojFile in $csprojFiles) {
            [xml]$csproj = Get-Content $csprojFile.FullName
            $packageName = $csproj.Project.PropertyGroup.PackageId

            if ($packageName) {
                $deps = @()
                foreach ($itemGroup in $csproj.Project.ItemGroup) {
                    if ($itemGroup.PackageReference) {
                        foreach ($packageRef in $itemGroup.PackageReference) {
                            if ($packageRef.Include -like "Acontplus.*") {
                                $deps += $packageRef.Include
                            }
                        }
                    }
                }
                $dependencies[$packageName] = $deps
            }
        }
    }

    return $dependencies
}

# Function to get topological sort order
function Get-UpdateOrder {
    param($dependencies)

    $visited = @{}
    $result = @()

    function Visit-Package($package) {
        if ($visited[$package] -eq "visiting") {
            throw "Circular dependency detected involving $package"
        }
        if ($visited[$package] -eq "visited") {
            return
        }

        $visited[$package] = "visiting"

        if ($dependencies[$package]) {
            foreach ($dep in $dependencies[$package]) {
                if ($dependencies.ContainsKey($dep)) {
                    Visit-Package $dep
                }
            }
        }

        $visited[$package] = "visited"
        $result += $package
    }

    foreach ($package in $dependencies.Keys) {
        if ($visited[$package] -ne "visited") {
            Visit-Package $package
        }
    }

    return $result
}

Write-Host "=== Batch NuGet Package Version Upgrader ===" -ForegroundColor Cyan

# Get dependencies and update order
$dependencies = Get-PackageDependencies
$updateOrder = Get-UpdateOrder $dependencies

Write-Host "Update order (dependencies first):" -ForegroundColor Green
$updateOrder | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }

$confirm = Read-Host "`nProceed with batch $BumpType update? (y/N)"
if ($confirm -notmatch '^[Yy]') {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

# Update each package in dependency order
foreach ($package in $updateOrder) {
    Write-Host "`n--- Updating $package ---" -ForegroundColor Cyan
    & "$workspaceRoot\upgrade-version.ps1" -PackageName $package -BumpType $BumpType -SkipBuild
}

# Final build
if (-not $SkipBuild) {
    Write-Host "`n--- Final Build ---" -ForegroundColor Cyan
    $solutionPath = Join-Path $workspaceRoot "acontplus-dotnet-libs.slnx"
    if (Test-Path $solutionPath) {
        dotnet build $solutionPath
    }
}

Write-Host "`nBatch update complete!" -ForegroundColor Green
