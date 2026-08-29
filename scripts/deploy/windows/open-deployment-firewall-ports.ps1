<#
  Opens exactly the ports this deployment needs publicly reachable, and nothing else.
  PostgreSQL (5432) is deliberately never touched here — it must stay reachable only from
  localhost (the API running on the same VPS), never from the public internet.

  Run this in an ELEVATED PowerShell (Run as Administrator).

  Usage:
    powershell -ExecutionPolicy Bypass -File open-deployment-firewall-ports.ps1
    # or, to also open the API's own port for direct testing (see docs/Deployment.md):
    powershell -ExecutionPolicy Bypass -File open-deployment-firewall-ports.ps1 -ExposeApiPortDirectly
#>

[CmdletBinding()]
param(
    [int]$HttpPort = 80,
    [int]$ApiPort = 58158,
    [switch]$ExposeApiPortDirectly
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

function Add-RuleIfMissing($name, $port) {
    if (Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue) {
        Write-Host "Rule '$name' already exists — skipping." -ForegroundColor DarkGray
        return
    }
    New-NetFirewallRule -DisplayName $name -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow | Out-Null
    Write-Host "Opened TCP $port ('$name')." -ForegroundColor Green
}

# Reverse proxy (nginx): serves the React build and forwards /api/* internally.
# This is the only port that needs to be public for the app to work end to end.
Add-RuleIfMissing "HMS Reverse Proxy (HTTP)" $HttpPort

if ($ExposeApiPortDirectly) {
    # Optional: lets you curl/Swagger the API directly on its own port, bypassing nginx,
    # for testing. Not required for the app itself to work through the reverse proxy.
    Add-RuleIfMissing "HMS API (direct)" $ApiPort
}

Write-Host ""
Write-Host "PostgreSQL port 5432 was NOT opened — it stays private to this machine by design." -ForegroundColor Yellow
Write-Host "Current inbound rules matching this deployment:" -ForegroundColor Cyan
Get-NetFirewallRule -DisplayName "HMS*" | Select-Object DisplayName, Enabled, Direction, Action
