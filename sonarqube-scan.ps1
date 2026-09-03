#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Corre un análisis SonarQube local para acontplus-dotnet-libs.

.DESCRIPTION
    Este script automatiza los pasos necesarios para registrar y analizar
    el proyecto acontplus-dotnet-libs en un SonarQube Server local.

    Pasos que ejecuta:
      1. Verifica prerequisitos (dotnet, dotnet-sonarscanner, SonarQube accesible)
      2. Crea el proyecto en SonarQube si no existe
      3. Corre: sonarscanner begin → dotnet build → sonarscanner end
      4. Espera que el análisis se procese y muestra el resumen de métricas

.PARAMETER Token
    Token de usuario de SonarQube (User Token).
    Generarlo en: http://localhost:9000 → My Account → Security → Generate Tokens

.PARAMETER ServerUrl
    URL del servidor SonarQube. Por defecto: http://localhost:9000

.PARAMETER ProjectKey
    Clave del proyecto en SonarQube. Por defecto: acontplus-dotnet-libs

.PARAMETER ProjectName
    Nombre visible del proyecto. Por defecto: Acontplus .NET Libraries

.EXAMPLE
    # Uso básico
    .\sonarqube-scan.ps1 -Token "squ_xxxxxxxxxxxx"

.EXAMPLE
    # Con servidor y proyecto personalizados
    .\sonarqube-scan.ps1 -Token "squ_xxxxxxxxxxxx" -ServerUrl "http://192.168.1.10:9000" -ProjectKey "mi-proyecto"

.NOTES
    Prerequisitos:
      - .NET SDK 10.x instalado
      - dotnet-sonarscanner instalado globalmente:
            dotnet tool install --global dotnet-sonarscanner
      - SonarQube Server corriendo y accesible
      - Docker corriendo (solo necesario para el MCP — no para este script)
#>

param(
    [Parameter(Mandatory = $true)]
    [string] $Token,

    [string] $ServerUrl  = "http://localhost:9000",
    [string] $ProjectKey = "acontplus-dotnet-libs",
    [string] $ProjectName = "Acontplus .NET Libraries"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─── Helpers ────────────────────────────────────────────────────────────────

function Write-Step([string] $msg) {
    Write-Host "`n── $msg" -ForegroundColor Cyan
}

function Write-Ok([string] $msg) {
    Write-Host "  ✓ $msg" -ForegroundColor Green
}

function Write-Warn([string] $msg) {
    Write-Host "  ⚠ $msg" -ForegroundColor Yellow
}

function Write-Fail([string] $msg) {
    Write-Host "  ✗ $msg" -ForegroundColor Red
}

function Get-AuthHeader([string] $token) {
    $bytes  = [Text.Encoding]::ASCII.GetBytes("${token}:")
    $base64 = [Convert]::ToBase64String($bytes)
    return @{ Authorization = "Basic $base64" }
}

function Invoke-SonarApi([string] $method, [string] $path, [hashtable] $body = @{}) {
    $headers = Get-AuthHeader $Token
    $uri     = "$ServerUrl$path"
    try {
        if ($method -eq "GET") {
            return Invoke-RestMethod -Method Get -Uri $uri -Headers $headers
        }
        return Invoke-RestMethod -Method Post -Uri $uri -Headers $headers `
            -ContentType "application/x-www-form-urlencoded" `
            -Body ($body.GetEnumerator() | ForEach-Object { "$($_.Key)=$([Uri]::EscapeDataString($_.Value))" } | Join-String -Separator "&")
    }
    catch {
        $status = $_.Exception.Response?.StatusCode.value__
        throw "SonarQube API $method $path falló (HTTP $status): $_"
    }
}

# ─── Paso 1: Prerequisitos ──────────────────────────────────────────────────

Write-Step "Verificando prerequisitos"

# dotnet
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail ".NET SDK no encontrado. Instálalo desde https://dotnet.microsoft.com/download"
    exit 1
}
$dotnetVer = (dotnet --version 2>&1).Trim()
Write-Ok ".NET SDK $dotnetVer"

# dotnet-sonarscanner
if (-not (Get-Command dotnet-sonarscanner -ErrorAction SilentlyContinue)) {
    Write-Warn "dotnet-sonarscanner no encontrado. Instalando..."
    dotnet tool install --global dotnet-sonarscanner
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "No se pudo instalar dotnet-sonarscanner"
        exit 1
    }
}
$scannerVer = (dotnet-sonarscanner --version 2>&1 | Select-Object -First 1).Trim()
Write-Ok "dotnet-sonarscanner: $scannerVer"

# SonarQube accesible
Write-Host "  Verificando $ServerUrl ..." -NoNewline
try {
    $status = Invoke-SonarApi "GET" "/api/system/status"
    Write-Host " OK" -ForegroundColor Green
    Write-Ok "SonarQube $($status.version) — estado: $($status.status)"
}
catch {
    Write-Host ""
    Write-Fail "No se puede conectar a SonarQube en $ServerUrl"
    Write-Host "  Asegúrate de que el contenedor esté corriendo y el puerto sea accesible." -ForegroundColor Yellow
    exit 1
}

# Token válido
$validation = Invoke-SonarApi "GET" "/api/authentication/validate"
if (-not $validation.valid) {
    Write-Fail "Token inválido. Generá uno en $ServerUrl → My Account → Security → Generate Tokens"
    exit 1
}
Write-Ok "Token autenticado correctamente"

