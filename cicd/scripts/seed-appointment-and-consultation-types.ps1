<#
.SYNOPSIS
    Seeds a hospital's Appointment Types and Consultation Types master data through the real
    Masters API - not a database migration, since this is tenant-specific reference data
    (every hospital sets its own fee schedule), not a schema change every tenant should share.

.DESCRIPTION
    Logs in as the given hospital's Super Admin, then creates each Appointment Type and
    Consultation Type below if a record with that exact name doesn't already exist. Safe to
    re-run - already-seeded rows are skipped, not duplicated. Neither entity has a Code field
    (removed from both this session - see DecisionLog), so only Name (+ Amount for
    Consultation Type) is sent.

    Goes through the same POST /api/v1/masters/appointment-types and
    /api/v1/masters/consultation-types endpoints the Masters UI itself uses, so this exercises
    the same validation and business logic a person clicking through the UI would.

.PARAMETER HospitalCode
    The tenant to seed (e.g. 'lhs', 'qa2' - whichever hospital was registered through the
    Platform Portal).

.PARAMETER Username
    That hospital's Super Admin username.

.PARAMETER Password
    That hospital's Super Admin password.

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.EXAMPLE
    ./seed-appointment-and-consultation-types.ps1 -HospitalCode lhs -Username lhsadmin -Password 'Lakshmi@123'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HospitalCode,

    [Parameter(Mandatory = $true)]
    [string]$Username,

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [string]$ApiBaseUrl = 'http://localhost:58158'
)

$ErrorActionPreference = 'Stop'

$appointmentTypes = @(
    'Regular (Walk-in)',
    'Online (Website/WhatsApp)',
    'Phone Call',
    'Scheduled'
)

# Amount is $null for "Others / On-call" - decided per visit instead of a fixed rate (the
# backend's CreateConsultationTypeRequest.Amount is nullable for exactly this case).
$consultationTypes = @(
    @{ Name = "Doctor's Consultation (In-house) - Regular"; Amount = 200 },
    @{ Name = "Doctor's Consultation (In-house) - Priority"; Amount = 300 },
    @{ Name = "Doctor's Consultation (Visiting) - Regular"; Amount = 250 },
    @{ Name = 'Emergency / Casualty Doctor''s Consultation'; Amount = 500 },
    @{ Name = "Doctor's Consultation - Others / On-call"; Amount = $null }
)

Write-Host "Logging in to hospital '$HospitalCode' as '$Username'..." -ForegroundColor Cyan
$loginHeaders = @{ 'Content-Type' = 'application/json'; 'X-Hospital-Code' = $HospitalCode }
$loginBody = @{ loginType = 'superAdmin'; username = $Username; password = $Password } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/v1/auth/login" -Headers $loginHeaders -Body $loginBody
$token = $loginResponse.data.token
if (-not $token) {
    throw "Login succeeded but no token was returned - check the response shape hasn't changed."
}
Write-Host "Signed in as $($loginResponse.data.user.username)." -ForegroundColor Green

$authHeaders = @{ 'Content-Type' = 'application/json'; Authorization = "Bearer $token" }

function Get-AllMasterRecords {
    param([string]$Entity)
    $uri = "$ApiBaseUrl/api/v1/masters/$Entity`?page=1&pageSize=500"
    $response = Invoke-RestMethod -Method Get -Uri $uri -Headers $authHeaders
    # Leading-comma trick: PowerShell auto-unrolls a returned array back down to its bare
    # element(s) as it crosses the function's output stream, so a 0- or 1-element @(...) here
    # would otherwise arrive at the caller as $null or a bare object despite the @() wrap - see
    # seed-departments-and-doctors.ps1's identical comment for the full explanation.
    return ,@($response.data)
}

Write-Host "Fetching existing appointment types and consultation types for idempotency checks..." -ForegroundColor Cyan
$existingAppointmentTypes = [System.Collections.Generic.List[object]]::new()
$existingAppointmentTypes.AddRange([object[]](Get-AllMasterRecords -Entity 'appointment-types'))
$existingConsultationTypes = [System.Collections.Generic.List[object]]::new()
$existingConsultationTypes.AddRange([object[]](Get-AllMasterRecords -Entity 'consultation-types'))

$createdAppointmentTypeCount = 0
$skippedAppointmentTypeCount = 0
$createdConsultationTypeCount = 0
$skippedConsultationTypeCount = 0

Write-Host "Appointment Types:" -ForegroundColor Cyan
foreach ($name in $appointmentTypes) {
    $existing = $existingAppointmentTypes | Where-Object { $_.name -eq $name } | Select-Object -First 1
    if ($existing) {
        Write-Host "  '$name' already exists - skipping." -ForegroundColor DarkGray
        $skippedAppointmentTypeCount++
        continue
    }
    $body = @{ name = $name; isActive = $true } | ConvertTo-Json
    $created = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/v1/masters/appointment-types" -Headers $authHeaders -Body $body
    Write-Host "  Created '$name'." -ForegroundColor Green
    $createdAppointmentTypeCount++
    $existingAppointmentTypes.Add($created.data)
}

Write-Host "Consultation Types:" -ForegroundColor Cyan
foreach ($entry in $consultationTypes) {
    $existing = $existingConsultationTypes | Where-Object { $_.name -eq $entry.Name } | Select-Object -First 1
    if ($existing) {
        Write-Host "  '$($entry.Name)' already exists - skipping." -ForegroundColor DarkGray
        $skippedConsultationTypeCount++
        continue
    }
    $bodyHash = @{ name = $entry.Name; isActive = $true }
    if ($null -ne $entry.Amount) {
        $bodyHash.amount = $entry.Amount
    }
    $body = $bodyHash | ConvertTo-Json
    $created = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/v1/masters/consultation-types" -Headers $authHeaders -Body $body
    $amountLabel = if ($null -ne $entry.Amount) { "Rs.$($entry.Amount)" } else { 'no fixed rate' }
    Write-Host "  Created '$($entry.Name)' ($amountLabel)." -ForegroundColor Green
    $createdConsultationTypeCount++
    $existingConsultationTypes.Add($created.data)
}

Write-Host ""
Write-Host "Done. Appointment Types: $createdAppointmentTypeCount created, $skippedAppointmentTypeCount already existed." -ForegroundColor Cyan
Write-Host "Consultation Types: $createdConsultationTypeCount created, $skippedConsultationTypeCount already existed." -ForegroundColor Cyan
