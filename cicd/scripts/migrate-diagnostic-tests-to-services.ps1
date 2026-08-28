<#
.SYNOPSIS
    One-time data migration: copies a hospital's Laboratory/Radiology DiagnosticTest rows into
    the new normalized DiagnosticCategory/DiagnosticProvider/DiagnosticService masters, then
    deactivates the migrated DiagnosticTest rows. Procedure-type DiagnosticTest rows are left
    completely untouched (never read, never modified) - Procedure billing stays on the old
    entity.

.DESCRIPTION
    Logs in as the given hospital's Super Admin (same X-Hospital-Code/loginType=superAdmin
    pattern as seed-diagnostic-tests.ps1), then:

      1. Pages through GET /api/v1/masters/diagnostic-tests (respecting the server's
         MaxPageSize clamp, same walk-every-page approach seed-diagnostic-tests.ps1 uses).
      2. For Laboratory/Radiology rows, collects distinct Category strings - scoped per
         ServiceType, so a category name that happens to appear under both Laboratory and
         Radiology produces two separate DiagnosticCategory rows, never one merged row - and
         distinct ReferenceLab strings (where IsOutsourced is true, not type-scoped - a
         reference lab can equally outsource Laboratory and Radiology work). POSTs a
         DiagnosticCategory/DiagnosticProvider for each, skipping any whose Code or Name
         already exists (checked via GET first, so this script is safe to re-run).
      3. POSTs a DiagnosticService per migrated DiagnosticTest row, wiring up the right
         CategoryId/ProviderId. Skips a row if a matching DiagnosticService (same Name,
         ServiceType, IsOutsourced) already exists.
      4. PUTs each migrated DiagnosticTest row back with IsActive=false (deactivate, never
         delete - the historical billing rows that reference it must keep resolving).
      5. Prints a created/skipped/deactivated summary.

    A DiagnosticTest row with a blank Category, or IsOutsourced=true with a blank
    ReferenceLab, has no natural category/provider to attach to - rather than fail the row
    outright, this script falls back to a per-type "Uncategorized" category / a single
    "Unspecified Provider" provider (created on first use) so the row still migrates. Recode
    these by hand afterward if that's not what you want.

    Code values for created categories/providers/services are derived from the Name (upper-
    cased, non-alphanumeric characters stripped, truncated to fit the 20-char column, with a
    numeric suffix if that collides with a code already in use) - "keep it simple, a human can
    recode later" per the rollout plan.

.PARAMETER HospitalCode
    The tenant to migrate (e.g. 'lhs', 'qa2' - whichever hospital was registered through the
    Platform Portal).

.PARAMETER Username
    That hospital's Super Admin username.

.PARAMETER Password
    That hospital's Super Admin password.

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.EXAMPLE
    ./migrate-diagnostic-tests-to-services.ps1 -HospitalCode lhs -Username lhsadmin -Password 'Lakshmi@123'
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

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Get-AllPaged {
    param([string]$Entity)
    # The server clamps pageSize to 100 (PagedRequest.MaxPageSize) regardless of what's
    # requested, so this has to walk every page - same approach seed-diagnostic-tests.ps1 uses.
    $all = [System.Collections.Generic.List[object]]::new()
    $page = 1
    do {
        # `_` cache-busts the request - Invoke-RestMethod on Windows PowerShell 5.1 can serve a
        # stale cached response for a repeated identical URL (WinINet-backed).
        $cacheBust = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
        $uri = "$ApiBaseUrl/api/v1/masters/$Entity`?page=$page&pageSize=100&_=$cacheBust"
        $response = Invoke-RestMethod -Method Get -Uri $uri -Headers $authHeaders
        # No leading-comma trick here - unlike the `return , $all` below, this AddRange call
        # doesn't cross a function output/pipeline boundary, so @($response.data) alone already
        # gives a flat array of this page's items. Adding the leading comma here (as an earlier
        # version of this script did) wraps the whole per-page array as a single nested element
        # instead of flattening it, so AddRange adds exactly one (wrong) item per page instead of
        # up to 100 - confirmed live: it silently under-fetched every paged entity in this script.
        $all.AddRange([object[]]@($response.data))
        $totalPages = $response.meta.totalPages
        $page++
    } while ($page -le $totalPages)
    # Same leading-comma trick applied to the *returned* List itself - without it the caller
    # receives a plain fixed-size Object[] instead of the mutable List, and $list.Add(...)
    # later throws "Collection was of a fixed size."
    return , $all
}

