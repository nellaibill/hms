<#
  Docker Engine (Moby) for Windows Server 2025 - NOT Docker Desktop.
  Docker Desktop isn't the supported path on Server editions.

  This enables the Windows "Containers" feature, which requires a reboot,
  and installs the Docker Engine service. Run in two stages on purpose:
  stage 1 enables the feature and reboots; stage 2 (re-run this same script
  after reboot) installs the engine itself.

  Run this in an ELEVATED PowerShell (Run as Administrator).
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

$feature = Get-WindowsFeature -Name Containers

if (-not $feature.Installed) {
    Write-Host "Enabling the Windows 'Containers' feature. This requires a reboot." -ForegroundColor Yellow
    Install-WindowsFeature -Name Containers
    Write-Host ""
    Write-Host "Reboot now, then re-run this exact script to finish installing Docker Engine." -ForegroundColor Yellow
    $answer = Read-Host "Reboot now? (y/N)"
    if ($answer -eq 'y') {
        Restart-Computer
    } else {
        Write-Host "Reboot manually when ready, then re-run this script." -ForegroundColor Yellow
    }
    return
}

Write-Host "'Containers' feature already enabled - installing Docker Engine." -ForegroundColor Cyan

# Official Docker Engine install script for Windows (Moby) - not Docker Desktop.
Invoke-WebRequest -UseBasicParsing "https://raw.githubusercontent.com/microsoft/Windows-Containers/Main/helpful_tools/Install-DockerCE/install-docker-ce.ps1" -OutFile "$env:TEMP\install-docker-ce.ps1"
& "$env:TEMP\install-docker-ce.ps1"

Write-Host ""
Write-Host "Docker Engine installed. Verify with: docker version" -ForegroundColor Green
Write-Host "Then install the Compose plugin if the script above didn't already:" -ForegroundColor Green
Write-Host "  https://docs.docker.com/compose/install/linux/#install-the-plugin-manually (Windows binaries section)" -ForegroundColor Green