# ─── Paso 2: Crear proyecto si no existe ────────────────────────────────────

Write-Step "Verificando proyecto '$ProjectKey' en SonarQube"

$projects = Invoke-SonarApi "GET" "/api/projects/search?projects=$ProjectKey"
$exists   = $projects.components | Where-Object { $_.key -eq $ProjectKey }

if ($exists) {
    Write-Ok "Proyecto ya existe — se omite la creación"
}
else {
    Write-Host "  Creando proyecto '$ProjectKey'..." -NoNewline
    Invoke-SonarApi "POST" "/api/projects/create" @{
        project    = $ProjectKey
        name       = $ProjectName
        mainBranch = "main"
    } | Out-Null
    Write-Host " OK" -ForegroundColor Green
    Write-Ok "Proyecto '$ProjectName' creado"
}

# ─── Paso 3: sonarscanner begin ─────────────────────────────────────────────

Write-Step "sonarscanner begin"

$beginArgs = @(
    "/k:$ProjectKey",
    "/n:$ProjectName",
    "/d:sonar.host.url=$ServerUrl",
    "/d:sonar.token=$Token",
    "/d:sonar.scm.provider=git"
)

dotnet-sonarscanner begin @beginArgs
if ($LASTEXITCODE -ne 0) {
    Write-Fail "sonarscanner begin falló (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}
Write-Ok "Begin completado"

# ─── Paso 4: dotnet build ────────────────────────────────────────────────────

Write-Step "dotnet build (Release)"

$slnx = Join-Path $PSScriptRoot "acontplus-dotnet-libs.slnx"
if (-not (Test-Path $slnx)) {
    Write-Fail "No se encontró el archivo de solución: $slnx"
    Write-Host "  Ejecuta el script desde la raíz del repositorio." -ForegroundColor Yellow
    exit 1
}

dotnet build $slnx --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Fail "dotnet build falló (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}
Write-Ok "Build completado"

# ─── Paso 5: sonarscanner end ────────────────────────────────────────────────

Write-Step "sonarscanner end (enviando resultados a SonarQube)"

dotnet-sonarscanner end /d:sonar.token="$Token"
if ($LASTEXITCODE -ne 0) {
    Write-Fail "sonarscanner end falló (exit $LASTEXITCODE)"
    exit $LASTEXITCODE
}
Write-Ok "Resultados enviados"

# ─── Paso 6: Esperar que se procese el análisis ──────────────────────────────

Write-Step "Esperando que SonarQube procese el análisis"

$maxWait   = 120   # segundos máximo
$interval  = 5
$waited    = 0
$processed = $false

while ($waited -lt $maxWait) {
    Start-Sleep -Seconds $interval
    $waited += $interval
    Write-Host "  Esperando... ($waited/$maxWait s)" -NoNewline `r

    try {
        $analyses = Invoke-SonarApi "GET" "/api/project_analyses/search?project=$ProjectKey&ps=1"
        if ($analyses.paging.total -gt 0) {
            $processed = $true
            break
        }
    }
    catch { <# ignorar errores intermedios #> }
}

Write-Host ""
if (-not $processed) {
    Write-Warn "El análisis no aparece después de $maxWait s. Puede seguir procesándose en background."
    Write-Host "  Revisa el estado en: $ServerUrl/dashboard?id=$ProjectKey" -ForegroundColor Yellow
    exit 0
}
Write-Ok "Análisis procesado"

# ─── Paso 7: Mostrar métricas ────────────────────────────────────────────────

Write-Step "Métricas del proyecto"

$metrics  = "bugs,vulnerabilities,code_smells,coverage,sqale_rating,reliability_rating,security_rating,ncloc,duplicated_lines_density"
$measures = Invoke-SonarApi "GET" "/api/measures/component?component=$ProjectKey&metricKeys=$metrics"

$ratingMap = @{ "1.0" = "A ✅"; "2.0" = "B 🟡"; "3.0" = "C 🟠"; "4.0" = "D 🔴"; "5.0" = "E ⛔" }

function Get-Rating([string] $val) {
    return $ratingMap[$val] ?? $val
}

$m = @{}
foreach ($measure in $measures.component.measures) {
    $m[$measure.metric] = $measure.value
}

$table = @(
    [PSCustomObject]@{ Métrica = "Lines of Code";       Valor = $m["ncloc"]                          }
    [PSCustomObject]@{ Métrica = "Bugs";                Valor = "$($m["bugs"]) — $(Get-Rating $m["reliability_rating"])" }
    [PSCustomObject]@{ Métrica = "Vulnerabilities";     Valor = "$($m["vulnerabilities"]) — $(Get-Rating $m["security_rating"])" }
    [PSCustomObject]@{ Métrica = "Code Smells";         Valor = "$($m["code_smells"]) — $(Get-Rating $m["sqale_rating"])" }
    [PSCustomObject]@{ Métrica = "Coverage";            Valor = "$($m["coverage"])%"                  }
    [PSCustomObject]@{ Métrica = "Duplicated Lines";    Valor = "$($m["duplicated_lines_density"])%"  }
)

$table | Format-Table -AutoSize

Write-Host "`n  Dashboard: $ServerUrl/dashboard?id=$ProjectKey`n" -ForegroundColor Cyan
