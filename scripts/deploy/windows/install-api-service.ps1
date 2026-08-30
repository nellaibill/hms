<#
  Installs the published HMS.Api as a Windows Service (via NSSM) so it runs continuously,
  survives reboots, and restarts on crash - the native-Windows equivalent of a systemd unit
  or Docker's `restart: unless-stopped`. No application code changes required: this wraps
  the exact same `dotnet HMS.Api.dll` process `dotnet run` uses in dev, just published and
  configured entirely through environment variables (the same Section__Key convention
  docker-compose.yml already uses for the Docker deployment - see docs/Configuration.md).

  Run this in an ELEVATED PowerShell (Run as Administrator), after `dotnet publish` (see
  docs/Deployment.md's Manual Deployment Steps for the full sequence).

  Usage:
    powershell -ExecutionPolicy Bypass -File install-api-service.ps1 `
      -PublishDir "C:\hms\backend\publish" `
      -ApiPort 58158 `
      -PublicOrigin "http://162.35.105.234" `
      -JwtSigningKey "<paste a generated key>" `
      -SuperAdminPassword "<...>" `
      -PlatformAdminPassword "<...>"
#>

[CmdletBinding()]
param(
    [string]$PublishDir = "C:\hms\backend\publish",
    [int]$ApiPort = 58158,
    [Parameter(Mandatory = $true)]
    [string]$PublicOrigin,
    [Parameter(Mandatory = $true)]
    [string]$JwtSigningKey,
    [Parameter(Mandatory = $true)]
    [string]$SuperAdminPassword,
    [Parameter(Mandatory = $true)]
    [string]$PlatformAdminPassword,
    [string]$PgUser = "hms",
    [string]$PgPassword = "hms",
    [string]$ServiceName = "HmsApi"
)

$ErrorActionPreference = 'Stop'

function Assert-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p  = New-Object Security.Principal.WindowsPrincipal($id)
    if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated (Administrator) PowerShell."
    }
}
Assert-Admin

if (-not (Test-Path (Join-Path $PublishDir "HMS.Api.dll"))) {
    throw "HMS.Api.dll not found in $PublishDir - run 'dotnet publish' first (see docs/Deployment.md)."
}

if (-not (Get-Command nssm -ErrorAction SilentlyContinue)) {
    Write-Host "Installing NSSM (Windows service wrapper) via Chocolatey..." -ForegroundColor Cyan
    choco install nssm -y
}

$dotnetPath = (Get-Command dotnet).Source

if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Service '$ServiceName' already exists - stopping and removing it first." -ForegroundColor Yellow
    nssm stop $ServiceName confirm | Out-Null
    nssm remove $ServiceName confirm | Out-Null
}

Write-Host "Installing service '$ServiceName'..." -ForegroundColor Cyan
nssm install $ServiceName $dotnetPath "`"$PublishDir\HMS.Api.dll`""
nssm set $ServiceName AppDirectory $PublishDir
nssm set $ServiceName Start SERVICE_AUTO_START
nssm set $ServiceName AppStdout (Join-Path $PublishDir "service.out.log")
nssm set $ServiceName AppStderr (Join-Path $PublishDir "service.err.log")
nssm set $ServiceName AppRotateFiles 1

# Kept as ASPNETCORE_ENVIRONMENT=Development deliberately, matching the working setup
# already validated by hand earlier in this deployment: it auto-applies EF Core migrations
# and seeds Platform Admin / the legacy tenant on every startup (Program.cs), same as
# `dotnet run` does. This is an MVP deployment (per docs/Deployment.md - no secrets-manager
# integration exists yet either), not a hardened production posture; see that doc for the
# `dotnet HMS.Api.dll migrate` alternative if you want migrations decoupled from app startup.
$envVars = @(
    "ASPNETCORE_ENVIRONMENT=Development",
    "ASPNETCORE_URLS=http://0.0.0.0:$ApiPort",
    "ConnectionStrings__Default=Host=localhost;Port=5432;Database=hms_qa;Username=$PgUser;Password=$PgPassword",
    "ConnectionStrings__Platform=Host=localhost;Port=5432;Database=hms_platform;Username=$PgUser;Password=$PgPassword",
    "ConnectionStrings__PlatformAdmin=Host=localhost;Port=5432;Database=postgres;Username=$PgUser;Password=$PgPassword",
    "Jwt__SigningKey=$JwtSigningKey",
    "SuperAdminSeed__Password=$SuperAdminPassword",
    "PlatformAdminSeed__Password=$PlatformAdminPassword",
    "Cors__AllowedOrigins__0=$PublicOrigin"
)
nssm set $ServiceName AppEnvironmentExtra ($envVars -join "`0")

Write-Host "Starting service '$ServiceName'..." -ForegroundColor Cyan
nssm start $ServiceName

Write-Host ""
Write-Host "Done. Verify with:" -ForegroundColor Green
Write-Host "  Get-Service $ServiceName" -ForegroundColor Green
Write-Host "  Get-Content '$PublishDir\service.out.log' -Tail 40" -ForegroundColor Green
Write-Host "  curl http://localhost:$ApiPort/health" -ForegroundColor Green
