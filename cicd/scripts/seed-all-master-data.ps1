<#
.SYNOPSIS
    Runs every master-data seed script for one hospital in a single pass: Departments +
    Consultants, Appointment Types + Consultation Types, and Diagnostic Tests (Laboratory/
    Radiology tariffs). Each underlying script is already idempotent (skips rows that
    already exist), so this is safe to re-run.

.DESCRIPTION
    A thin wrapper - no seeding logic of its own - that forwards the same
    HospitalCode/Username/Password/ApiBaseUrl to seed-departments-and-doctors.ps1,
    seed-appointment-and-consultation-types.ps1, and seed-diagnostic-tests.ps1 in sequence,
    so a fresh tenant's master data can be seeded with one command instead of three.

.PARAMETER HospitalCode
    The tenant to seed (e.g. 'legacy', 'lhs', 'qa2' - whichever hospital was registered
    through the Platform Portal or the legacy tenant seed).

.PARAMETER Username
    That hospital's Super Admin username.

.PARAMETER Password
    That hospital's Super Admin password.

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.PARAMETER DiagnosticTestsSeedFile
    Passed through to seed-diagnostic-tests.ps1. Defaults to lab-tests-seed.json next to
    these scripts.

.EXAMPLE
    ./seed-all-master-data.ps1 -HospitalCode legacy -Username superadmin -Password 'Xxx123!'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HospitalCode,

    [Parameter(Mandatory = $true)]
    [string]$Username,

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [string]$ApiBaseUrl = 'http://localhost:58158',

    [string]$DiagnosticTestsSeedFile = (Join-Path $PSScriptRoot 'lab-tests-seed.json')
)

$ErrorActionPreference = 'Stop'

$commonArgs = @{
    HospitalCode = $HospitalCode
    Username     = $Username
    Password     = $Password
    ApiBaseUrl   = $ApiBaseUrl
}

Write-Host "=== 1/3: Departments and Consultants ===" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'seed-departments-and-doctors.ps1') @commonArgs

Write-Host ""
Write-Host "=== 2/3: Appointment Types and Consultation Types ===" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'seed-appointment-and-consultation-types.ps1') @commonArgs

Write-Host ""
Write-Host "=== 3/3: Diagnostic Tests (Laboratory/Radiology) ===" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'seed-diagnostic-tests.ps1') @commonArgs -SeedFile $DiagnosticTestsSeedFile

Write-Host ""
Write-Host "All master data seeded for hospital '$HospitalCode'." -ForegroundColor Green
