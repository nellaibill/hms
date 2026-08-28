<#
.SYNOPSIS
    Seeds a hospital's Diagnostic Test (Laboratory/Radiology tariff) master data through the
    real Masters API, from a JSON file - not a database migration, since this is tenant-specific
    reference data (every hospital sets its own price list), not a schema change every tenant
    should share.

.DESCRIPTION
    Logs in as the given hospital's Super Admin, then creates each row in the seed file if a
    record with that exact (Name, ServiceType, IsOutsourced) combination doesn't already exist
    - the same uniqueness scope the backend enforces (see DiagnosticTestConfiguration's unique
    index), since the same test name can legitimately appear twice: once priced in-house, once
    priced through an outsourced reference lab. Safe to re-run - already-seeded rows are
    skipped, not duplicated.

    Goes through the same POST /api/v1/masters/diagnostic-tests endpoint the Masters UI itself
    uses, so this exercises the same validation and business logic a person clicking through
    the UI would. Reads rows from -SeedFile (default lab-tests-seed.json, generated from the
    hospital's tariff spreadsheet) rather than an inline array, since there are ~300 of them -
    too many to hardcode like seed-appointment-and-consultation-types.ps1 does.

.PARAMETER HospitalCode
    The tenant to seed (e.g. 'lhs', 'qa2' - whichever hospital was registered through the
    Platform Portal).

.PARAMETER Username
    That hospital's Super Admin username.

.PARAMETER Password
    That hospital's Super Admin password.

.PARAMETER SeedFile
    Path to the JSON array of { name, serviceType, category, price, isOutsourced, referenceLab }
    rows to seed. Defaults to lab-tests-seed.json next to this script.

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.EXAMPLE
    ./seed-diagnostic-tests.ps1 -HospitalCode lhs -Username lhsadmin -Password 'Lakshmi@123'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$HospitalCode,

    [Parameter(Mandatory = $true)]
    [string]$Username,

    [Parameter(Mandatory = $true)]
    [string]$Password,

    [string]$SeedFile = (Join-Path $PSScriptRoot 'lab-tests-seed.json'),

    [string]$ApiBaseUrl = 'http://localhost:58158'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $SeedFile)) {
    throw "Seed file not found: $SeedFile"
}
$seedRows = Get-Content $SeedFile -Raw | ConvertFrom-Json
Write-Host "Loaded $($seedRows.Count) diagnostic test rows from '$SeedFile'." -ForegroundColor Cyan

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

function Get-AllDiagnosticTests {
    # The server clamps pageSize to 100 (PagedRequest.MaxPageSize) regardless of what's
    # requested, so with ~300 rows this has to walk every page rather than ask for one huge
    # page - same fix as the frontend's masterStoreFactory.ts getAll().
    $all = [System.Collections.Generic.List[object]]::new()
    $page = 1
    do {
        # `_` cache-busts the request - Invoke-RestMethod on Windows PowerShell 5.1 can serve a
        # stale cached response for a repeated identical URL (WinINet-backed), which previously
        # made this loop under-report a growing result set mid-run.
        $cacheBust = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
        $uri = "$ApiBaseUrl/api/v1/masters/diagnostic-tests`?page=$page&pageSize=100&_=$cacheBust"
        $response = Invoke-RestMethod -Method Get -Uri $uri -Headers $authHeaders
        # Leading-comma trick: PowerShell auto-unrolls a returned array back down to its bare
        # element(s) as it crosses the function's output stream, so a 0- or 1-element @(...)
        # would otherwise arrive at the caller as $null or a bare object - see
        # seed-departments-and-doctors.ps1's identical comment for the full explanation.
        $all.AddRange([object[]](, @($response.data)))
        $totalPages = $response.meta.totalPages
        $page++
    } while ($page -le $totalPages)
    # Same leading-comma trick applied to the *returned* List itself: without it, PowerShell
    # enumerates the List as it crosses the function's output stream and the caller receives a
    # plain fixed-size Object[] instead of the mutable List - then $existing.Add(...) below
    # throws "Collection was of a fixed size." (this bit a real seeding run: creates still
    # succeeded server-side, but got mis-logged as failures because the Add() after each POST
    # threw before the loop could report success).
    return , $all
}

Write-Host "Fetching existing diagnostic tests for idempotency checks..." -ForegroundColor Cyan
$existing = Get-AllDiagnosticTests
Write-Host "Found $($existing.Count) existing diagnostic tests." -ForegroundColor Cyan

function Test-ExistingMatch {
    param($Row)
    return $existing | Where-Object {
        $_.name -eq $Row.name -and $_.serviceType -eq $Row.serviceType -and [bool]$_.isOutsourced -eq [bool]$Row.isOutsourced
    } | Select-Object -First 1
}

$createdCount = 0
$skippedCount = 0
$failedCount = 0

# The API's global rate limiter allows 200 requests/minute per client IP (RateLimitingConfiguration.cs)
# - shared with whatever else is hitting this dev API from the same machine (browser tabs
# included), so throttle well under that and back off + retry on a 429 rather than treating it
# as a hard failure.
foreach ($row in $seedRows) {
    if (Test-ExistingMatch -Row $row) {
        $skippedCount++
        continue
    }

    $bodyHash = @{
        name          = $row.name
        serviceType   = $row.serviceType
        category      = $row.category
        price         = $row.price
        isOutsourced  = [bool]$row.isOutsourced
        isActive      = $true
    }
    if ($row.referenceLab) {
        $bodyHash.referenceLab = $row.referenceLab
    }
    $body = $bodyHash | ConvertTo-Json

    $attempt = 0
    $done = $false
    while (-not $done -and $attempt -lt 3) {
        $attempt++
        try {
            $created = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests" -Headers $authHeaders -Body $body
            Write-Host "  Created '$($row.name)' [$($row.serviceType)$(if ($row.isOutsourced) { ', outsourced' })] - Rs.$($row.price)" -ForegroundColor Green
            $createdCount++
            $existing.Add($created.data)
            $done = $true
        }
        catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            if ($statusCode -eq 429 -and $attempt -lt 3) {
                Write-Host "  Rate limited - waiting 65s before retrying '$($row.name)' (attempt $attempt)..." -ForegroundColor Yellow
                Start-Sleep -Seconds 65
                continue
            }
            Write-Host "  FAILED '$($row.name)': $($_.Exception.Message)" -ForegroundColor Red
            $failedCount++
            $done = $true
        }
    }
    # 350ms/request caps this script at ~170 req/min, leaving headroom under the 200/min
    # global limit for whatever else (browser tabs) shares this client IP.
    Start-Sleep -Milliseconds 350
}

Write-Host ""
Write-Host "Done. Diagnostic Tests: $createdCount created, $skippedCount already existed, $failedCount failed." -ForegroundColor Cyan