function Get-Slug {
    param([string]$Name, [int]$MaxLength = 20)
    $slug = ($Name.ToUpperInvariant() -replace '[^A-Z0-9]', '')
    if ([string]::IsNullOrEmpty($slug)) {
        $slug = 'X'
    }
    if ($slug.Length -gt $MaxLength) {
        $slug = $slug.Substring(0, $MaxLength)
    }
    return $slug
}

function Get-UniqueCode {
    param([string]$Name, [System.Collections.Generic.HashSet[string]]$UsedCodes)
    $base = Get-Slug -Name $Name -MaxLength 18
    $code = $base
    $suffix = 2
    while ($UsedCodes.Contains($code)) {
        $code = "$base$suffix"
        if ($code.Length -gt 20) {
            $code = $code.Substring(0, 20)
        }
        $suffix++
    }
    [void]$UsedCodes.Add($code)
    return $code
}

function Invoke-ApiPostWithRetry {
    param([string]$Uri, [string]$Body, [string]$Label)
    # The API's global rate limiter allows 200 requests/minute per client IP - back off and
    # retry on 429 rather than treating it as a hard failure, same as seed-diagnostic-tests.ps1.
    $attempt = 0
    while ($true) {
        $attempt++
        try {
            return Invoke-RestMethod -Method Post -Uri $Uri -Headers $authHeaders -Body $Body
        }
        catch {
            $statusCode = $_.Exception.Response.StatusCode.value__
            if ($statusCode -eq 429 -and $attempt -lt 3) {
                Write-Host "  Rate limited - waiting 65s before retrying '$Label' (attempt $attempt)..." -ForegroundColor Yellow
                Start-Sleep -Seconds 65
                continue
            }
            throw
        }
    }
}

# ---------------------------------------------------------------------------
# Step 1: fetch every DiagnosticTest row
# ---------------------------------------------------------------------------

Write-Host "Fetching all diagnostic tests..." -ForegroundColor Cyan
$allTests = Get-AllPaged -Entity 'diagnostic-tests'
$migratableTests = $allTests | Where-Object { $_.serviceType -in @('Laboratory', 'Radiology') }
Write-Host "Found $($allTests.Count) diagnostic tests total; $($migratableTests.Count) are Laboratory/Radiology and will be migrated (Procedure rows are left untouched)." -ForegroundColor Cyan

if ($migratableTests.Count -eq 0) {
    Write-Host "Nothing to migrate. Done." -ForegroundColor Cyan
    return
}

# ---------------------------------------------------------------------------
# Step 2: categories (scoped per ServiceType) and providers
# ---------------------------------------------------------------------------

Write-Host "Fetching existing categories/providers/services for idempotency checks..." -ForegroundColor Cyan
$existingCategories = Get-AllPaged -Entity 'diagnostic-categories'
$existingProviders = Get-AllPaged -Entity 'diagnostic-providers'
$existingServices = Get-AllPaged -Entity 'diagnostic-services'

$usedCategoryCodes = [System.Collections.Generic.HashSet[string]]::new([string[]]($existingCategories | ForEach-Object { $_.code }))
$usedProviderCodes = [System.Collections.Generic.HashSet[string]]::new([string[]]($existingProviders | ForEach-Object { $_.code }))
$usedServiceCodes = [System.Collections.Generic.HashSet[string]]::new([string[]]($existingServices | ForEach-Object { $_.code }))

