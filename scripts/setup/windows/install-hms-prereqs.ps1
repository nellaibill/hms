<#
  HMS native dev environment - batch installer for Windows Server 2025.
  Run this in an ELEVATED PowerShell (Run as Administrator).

  Installs: Git, .NET 10 SDK, dotnet-ef, Node.js 20 LTS, PostgreSQL 16, VS Code.
  Docker is handled separately at the bottom because enabling the Windows
  "Containers" feature forces a reboot - don't want that happening as a
  side effect of an unattended batch run.

  Usage:
    powershell -ExecutionPolicy Bypass -File install-hms-prereqs.ps1
#>

$ErrorActionPreference = 'Stop'

function Assert-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $p  = New-Object Security.Principal.WindowsPrincipal($id)
    if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated (Administrator) PowerShell."
    }
}
Assert-Admin

# ------------------------------------------------------------
# 1. Chocolatey (package manager)
# ------------------------------------------------------------
if (-not (Get-Command choco -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Chocolatey..." -ForegroundColor Cyan
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    $env:Path += ";$env:ProgramData\chocolatey\bin"
} else {
    Write-Host "Chocolatey already installed." -ForegroundColor DarkGray
}

# ------------------------------------------------------------
# 2. Core tooling + backend + frontend + database
# ------------------------------------------------------------
$packages = @(
    'git',
    'dotnet-10.0-sdk',   # .NET 10 SDK - matches Directory.Build.props TargetFramework net10.0
    'nodejs-lts',        # Node.js 20.x LTS - matches frontend/web/Dockerfile's node:20-alpine
    'postgresql16',      # matches docker-compose.yml's postgres:16-alpine
    'vscode'             # optional editor; skip if you already have Visual Studio 2022
)

foreach ($pkg in $packages) {
    Write-Host "Installing $pkg..." -ForegroundColor Cyan
    choco install $pkg -y
}

# Refresh PATH in this session so subsequent commands (dotnet, npm) resolve
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

# ------------------------------------------------------------
# 3. EF Core CLI tool (for migrations)
# ------------------------------------------------------------
Write-Host "Installing dotnet-ef..." -ForegroundColor Cyan
dotnet tool install --global dotnet-ef

Write-Host ""
Write-Host "Done: Git, .NET 10 SDK, dotnet-ef, Node.js LTS, PostgreSQL 16, VS Code installed." -ForegroundColor Green
Write-Host "Note: postgresql16's Chocolatey package sets its own 'postgres' superuser password" -ForegroundColor Yellow
Write-Host "      during install (or prompts for one) - check the choco install output above." -ForegroundColor Yellow
Write-Host ""
Write-Host "Docker was NOT installed by this script - see install-docker-engine.ps1" -ForegroundColor Yellow
Write-Host "(separate step: it enables a Windows feature and requires a reboot)." -ForegroundColor Yellow
