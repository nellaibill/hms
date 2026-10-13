<#
  Opens exactly the ports this deployment needs publicly reachable, and nothing else.
  PostgreSQL (5432) is deliberately never touched here - it must stay reachable only from
  localhost (the API running on the same VPS), never from the public internet.

  The API's own port stays closed too: nginx reaches it over localhost, and opening it serves
  the API to the internet over plain HTTP, past the proxy. If a rule for it already exists
  (an earlier -ExposeApiPortDirectly run, or one added by hand for a `dotnet run` session),
  this script says so; -CloseApiPort removes it.

  Run this in an ELEVATED PowerShell (Run as Administrator).

  Usage:
    powershell -ExecutionPolicy Bypass -File open-deployment-firewall-ports.ps1
    # remove any inbound rule that opens the API port:
    powershell -ExecutionPolicy Bypass -File open-deployment-firewall-ports.ps1 -CloseApiPort
    # or, to open the API's own port for direct testing (see docs/Deployment.md):
    powershell -ExecutionPolicy Bypass -File open-deployment-firewall-ports.ps1 -ExposeApiPortDirectly
#>

[CmdletBinding()]
param(
    [int]$HttpPort = 80,
    [int]$ApiPort = 58158,
    [switch]$ExposeApiPortDirectly,
    [switch]$CloseApiPort
)

$ErrorActionPreference = 'Stop'

if ($ExposeApiPortDirectly -and $CloseApiPort) {
    throw "-ExposeApiPortDirectly and -CloseApiPort are opposites; pass one or neither."
}

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
        Write-Host "Rule '$name' already exists - skipping." -ForegroundColor DarkGray
        return
    }
    New-NetFirewallRule -DisplayName $name -Direction Inbound -Protocol TCP -LocalPort $port -Action Allow | Out-Null
    Write-Host "Opened TCP $port ('$name')." -ForegroundColor Green
}

# Enabled inbound allow rules naming exactly this TCP port, whatever they are called. A rule
# that covers the port through a range or "Any" is not matched.
function Get-RulesOpeningPort($port) {
    Get-NetFirewallPortFilter -Protocol TCP |
        Where-Object { @($_.LocalPort) -contains "$port" } |
        Get-NetFirewallRule |
        Where-Object { $_.Direction -eq 'Inbound' -and $_.Action -eq 'Allow' -and $_.Enabled -eq 'True' }
}

# Reverse proxy (nginx): serves the React build and forwards /api/* internally.
# This is the only port that needs to be public for the app to work end to end.
Add-RuleIfMissing "HMS Reverse Proxy (HTTP)" $HttpPort

if ($ExposeApiPortDirectly) {
    # Optional: lets you curl the API directly on its own port, bypassing nginx, for testing.
    # Not required for the app itself to work through the reverse proxy, and it only has an
    # effect if the API also listens beyond localhost (install-api-service.ps1 has the same
    # switch).
    Add-RuleIfMissing "HMS API (direct)" $ApiPort
}
else {
    $apiRules = @(Get-RulesOpeningPort $ApiPort)
    if ($apiRules.Count -gt 0 -and $CloseApiPort) {
        foreach ($rule in $apiRules) {
            Remove-NetFirewallRule -Name $rule.Name
            Write-Host "Removed '$($rule.DisplayName)' - TCP $ApiPort is no longer open." -ForegroundColor Green
        }
    }
    elseif ($apiRules.Count -gt 0) {
        Write-Host ""
        Write-Host "WARNING: TCP $ApiPort (the API's own port) is open to inbound connections by:" -ForegroundColor Red
        $apiRules | ForEach-Object { Write-Host "  - $($_.DisplayName)" -ForegroundColor Red }
        Write-Host "The API is then reachable over plain HTTP without going through nginx." -ForegroundColor Red
        Write-Host "Re-run with -CloseApiPort to remove the rule(s)." -ForegroundColor Red
    }
    elseif ($CloseApiPort) {
        Write-Host "No inbound rule opens TCP $ApiPort - nothing to remove." -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "PostgreSQL port 5432 was NOT opened - it stays private to this machine by design." -ForegroundColor Yellow
Write-Host "Current inbound rules matching this deployment:" -ForegroundColor Cyan
Get-NetFirewallRule -DisplayName "HMS*" | Select-Object DisplayName, Enabled, Direction, Action