$categoriesCreated = 0
$providersCreated = 0
$servicesCreated = 0
$servicesSkipped = 0
$testsDeactivated = 0

# key: "<ServiceType>|<CategoryName>" -> categoryId. Deliberately scoped per ServiceType so a
# category string shared between Laboratory and Radiology rows still produces two rows.
$categoryIdByKey = @{}
# Names already claimed by an earlier-processed ServiceType this run, so a same-named category
# for the *other* type is disambiguated (suffixed) instead of accidentally reusing the row.
$categoryNamesClaimed = [System.Collections.Generic.HashSet[string]]::new()

function Get-OrCreateCategory {
    param([string]$ServiceType, [string]$CategoryName)

    $rawName = if ([string]::IsNullOrWhiteSpace($CategoryName)) { 'Uncategorized' } else { $CategoryName.Trim() }
    $key = "$ServiceType|$rawName"
    if ($categoryIdByKey.ContainsKey($key)) {
        return $categoryIdByKey[$key]
    }

    $effectiveName = $rawName
    if ($categoryNamesClaimed.Contains($rawName)) {
        $effectiveName = "$rawName ($ServiceType)"
    }

    $existing = $existingCategories | Where-Object { $_.name -eq $effectiveName } | Select-Object -First 1
    if ($existing) {
        $categoryIdByKey[$key] = $existing.id
        [void]$categoryNamesClaimed.Add($rawName)
        return $existing.id
    }

    $code = Get-UniqueCode -Name $effectiveName -UsedCodes $usedCategoryCodes
    $body = @{ code = $code; name = $effectiveName; isActive = $true } | ConvertTo-Json
    $created = Invoke-ApiPostWithRetry -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-categories" -Body $body -Label $effectiveName
    Write-Host "  Created category '$effectiveName' [$ServiceType] (code $code)." -ForegroundColor Green
    $script:categoriesCreated++
    Start-Sleep -Milliseconds 350

    $existingCategories.Add($created.data)
    $categoryIdByKey[$key] = $created.data.id
    [void]$categoryNamesClaimed.Add($rawName)
    return $created.data.id
}

# key: ReferenceLab name -> providerId. Not type-scoped - the same reference lab can serve
# both Laboratory and Radiology outsourcing.
$providerIdByName = @{}

function Get-OrCreateProvider {
    param([string]$ReferenceLabName)

    $rawName = if ([string]::IsNullOrWhiteSpace($ReferenceLabName)) { 'Unspecified Provider' } else { $ReferenceLabName.Trim() }
    if ($providerIdByName.ContainsKey($rawName)) {
        return $providerIdByName[$rawName]
    }

    $existing = $existingProviders | Where-Object { $_.name -eq $rawName } | Select-Object -First 1
    if ($existing) {
        $providerIdByName[$rawName] = $existing.id
        return $existing.id
    }

    $code = Get-UniqueCode -Name $rawName -UsedCodes $usedProviderCodes
    $body = @{ code = $code; name = $rawName; isActive = $true } | ConvertTo-Json
    $created = Invoke-ApiPostWithRetry -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-providers" -Body $body -Label $rawName
    Write-Host "  Created provider '$rawName' (code $code)." -ForegroundColor Green
    $script:providersCreated++
    Start-Sleep -Milliseconds 350

    $existingProviders.Add($created.data)
    $providerIdByName[$rawName] = $created.data.id
    return $created.data.id
}

Write-Host "Categories and providers:" -ForegroundColor Cyan
foreach ($testRow in $migratableTests) {
    [void](Get-OrCreateCategory -ServiceType $testRow.serviceType -CategoryName $testRow.category)
    if ([bool]$testRow.isOutsourced) {
        [void](Get-OrCreateProvider -ReferenceLabName $testRow.referenceLab)
    }
}

# ---------------------------------------------------------------------------
# Step 3: diagnostic services (one per migratable DiagnosticTest row)
# ---------------------------------------------------------------------------

Write-Host "Diagnostic services:" -ForegroundColor Cyan
foreach ($testRow in $migratableTests) {
    $isOutsourced = [bool]$testRow.isOutsourced
    $alreadyExists = $existingServices | Where-Object {
        $_.name -eq $testRow.name -and $_.serviceType -eq $testRow.serviceType -and [bool]$_.isOutsourced -eq $isOutsourced
    } | Select-Object -First 1
    if ($alreadyExists) {
        Write-Host "  '$($testRow.name)' [$($testRow.serviceType)] already migrated - skipping." -ForegroundColor DarkGray
        $servicesSkipped++
        continue
    }

    $categoryId = Get-OrCreateCategory -ServiceType $testRow.serviceType -CategoryName $testRow.category
    $providerId = $null
    if ($isOutsourced) {
        $providerId = Get-OrCreateProvider -ReferenceLabName $testRow.referenceLab
    }

    $code = Get-UniqueCode -Name $testRow.name -UsedCodes $usedServiceCodes
    $bodyHash = @{
        code         = $code
        name         = $testRow.name
        categoryId   = $categoryId
        serviceType  = $testRow.serviceType
        isOutsourced = $isOutsourced
        price        = $testRow.price
        isActive     = $true
    }
    if ($providerId) {
        $bodyHash.providerId = $providerId
    }
    $body = $bodyHash | ConvertTo-Json

    try {
        $created = Invoke-ApiPostWithRetry -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services" -Body $body -Label $testRow.name
        Write-Host "  Created service '$($testRow.name)' [$($testRow.serviceType)$(if ($isOutsourced) { ', outsourced' })] - Rs.$($testRow.price)" -ForegroundColor Green
        $servicesCreated++
        $existingServices.Add($created.data)
    }
    catch {
        Write-Host "  FAILED to create service '$($testRow.name)': $($_.Exception.Message)" -ForegroundColor Red
    }
    Start-Sleep -Milliseconds 350
}

# ---------------------------------------------------------------------------
# Step 4: deactivate the migrated DiagnosticTest rows
# ---------------------------------------------------------------------------

Write-Host "Deactivating migrated diagnostic tests:" -ForegroundColor Cyan
foreach ($testRow in $migratableTests) {
    if ($testRow.isActive -eq $false) {
        Write-Host "  '$($testRow.name)' already inactive - skipping." -ForegroundColor DarkGray
        continue
    }

    $bodyHash = @{
        name         = $testRow.name
        serviceType  = $testRow.serviceType
        price        = $testRow.price
        isOutsourced = [bool]$testRow.isOutsourced
        isActive     = $false
    }
    if ($testRow.category) {
        $bodyHash.category = $testRow.category
    }
    if ($testRow.referenceLab) {
        $bodyHash.referenceLab = $testRow.referenceLab
    }
    $body = $bodyHash | ConvertTo-Json

    try {
        Invoke-RestMethod -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests/$($testRow.id)" -Headers $authHeaders -Body $body | Out-Null
        Write-Host "  Deactivated '$($testRow.name)' [$($testRow.serviceType)]." -ForegroundColor Green
        $testsDeactivated++
    }
    catch {
        Write-Host "  FAILED to deactivate '$($testRow.name)': $($_.Exception.Message)" -ForegroundColor Red
    }
    Start-Sleep -Milliseconds 350
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "Done." -ForegroundColor Cyan
Write-Host "  Categories created: $categoriesCreated" -ForegroundColor Cyan
Write-Host "  Providers created:  $providersCreated" -ForegroundColor Cyan
Write-Host "  Services created:   $servicesCreated (skipped $servicesSkipped already-migrated)" -ForegroundColor Cyan
Write-Host "  Tests deactivated:  $testsDeactivated" -ForegroundColor Cyan
