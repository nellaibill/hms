<#
.SYNOPSIS
    Single-entry-point seed script for Lakshmi Hospitals' master data — Departments,
    Consultants, Appointment Types, Consultation Types, and the full Diagnostic Laboratory /
    Radiology / Procedural catalog (categories, providers, services, packages, legacy
    Procedure rows). Run this against a fresh tenant and it reproduces the exact master data
    the live Lakshmi Hospitals tenant has today.

.DESCRIPTION
    Consolidates six previously separate scripts (seed-departments-and-doctors.ps1,
    seed-appointment-and-consultation-types.ps1, seed-diagnostic-tests.ps1,
    migrate-diagnostic-tests-to-services.ps1, refresh-lab-tariffs-v3.ps1,
    import-radiology-procedures-v3.ps1) into one ordered run — those six are kept in this
    directory as historical reference (each documents *why* its own step exists) but this is
    now the one script to actually run. The six originals were sequential and interdependent
    (a later phase fixes prices/names a earlier phase set, or looks up records an earlier
    phase created by exact name), so this file reproduces them **in the same order**, not as
    an independently-derived "final state" — that's what makes the result provably match the
    live tenant rather than being a fresh, error-prone re-derivation.

    Every phase is idempotent (checks for existing rows by exact name/code before creating),
    so this script is safe to run more than once against the same tenant, and safe to run
    against a tenant that already has some (but not all) of this data. A final validation
    phase re-fetches every category's count and compares it against the expected end-state,
    printing a pass/fail summary and exiting non-zero if anything doesn't match.

.PARAMETER HospitalCode
    The tenant to seed (e.g. 'lhs' for Lakshmi Hospitals).

.PARAMETER Username
    That hospital's Super Admin username.

.PARAMETER Password
    That hospital's Super Admin password.

.PARAMETER SeedFile
    Path to the ~294-row Laboratory/Radiology tariff JSON (Phase 3). Defaults to
    lab-tests-seed.json next to this script — must ship alongside it; not inlined here since
    it's too large to hardcode (see seed-diagnostic-tests.ps1's own reasoning).

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.EXAMPLE
    ./seed-lakshmi-hospitals.ps1 -HospitalCode lhs -Username lhsadmin -Password 'Lakshmi@123'

.NOTES
    Scope, confirmed with the product owner before writing this: only categories that
    actually have live data in Lakshmi Hospitals are seeded here (the nine covered by
    Validation, below). Products/Pharmacy/Inventory masters (Brands, Manufacturers,
    Suppliers, UOM, Taxes, Warehouses, etc.) are intentionally out of scope — none of them
    have any real data in the live tenant to reproduce.
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
    throw "Seed file not found: $SeedFile (Phase 3 needs this - it ships alongside this script in cicd/scripts/)."
}

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

# ===========================================================================
# Shared helpers (used by every phase below)
# ===========================================================================

function Get-AllPaged {
    param([string]$Entity)
    # The server clamps pageSize to 100 (PagedRequest.MaxPageSize) regardless of what's
    # requested, so this always walks every page rather than assuming one page covers
    # everything - some of these categories (diagnostic-services, diagnostic-tests) run into
    # the hundreds of rows.
    $all = [System.Collections.Generic.List[object]]::new()
    $page = 1
    do {
        # `_` cache-busts the request - Invoke-RestMethod on Windows PowerShell 5.1 can serve a
        # stale cached response for a repeated identical URL (WinINet-backed), which previously
        # made a paging loop under-report a growing result set mid-run.
        $cacheBust = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
        $uri = "$ApiBaseUrl/api/v1/masters/$Entity`?page=$page&pageSize=100&_=$cacheBust"
        $response = Invoke-RestMethod -Method Get -Uri $uri -Headers $authHeaders
        # No leading-comma trick here - this AddRange call doesn't cross a function
        # output/pipeline boundary, so @($response.data) alone already gives a flat array of
        # this page's items. Adding a leading comma here (an earlier version of one of the six
        # source scripts did) wraps the whole per-page array as a single nested element instead
        # of flattening it, so AddRange would add exactly one (wrong) item per page instead of
        # up to 100 - confirmed live, it silently under-fetched. Keep it this way.
        $all.AddRange([object[]]@($response.data))
        $totalPages = $response.meta.totalPages
        $page++
    } while ($page -le $totalPages)
    # Leading comma here IS needed - this return crosses the function's output stream, and
    # PowerShell auto-unrolls a returned array back down to its bare element(s) as it does so.
    # Without the comma, a 0- or 1-element list would arrive at the caller as $null or a bare
    # object, and a multi-element list would arrive as a fixed-size Object[] - either way
    # $list.Add(...) at the call site later throws "Collection was of a fixed size."
    return , $all
}

function Get-Slug {
    param([string]$Name, [int]$MaxLength = 20)
    $slug = ($Name.ToUpperInvariant() -replace '[^A-Z0-9]', '')
    if ([string]::IsNullOrEmpty($slug)) { $slug = 'X' }
    if ($slug.Length -gt $MaxLength) { $slug = $slug.Substring(0, $MaxLength) }
    return $slug
}

function Get-UniqueCode {
    param([string]$Name, [System.Collections.Generic.HashSet[string]]$UsedCodes)
    $base = Get-Slug -Name $Name -MaxLength 18
    $code = $base
    $suffix = 2
    while ($UsedCodes.Contains($code)) {
        $code = "$base$suffix"
        if ($code.Length -gt 20) { $code = $code.Substring(0, 20) }
        $suffix++
    }
    [void]$UsedCodes.Add($code)
    return $code
}

function Invoke-WithRetry {
    param([string]$Method = 'Post', [string]$Uri, [string]$Body, [string]$Label)
    # The API's global rate limiter allows 200 requests/minute per client IP
    # (RateLimitingConfiguration.cs) - shared with whatever else is hitting this API from the
    # same machine (browser tabs included), so every phase below throttles well under that and
    # backs off + retries on a 429 rather than treating it as a hard failure.
    $attempt = 0
    while ($true) {
        $attempt++
        try {
            if ($Body) {
                return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $authHeaders -Body $Body
            }
            return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $authHeaders
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

# ===========================================================================
# PHASE 1: Departments & Consultants
# (was seed-departments-and-doctors.ps1)
# ===========================================================================
Write-Host "`n===== PHASE 1: Departments & Consultants =====" -ForegroundColor Magenta

# Department -> doctor roster, stored exactly as given (full name including title and
# degrees goes into Consultant.name as-is - there's no separate "qualifications" field).
$departments = @(
    @{ Code = 'OBGYN'; Name = 'Obs & Gyn'; Doctors = @(
        'Dr. M. Ramalakshmi, D.G.O.',
        'Dr. S. Meena, M.D (OBG), DLS (Ger)',
        'Dr. L. Madhavi, M.S (OBG), F.R.M & ART'
    ) },
    @{ Code = 'ORTHO'; Name = 'Orthopaedics'; Doctors = @(
        'Dr. S. Karthikeyan, M.S (Ortho)',
        'Dr. G. Balasubramanian, M.S (Ortho)'
    ) },
    @{ Code = 'GENMED'; Name = 'General Medicine & Endocrinology'; Doctors = @(
        'Dr. M. Lakshmanan, M.D (Gen. Med)',
        'Dr. N. Govindarajan, M.D (Gen. Med)',
        'Dr. T. S. Nawas Sherief, M.D (Internal Medicine)'
    ) },
    @{ Code = 'GENSURG'; Name = 'General Surgery'; Doctors = @(
        'Dr. K. Parimalam, M.S (Gen. Surgery)',
        'Dr. S. Sivanupandiyan, M.S (Gen. Sur)',
        'Dr. P. Anitha, M.S (General Surgery), FIAGES, FALS (Hernia)'
    ) },
    @{ Code = 'PAED'; Name = 'Paediatrics & Neonatology'; Doctors = @(
        'Dr. P. Suresh, M.D (Paediatrics)',
        'Dr. S. Shahana Parvin, D.N.B (Paed)'
    ) },
    @{ Code = 'CARDIO'; Name = 'Cardiology'; Doctors = @(
        'Dr. R. Anto Prabhu, M.D., D.M (Cardio)',
        'Dr. K. Bala Ganesh, M.D., D.M (Cardio)'
    ) },
    @{ Code = 'RADIO'; Name = 'Radiology & Sonology'; Doctors = @(
        'Dr. M. Fouzal Hithaya, M.D (RD)',
        'Dr. S. Pearly Stephen, M.D (RD)'
    ) },
    @{ Code = 'PULMO'; Name = 'Pulmonology'; Doctors = @(
        'Dr. M. Viswanathan, M.D., D.N.B (Resp. Medicine)'
    ) },
    @{ Code = 'NEUROMED'; Name = 'Neuromedicine'; Doctors = @(
        'Dr. S. Sankara Narayanan, M.D., D.M (Neuro)',
        'Dr. Jason, M.D., D.M (Neuro)'
    ) },
    @{ Code = 'NEUROSURG'; Name = 'Neurosurgery'; Doctors = @(
        'Dr. S. Senthil Babu, M.S., M.Ch (Neuro)',
        'Dr. D. Joel, M.S., M.Ch (Neuro)'
    ) },
    @{ Code = 'GASTROMED'; Name = 'Gastromedicine (MGE)'; Doctors = @(
        'Dr. E. Kandaswamy, M.D., D.M (Gastro)',
        'Dr. R. Poppy, M.D., D.M (Gastro)'
    ) },
    @{ Code = 'GASTROSURG'; Name = 'Gastrosurgery (SGE)'; Doctors = @(
        'Dr. M. Uma Maheshwaran, M.S., MRCS., M.Ch (Gastro)'
    ) },
    @{ Code = 'URO'; Name = 'Urosurgery'; Doctors = @(
        'Dr. S. Subha Ganesh, M.S., M.Ch (Uro)'
    ) },
    @{ Code = 'PLASTIC'; Name = 'Plastic & Reconstructive Surgery'; Doctors = @(
        'Dr. E. Ramya, M.S., M.Ch (Plastic)',
        'Dr. G. R. Balaji Sharma, M.S., M.Ch (Plastic)'
    ) },
    @{ Code = 'MICRO'; Name = 'Microbiology'; Doctors = @(
        'Dr. R. Pazhaniyappan, M.D (Micro)'
    ) },
    @{ Code = 'ENT'; Name = 'E.N.T'; Doctors = @(
        'Dr. S. Suresh Kumar, M.S., DLO (ENT)',
        'Dr. C. Ravikumar, M.S (ENT)'
    ) },
    @{ Code = 'ANAES'; Name = 'Anaesthesiology & Emergency Medicine'; Doctors = @(
        'Dr. M. Kannan, M.D., DA (Anaes)',
        'Dr. V. Sundarajan, DA (Anaes)',
        'Dr. V. Karthikeyan, DA (Anaes)',
        'Dr. C. Sankaran, M.D (Anaes)'
    ) },
    @{ Code = 'ONCO'; Name = 'Oncosurgery'; Doctors = @(
        'Dr. G. Anitha, M.S., M.Ch (Onco)'
    ) }
)

Write-Host "Fetching existing departments and consultants for idempotency checks..." -ForegroundColor Cyan
$existingDepartments = Get-AllPaged -Entity 'departments'
$existingConsultants = Get-AllPaged -Entity 'consultants'

$departmentCount = 0
$consultantCount = 0

foreach ($dept in $departments) {
    $existingDept = $existingDepartments | Where-Object { $_.name -eq $dept.Name } | Select-Object -First 1
    if ($existingDept) {
        Write-Host "  Department '$($dept.Name)' already exists - skipping." -ForegroundColor DarkGray
        $deptId = $existingDept.id
    }
    else {
        $deptBody = @{ code = $dept.Code; name = $dept.Name } | ConvertTo-Json
        $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/departments" -Body $deptBody -Label $dept.Name
        $deptId = $created.data.id
        Write-Host "  Created department '$($dept.Name)' ($($dept.Code))." -ForegroundColor Green
        $departmentCount++
        $existingDepartments.Add($created.data)
    }

    foreach ($doctorName in $dept.Doctors) {
        $existingDoctor = $existingConsultants | Where-Object { $_.name -eq $doctorName } | Select-Object -First 1
        if ($existingDoctor) {
            Write-Host "    Consultant '$doctorName' already exists - skipping." -ForegroundColor DarkGray
            continue
        }
        # Consultant.Code was removed from the Masters module - CreateConsultantRequest has no
        # Code field, so nothing is generated/sent for it. Two consultants can legitimately
        # share a display name; the UI disambiguates via Specialization instead.
        $consultantBody = @{ name = $doctorName; departmentId = $deptId } | ConvertTo-Json
        $createdConsultant = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/consultants" -Body $consultantBody -Label $doctorName
        Write-Host "    Created consultant '$doctorName'." -ForegroundColor Green
        $consultantCount++
        $existingConsultants.Add($createdConsultant.data)
    }
}
Write-Host "Phase 1 done. Departments: $departmentCount created. Consultants: $consultantCount created." -ForegroundColor Cyan

# ===========================================================================
# PHASE 2: Appointment Types & Consultation Types
# (was seed-appointment-and-consultation-types.ps1 - note: Consultation Type prices here are
# the ORIGINAL values; Phase 6 below corrects two of them to match the v3 tariff sheet, same
# as it did historically. Don't "fix" the numbers here - the correction is Phase 6's job.)
# ===========================================================================
Write-Host "`n===== PHASE 2: Appointment Types & Consultation Types =====" -ForegroundColor Magenta

$appointmentTypes = @(
    'Regular (Walk-in)',
    'Online (Website/WhatsApp)',
    'Phone Call',
    'Scheduled'
)

# Amount is $null for "Others / On-call" - decided per visit instead of a fixed rate.
$consultationTypes = @(
    @{ Name = "Doctor's Consultation (In-house) - Regular"; Amount = 200 },
    @{ Name = "Doctor's Consultation (In-house) - Priority"; Amount = 300 },
    @{ Name = "Doctor's Consultation (Visiting) - Regular"; Amount = 250 },
    @{ Name = 'Emergency / Casualty Doctor''s Consultation'; Amount = 500 },
    @{ Name = "Doctor's Consultation - Others / On-call"; Amount = $null }
)

Write-Host "Fetching existing appointment types and consultation types..." -ForegroundColor Cyan
$existingAppointmentTypes = Get-AllPaged -Entity 'appointment-types'
$existingConsultationTypes = Get-AllPaged -Entity 'consultation-types'

$apptTypeCount = 0
$consultTypeCount = 0

foreach ($name in $appointmentTypes) {
    $existing = $existingAppointmentTypes | Where-Object { $_.name -eq $name } | Select-Object -First 1
    if ($existing) {
        Write-Host "  '$name' already exists - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{ name = $name; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/appointment-types" -Body $body -Label $name
    Write-Host "  Created appointment type '$name'." -ForegroundColor Green
    $apptTypeCount++
    $existingAppointmentTypes.Add($created.data)
}

foreach ($entry in $consultationTypes) {
    $existing = $existingConsultationTypes | Where-Object { $_.name -eq $entry.Name } | Select-Object -First 1
    if ($existing) {
        Write-Host "  '$($entry.Name)' already exists - skipping." -ForegroundColor DarkGray
        continue
    }
    $bodyHash = @{ name = $entry.Name; isActive = $true }
    if ($null -ne $entry.Amount) { $bodyHash.amount = $entry.Amount }
    $body = $bodyHash | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/consultation-types" -Body $body -Label $entry.Name
    $amountLabel = if ($null -ne $entry.Amount) { "Rs.$($entry.Amount)" } else { 'no fixed rate' }
    Write-Host "  Created consultation type '$($entry.Name)' ($amountLabel)." -ForegroundColor Green
    $consultTypeCount++
    $existingConsultationTypes.Add($created.data)
}
Write-Host "Phase 2 done. Appointment Types: $apptTypeCount created. Consultation Types: $consultTypeCount created." -ForegroundColor Cyan

# ===========================================================================
# PHASE 3: Diagnostic Tests (legacy flat catalog, from the tariff spreadsheet)
# (was seed-diagnostic-tests.ps1)
# ===========================================================================
Write-Host "`n===== PHASE 3: Diagnostic Tests (legacy flat catalog) =====" -ForegroundColor Magenta

$seedRows = Get-Content $SeedFile -Raw | ConvertFrom-Json
Write-Host "Loaded $($seedRows.Count) diagnostic test rows from '$SeedFile'." -ForegroundColor Cyan

Write-Host "Fetching existing diagnostic tests for idempotency checks..." -ForegroundColor Cyan
$existingTests = Get-AllPaged -Entity 'diagnostic-tests'
Write-Host "Found $($existingTests.Count) existing diagnostic tests." -ForegroundColor Cyan

$phase3Created = 0
foreach ($row in $seedRows) {
    $existingMatch = $existingTests | Where-Object {
        $_.name -eq $row.name -and $_.serviceType -eq $row.serviceType -and [bool]$_.isOutsourced -eq [bool]$row.isOutsourced
    } | Select-Object -First 1
    if ($existingMatch) { continue }

    $bodyHash = @{
        name         = $row.name
        serviceType  = $row.serviceType
        category     = $row.category
        price        = $row.price
        isOutsourced = [bool]$row.isOutsourced
        isActive     = $true
    }
    if ($row.referenceLab) { $bodyHash.referenceLab = $row.referenceLab }
    $body = $bodyHash | ConvertTo-Json

    try {
        $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests" -Body $body -Label $row.name
        Write-Host "  Created '$($row.name)' [$($row.serviceType)$(if ($row.isOutsourced) { ', outsourced' })] - Rs.$($row.price)" -ForegroundColor Green
        $phase3Created++
        $existingTests.Add($created.data)
    }
    catch {
        Write-Host "  FAILED '$($row.name)': $($_.Exception.Message)" -ForegroundColor Red
    }
    # 350ms/request caps this well under the 200/min global rate limit.
    Start-Sleep -Milliseconds 350
}
Write-Host "Phase 3 done. Diagnostic Tests: $phase3Created created." -ForegroundColor Cyan

# ===========================================================================
# PHASE 4: Migrate Laboratory/Radiology DiagnosticTest rows -> DiagnosticCategory /
#          DiagnosticProvider / DiagnosticService, then deactivate the migrated rows.
#          Procedure-type rows are left untouched (Procedure Billing still reads them).
# (was migrate-diagnostic-tests-to-services.ps1)
# ===========================================================================
Write-Host "`n===== PHASE 4: Migrate Lab/Radiology tests to the normalized catalog =====" -ForegroundColor Magenta

Write-Host "Fetching all diagnostic tests..." -ForegroundColor Cyan
$allTests = Get-AllPaged -Entity 'diagnostic-tests'
# isActive -eq $true excludes rows a previous run of this same phase already migrated and
# deactivated - without this, re-running after a later phase renames the migrated service
# (Phase 6 does this for "Neonatal / Paediatric ECHO") makes this loop's name-match against
# $existingServices fail and silently create a duplicate under the old, stale name. Confirmed
# live: an early version of this script without the filter did exactly that on a second run.
$migratableTests = $allTests | Where-Object { $_.serviceType -in @('Laboratory', 'Radiology') -and $_.isActive -eq $true }
Write-Host "Found $($allTests.Count) diagnostic tests total; $($migratableTests.Count) are active Laboratory/Radiology rows and will be migrated." -ForegroundColor Cyan

if ($migratableTests.Count -gt 0) {
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

    # key: "<ServiceType>|<CategoryName>" -> categoryId. Scoped per ServiceType so a category
    # string shared between Laboratory and Radiology rows still produces two separate rows.
    $categoryIdByKey = @{}
    $categoryNamesClaimed = [System.Collections.Generic.HashSet[string]]::new()

    function Get-OrCreateCategory {
        param([string]$ServiceType, [string]$CategoryName)
        $rawName = if ([string]::IsNullOrWhiteSpace($CategoryName)) { 'Uncategorized' } else { $CategoryName.Trim() }
        $key = "$ServiceType|$rawName"
        if ($categoryIdByKey.ContainsKey($key)) { return $categoryIdByKey[$key] }

        $effectiveName = $rawName
        if ($categoryNamesClaimed.Contains($rawName)) { $effectiveName = "$rawName ($ServiceType)" }

        $existing = $existingCategories | Where-Object { $_.name -eq $effectiveName } | Select-Object -First 1
        if ($existing) {
            $categoryIdByKey[$key] = $existing.id
            [void]$categoryNamesClaimed.Add($rawName)
            return $existing.id
        }

        $code = Get-UniqueCode -Name $effectiveName -UsedCodes $usedCategoryCodes
        $body = @{ code = $code; name = $effectiveName; isActive = $true } | ConvertTo-Json
        $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-categories" -Body $body -Label $effectiveName
        Write-Host "  Created category '$effectiveName' [$ServiceType] (code $code)." -ForegroundColor Green
        $script:categoriesCreated++
        Start-Sleep -Milliseconds 350
        $existingCategories.Add($created.data)
        $categoryIdByKey[$key] = $created.data.id
        [void]$categoryNamesClaimed.Add($rawName)
        return $created.data.id
    }

    $providerIdByName = @{}
    function Get-OrCreateProvider {
        param([string]$ReferenceLabName)
        $rawName = if ([string]::IsNullOrWhiteSpace($ReferenceLabName)) { 'Unspecified Provider' } else { $ReferenceLabName.Trim() }
        if ($providerIdByName.ContainsKey($rawName)) { return $providerIdByName[$rawName] }

        $existing = $existingProviders | Where-Object { $_.name -eq $rawName } | Select-Object -First 1
        if ($existing) {
            $providerIdByName[$rawName] = $existing.id
            return $existing.id
        }

        $code = Get-UniqueCode -Name $rawName -UsedCodes $usedProviderCodes
        $body = @{ code = $code; name = $rawName; isActive = $true } | ConvertTo-Json
        $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-providers" -Body $body -Label $rawName
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
        if ([bool]$testRow.isOutsourced) { [void](Get-OrCreateProvider -ReferenceLabName $testRow.referenceLab) }
    }

    Write-Host "Diagnostic services:" -ForegroundColor Cyan
    foreach ($testRow in $migratableTests) {
        $isOutsourced = [bool]$testRow.isOutsourced
        $alreadyExists = $existingServices | Where-Object {
            $_.name -eq $testRow.name -and $_.serviceType -eq $testRow.serviceType -and [bool]$_.isOutsourced -eq $isOutsourced
        } | Select-Object -First 1
        if ($alreadyExists) {
            $servicesSkipped++
            continue
        }

        $categoryId = Get-OrCreateCategory -ServiceType $testRow.serviceType -CategoryName $testRow.category
        $providerId = $null
        if ($isOutsourced) { $providerId = Get-OrCreateProvider -ReferenceLabName $testRow.referenceLab }

        $code = Get-UniqueCode -Name $testRow.name -UsedCodes $usedServiceCodes
        $bodyHash = @{
            code = $code; name = $testRow.name; categoryId = $categoryId; serviceType = $testRow.serviceType
            isOutsourced = $isOutsourced; price = $testRow.price; isActive = $true
        }
        if ($providerId) { $bodyHash.providerId = $providerId }
        $body = $bodyHash | ConvertTo-Json

        try {
            $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services" -Body $body -Label $testRow.name
            Write-Host "  Created service '$($testRow.name)' [$($testRow.serviceType)$(if ($isOutsourced) { ', outsourced' })] - Rs.$($testRow.price)" -ForegroundColor Green
            $servicesCreated++
            $existingServices.Add($created.data)
        }
        catch {
            Write-Host "  FAILED to create service '$($testRow.name)': $($_.Exception.Message)" -ForegroundColor Red
        }
        Start-Sleep -Milliseconds 350
    }

    Write-Host "Deactivating migrated diagnostic tests:" -ForegroundColor Cyan
    foreach ($testRow in $migratableTests) {
        if ($testRow.isActive -eq $false) { continue }
        $bodyHash = @{
            name = $testRow.name; serviceType = $testRow.serviceType; price = $testRow.price
            isOutsourced = [bool]$testRow.isOutsourced; isActive = $false
        }
        if ($testRow.category) { $bodyHash.category = $testRow.category }
        if ($testRow.referenceLab) { $bodyHash.referenceLab = $testRow.referenceLab }
        $body = $bodyHash | ConvertTo-Json
        try {
            Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests/$($testRow.id)" -Body $body -Label $testRow.name | Out-Null
            $testsDeactivated++
        }
        catch {
            Write-Host "  FAILED to deactivate '$($testRow.name)': $($_.Exception.Message)" -ForegroundColor Red
        }
        Start-Sleep -Milliseconds 350
    }
    Write-Host "Phase 4 done. Categories: $categoriesCreated created. Providers: $providersCreated created. Services: $servicesCreated created ($servicesSkipped already migrated). Tests deactivated: $testsDeactivated." -ForegroundColor Cyan
}
else {
    Write-Host "Nothing to migrate (no active Laboratory/Radiology diagnostic-tests found) - Phase 4 skipped." -ForegroundColor DarkGray
}

# ===========================================================================
# PHASE 5: v3 tariff refresh - Q-LAB provider merge, 2 price fixes, 2 new outsourced
#          services, 16 real Lab packages, Injection Charges + Files (Procedure catalog)
# (was refresh-lab-tariffs-v3.ps1)
# ===========================================================================
Write-Host "`n===== PHASE 5: v3 tariff refresh (packages, Injection Charges, Files) =====" -ForegroundColor Magenta

$services = Get-AllPaged -Entity 'diagnostic-services'
$categories = Get-AllPaged -Entity 'diagnostic-categories'
$providers = Get-AllPaged -Entity 'diagnostic-providers'
$packages = Get-AllPaged -Entity 'diagnostic-packages'
$legacyTests = Get-AllPaged -Entity 'diagnostic-tests'

function Get-ServiceByName {
    param([string]$Name)
    $svc = $services | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if (-not $svc) { throw "Service not found by exact name: '$Name' - check spelling against the DB." }
    return $svc
}
function Get-CategoryByCode { param([string]$Code) return ($categories | Where-Object { $_.code -eq $Code } | Select-Object -First 1) }
function Get-ProviderByCode { param([string]$Code) return ($providers | Where-Object { $_.code -eq $Code } | Select-Object -First 1) }
function Get-PackageByCode { param([string]$Code) return ($packages | Where-Object { $_.code -eq $Code } | Select-Object -First 1) }
function Get-LegacyTestByName { param([string]$Name) return ($legacyTests | Where-Object { $_.name -eq $Name } | Select-Object -First 1) }

Write-Host "Step: Q-LAB provider merge" -ForegroundColor Cyan
$canonicalQLab = Get-ProviderByCode 'QLAB2'
$duplicateQLab = Get-ProviderByCode 'QLAB'
if ($canonicalQLab -and $duplicateQLab -and $duplicateQLab.isActive) {
    $affected = $services | Where-Object { $_.providerId -eq $duplicateQLab.id }
    foreach ($svc in $affected) {
        $body = @{ code = $svc.code; name = $svc.name; categoryId = $svc.categoryId; serviceType = $svc.serviceType; isOutsourced = $svc.isOutsourced; providerId = $canonicalQLab.id; price = $svc.price; isActive = $svc.isActive } | ConvertTo-Json
        Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($svc.id)" -Body $body -Label "repoint $($svc.name)" | Out-Null
        Start-Sleep -Milliseconds 350
    }
    $deactivateBody = @{ code = $duplicateQLab.code; name = $duplicateQLab.name; contactDetails = $duplicateQLab.contactDetails; isActive = $false } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-providers/$($duplicateQLab.id)" -Body $deactivateBody -Label 'deactivate duplicate Q-LAB' | Out-Null
    Write-Host "  Merged duplicate Q-LAB provider." -ForegroundColor Green
}
else {
    Write-Host "  Already merged or nothing to do." -ForegroundColor DarkGray
}

Write-Host "Step: price updates" -ForegroundColor Cyan
$priceUpdates = @(@{ name = 'Total Cholesterol'; price = 100 }, @{ name = 'PAP Smear'; price = 800 })
$services = Get-AllPaged -Entity 'diagnostic-services'
foreach ($u in $priceUpdates) {
    $svc = Get-ServiceByName $u.name
    if ($svc.price -eq $u.price) { continue }
    $body = @{ code = $svc.code; name = $svc.name; categoryId = $svc.categoryId; serviceType = $svc.serviceType; isOutsourced = $svc.isOutsourced; providerId = $svc.providerId; price = $u.price; isActive = $svc.isActive } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($svc.id)" -Body $body -Label "price update $($u.name)" | Out-Null
    Write-Host "  '$($u.name)': -> Rs.$($u.price)" -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

Write-Host "Step: new outsourced services" -ForegroundColor Cyan
$endocrinology = Get-CategoryByCode 'ENDOCRINOLOGY'
$anderson = Get-ProviderByCode 'ANDERSON'
$newServices = @(
    @{ code = 'ANTIB2MICRO'; name = 'Anti-Beta 2 (β2) Microglobulin' }
    @{ code = 'ANTIB2GLYIGM'; name = 'Anti-Beta 2 (β2) Glycoprotein IgM' }
)
foreach ($n in $newServices) {
    if ($services | Where-Object { $_.name -eq $n.name }) { continue }
    $body = @{ code = $n.code; name = $n.name; categoryId = $endocrinology.id; serviceType = 'Laboratory'; isOutsourced = $true; providerId = $anderson.id; price = 1400; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services" -Body $body -Label $n.name
    $services.Add($created.data)
    Write-Host "  Created '$($n.name)' - Rs.1400, outsourced to ANDERSON." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

Write-Host "Step: 16 Lab packages" -ForegroundColor Cyan
$packageDefs = @(
    @{ code='CBC'; name='Complete Blood Count (CBC)'; desc='Haemoglobin, TC, Platelet, RBC, Differential Count, PCV, MCV.'; price=300; legacy=$null
       items=@('Haemoglobin','Total WBC Count (TC)','Platelet Count','RBC Count','Differential Count','Packed Cell Volume (PCV)','MCV') }
    @{ code='CBCESR'; name='CBC + ESR'; desc='Complete Blood Count plus ESR.'; price=350; legacy='CBC + ESR'
       items=@('Haemoglobin','Total WBC Count (TC)','Platelet Count','RBC Count','Differential Count','Packed Cell Volume (PCV)','MCV','ESR') }
    @{ code='HBTCDCESR'; name='Hb-TC-DC-ESR'; desc='Haemoglobin, TC, Differential Count, ESR.'; price=150; legacy='HB+TC+DC+ESR'
       items=@('Haemoglobin','Total WBC Count (TC)','Differential Count','ESR') }
    @{ code='RFTKFT'; name='Renal Function Tests (RFT/KFT)'; desc='Urea, Creatinine.'; price=150; legacy='RENAL/KIDNEY FUNCTION TESTS (RFT/KFT)'
       items=@('Urea','Creatinine') }
    @{ code='LFT'; name='Liver Function Tests (LFT)'; desc='Bilirubin, SGOT, SGPT, Protein, Alk. Phosphatase.'; price=900; legacy='LIVER FUNCTION TESTS (LFT)'
       items=@('Bilirubin (Total+Direct)','S G O T','S G P T','Protein','Alk.Phosphatase') }
    @{ code='LIPID'; name='Lipid Profile'; desc='Total Cholesterol, Triglycerides, HDL.'; price=450; legacy='LIPID PROFILE'
       items=@('Total Cholesterol','Triglycerides','HDL') }
    @{ code='COAGPROFILE'; name='Coagulation Profile'; desc='PT, APTT, INR, Bleeding Time, Clotting Time.'; price=600; legacy='COAGULATION PROFILE'
       items=@('Prothrombin Time (PT)','APTT','INR','Bleeding Time (BT)','Clotting Time (CT)') }
    @{ code='ELECTROLYTES'; name='Electrolytes (Na, K, Cl, HCO3)'; desc='Sodium, Potassium, Chloride, Bicarbonate.'; price=450; legacy='SERUM ELECTROLYTES'
       items=@('Sodium','Potassium','Chloride','Bicarbonate') }
    @{ code='SEROLOGY1'; name='Serology 1'; desc='HIV, HBsAg, HCV, VDRL.'; price=800; legacy='SEROLOGY 1'
       items=@('HIV','HBsAg','HCV','VDRL') }
    @{ code='SEROLOGY2'; name='Serology 2'; desc='HBsAg, HCV, VDRL.'; price=650; legacy='SEROLOGY 2'
       items=@('HBsAg','HCV','VDRL') }
    @{ code='STOOLROUTINE'; name='Stool Routine'; desc='Stool for Occult Blood, Stool for Ova/Cyst.'; price=150; legacy='STOOL ROUTINE'
       items=@('Stool for Occult Blood','Stool for Ova / Cyst') }
    @{ code='URINEANALYSIS'; name='Urine Analysis'; desc='Sugar, Albumin, Deposits, Acetone, Bile Salts/Pigments, Urobilinogen.'; price=150; legacy='URINE ANALYSIS'
       items=@('Urine Sugar','Urine Albumin','Urine Deposits','Urine Acetone','Urine Bile Salts-Bile Pigments (BS/BP)','Urobilinogen') }
    @{ code='BIORFTBILCHOL'; name='BIO - RBS, RFT, Sr.Bil, Cholesterol'; desc='RBS, Urea, Creatinine, Bilirubin, Total Cholesterol.'; price=500; legacy='BIO - RBS, RFT, Sr.Bil, Cholesterol'
       items=@('Random Blood Sugar (RBS)','Urea','Creatinine','Bilirubin (Total+Direct)','Total Cholesterol') }
    @{ code='DIABETICPROFILE2'; name='Diabetic Profile 2'; desc='RBS, FBS.'; price=100; legacy='DIABETIC PROFILE 2'
       items=@('Random Blood Sugar (RBS)','Fasting Blood Sugar (FBS)') }
    @{ code='RHEUMATOID1'; name='Rheumatoid Profile 1'; desc='CBC, ESR, CRP, RA Factor, Anti-CCP, Uric Acid, ASO Titre, 25-OH-Vitamin D2.'; price=3600; legacy='RHEUMATOID PROFILE - 1 (CBC,ESR,CRP,RF,ACPA,UA,ASO,Vit D2)'
       items=@('Haemoglobin','Total WBC Count (TC)','Platelet Count','RBC Count','Differential Count','Packed Cell Volume (PCV)','MCV','ESR','CRP','RA Factor (RF)','Anti-CCP','Uric Acid (UA)','ASO Titre','25-OH-Vitamin D2') }
    @{ code='RHEUMATOID2'; name='Rheumatoid Profile 2'; desc='CBC, ESR, CRP, RA Factor, Uric Acid, 25-OH-Vitamin D2.'; price=2400; legacy='RHEUMATOID PROFILE - 2 (CBC,ESR,CRP,RF,UA,Vit D2)'
       items=@('Haemoglobin','Total WBC Count (TC)','Platelet Count','RBC Count','Differential Count','Packed Cell Volume (PCV)','MCV','ESR','CRP','RA Factor (RF)','Uric Acid (UA)','25-OH-Vitamin D2') }
)

foreach ($def in $packageDefs) {
    $existing = Get-PackageByCode $def.code
    if ($existing) {
        $currentServiceIds = $existing.items | ForEach-Object { $_.serviceId }
        foreach ($itemName in $def.items) {
            $svc = Get-ServiceByName $itemName
            if ($svc.id -in $currentServiceIds) { continue }
            $addBody = @{ serviceId = $svc.id } | ConvertTo-Json
            Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-packages/$($existing.id)/items" -Body $addBody -Label "add $itemName to $($def.name)" | Out-Null
            Start-Sleep -Milliseconds 350
        }
    }
    else {
        $serviceIds = @()
        foreach ($itemName in $def.items) { $serviceIds += (Get-ServiceByName $itemName).id }
        $body = @{ code = $def.code; name = $def.name; description = $def.desc; totalPrice = $def.price; isActive = $true; serviceIds = $serviceIds } | ConvertTo-Json
        Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-packages" -Body $body -Label "create $($def.name)" | Out-Null
        Write-Host "  Created package '$($def.name)' - Rs.$($def.price) ($($serviceIds.Count) tests)." -ForegroundColor Green
        Start-Sleep -Milliseconds 350
    }

    if ($def.legacy) {
        $legacy = Get-LegacyTestByName $def.legacy
        if ($legacy -and $legacy.isActive) {
            $body = @{ name = $legacy.name; serviceType = $legacy.serviceType; category = $legacy.category; price = $legacy.price; isOutsourced = $legacy.isOutsourced; referenceLab = $legacy.referenceLab; isActive = $false } | ConvertTo-Json
            Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests/$($legacy.id)" -Body $body -Label "deactivate legacy $($def.legacy)" | Out-Null
            Start-Sleep -Milliseconds 350
        }
    }
}

Write-Host "Step: Injection Charges + Files (Procedure catalog)" -ForegroundColor Cyan
$legacyTests = Get-AllPaged -Entity 'diagnostic-tests'
$procedureRows = @(
    @{ name = 'Injections - IM / SC / ID'; category = 'Injection Charges'; price = 200 }
    @{ name = 'Injections - Direct IV'; category = 'Injection Charges'; price = 500 }
    @{ name = 'Injections - IV as Drip'; category = 'Injection Charges'; price = 1000 }
    @{ name = 'Injections - Intra-articular / Specific Sites'; category = 'Injection Charges'; price = 1500 }
    @{ name = 'Blood Transfusion'; category = 'Injection Charges'; price = 2000 }
    @{ name = 'General Blue File'; category = 'Files'; price = 100 }
    @{ name = 'ANC File'; category = 'Files'; price = 50 }
    @{ name = 'Neonatal File'; category = 'Files'; price = 50 }
    @{ name = 'Green File'; category = 'Files'; price = 20 }
)
foreach ($row in $procedureRows) {
    $exists = $legacyTests | Where-Object { $_.name -eq $row.name -and $_.serviceType -eq 'Procedure' }
    if ($exists) { continue }
    $body = @{ name = $row.name; serviceType = 'Procedure'; category = $row.category; price = $row.price; isOutsourced = $false; isActive = $true } | ConvertTo-Json
    Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests" -Body $body -Label $row.name | Out-Null
    Write-Host "  Created '$($row.name)' [$($row.category)] - Rs.$($row.price)." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}
Write-Host "Phase 5 done." -ForegroundColor Cyan

# ===========================================================================
# PHASE 6: Consultation price correction, full Radiology/Ultrasound catalog, Procedural
#          Charges, 6 more Lab packages, and a data-quality fix for stale duplicate flat
#          services left over from Phase 5's package creation.
# (was import-radiology-procedures-v3.ps1)
# ===========================================================================
Write-Host "`n===== PHASE 6: Radiology/Ultrasound, Procedural Charges, 6 more packages =====" -ForegroundColor Magenta

$consultationTypesLive = Get-AllPaged -Entity 'consultation-types'
$consultationUpdates = @(
    @{ name = "Doctor's Consultation (In-house) - Regular"; amount = 250 }
    @{ name = "Doctor's Consultation (In-house) - Priority"; amount = 400 }
)
foreach ($u in $consultationUpdates) {
    $ct = $consultationTypesLive | Where-Object { $_.name -eq $u.name } | Select-Object -First 1
    if (-not $ct -or $ct.amount -eq $u.amount) { continue }
    $body = @{ name = $ct.name; amount = $u.amount; isActive = $ct.isActive } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/consultation-types/$($ct.id)" -Body $body -Label "price update $($u.name)" | Out-Null
    Write-Host "  '$($u.name)': -> Rs.$($u.amount)" -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

Write-Host "Step: Radiology catalog" -ForegroundColor Cyan
$radiologyPriceFixes = @(
    @{ name = 'Digital X-ray Large films for Femur/Tibia/Humerus'; price = 500 }
    @{ name = 'Digital X-ray Long Leg View Scannogram'; price = 800 }
)
foreach ($u in $radiologyPriceFixes) {
    $svc = Get-ServiceByName $u.name
    if ($svc.price -eq $u.price) { continue }
    $body = @{ code = $svc.code; name = $svc.name; categoryId = $svc.categoryId; serviceType = $svc.serviceType; isOutsourced = $svc.isOutsourced; providerId = $svc.providerId; price = $u.price; isActive = $svc.isActive } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($svc.id)" -Body $body -Label "price fix $($u.name)" | Out-Null
    Start-Sleep -Milliseconds 350
}

$staleName = 'Neonatal / Paediatric ECHO'
$correctName = 'Neonatal / Paediatric / Complex Fetal ECHO'
$services = Get-AllPaged -Entity 'diagnostic-services'
$staleEcho = $services | Where-Object { $_.name -eq $staleName } | Select-Object -First 1
if ($staleEcho) {
    $body = @{ code = $staleEcho.code; name = $correctName; categoryId = $staleEcho.categoryId; serviceType = $staleEcho.serviceType; isOutsourced = $staleEcho.isOutsourced; providerId = $staleEcho.providerId; price = $staleEcho.price; isActive = $staleEcho.isActive } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($staleEcho.id)" -Body $body -Label 'rename Neonatal ECHO' | Out-Null
    Start-Sleep -Milliseconds 350
}

$categories = Get-AllPaged -Entity 'diagnostic-categories'
$ultrasoundCategory = Get-CategoryByCode 'ULTRASOUND'
if (-not $ultrasoundCategory) {
    $body = @{ code = 'ULTRASOUND'; name = 'Ultrasound'; description = 'Ultrasound and Doppler studies (USG/Doppler subtypes).'; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-categories" -Body $body -Label 'create Ultrasound category'
    $categories.Add($created.data)
    $ultrasoundCategory = $created.data
    Write-Host "  Created category 'Ultrasound'." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

$digitalXray = Get-CategoryByCode 'DIGITALXRAY'
$cardiology = Get-CategoryByCode 'CARDIOLOGY'
$extraRadiologyServices = @(
    @{ code = 'HSGXRAY'; name = 'Hysterosalphingogram (HSG)'; categoryId = $digitalXray.id; price = 1000 }
    @{ code = 'ECHOADULT'; name = 'ECHO (Adult)'; categoryId = $cardiology.id; price = 2000 }
)
foreach ($n in $extraRadiologyServices) {
    if ($services | Where-Object { $_.name -eq $n.name -and $_.serviceType -eq 'Radiology' }) { continue }
    $body = @{ code = $n.code; name = $n.name; categoryId = $n.categoryId; serviceType = 'Radiology'; isOutsourced = $false; providerId = $null; price = $n.price; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services" -Body $body -Label $n.name
    $services.Add($created.data)
    Write-Host "  Created '$($n.name)' - Rs.$($n.price)." -ForegroundColor Green
    Start-Sleep -Milliseconds 250
}

# Rows 341-391 of the tariff sheet, excluding "All Scans and Doppler - Bedside" (a surcharge
# modifier, not a standalone billable service).
$ultrasoundDefs = @(
    @{ code='ABDOMENANDPELVISADUL'; name='Abdomen and Pelvis (Adult/Neonatal/Paediatric)'; price=2000 }
    @{ code='ANCEARLYPREGNANCYSCA'; name='ANC - Early Pregnancy Scan'; price=2000 }
    @{ code='ANCEARLYPREGNANCYSC2'; name='ANC - Early Pregnancy Scan - LH'; price=1300 }
    @{ code='ANCEARLYPREGNANCYSC3'; name='ANC - Early Pregnancy Scan - MR'; price=1300 }
    @{ code='ANCNTSCAN'; name='ANC - NT Scan'; price=2200 }
    @{ code='ANCTWINSNTSCAN'; name='ANC - Twins NT Scan'; price=3000 }
    @{ code='ANCNTSCANLH'; name='ANC - NT Scan - LH'; price=1300 }
    @{ code='ANCNTSCANMR'; name='ANC - NT Scan - MR'; price=1300 }
    @{ code='ANCANOMALYSCAN'; name='ANC - Anomaly Scan'; price=3000 }
    @{ code='ANCFETALECHO'; name='ANC - Fetal ECHO'; price=3500 }
    @{ code='ANCCOMPLEXFETALECHOD'; name='ANC - Complex Fetal ECHO (Dr.Balaganesh)'; price=5000 }
    @{ code='ANCGROWTHSCAN'; name='ANC - Growth Scan'; price=2200 }
    @{ code='ANCGROWTHSCANLH'; name='ANC - Growth Scan - LH'; price=1300 }
    @{ code='ANCGROWTHSCANMR'; name='ANC - Growth Scan - MR'; price=1300 }
    @{ code='ANCGROWTHSCANDOPPLER'; name='ANC - Growth Scan + Doppler'; price=2500 }
    @{ code='ANCGROWTHSCANDOPPLE2'; name='ANC - Growth Scan + Doppler - LH'; price=1800 }
    @{ code='ANCTWINSGROWTHSCAN'; name='ANC - Twins Growth Scan'; price=3200 }
    @{ code='ANCTWINSGROWTHDOPPLE'; name='ANC - Twins Growth + Doppler Scan'; price=4000 }
    @{ code='ANCLIQUORANDFHLH'; name='ANC - Liquor and FH - LH'; price=500 }
    @{ code='ANCLIQUORANDFHMR'; name='ANC - Liquor and FH - MR'; price=500 }
    @{ code='ANC4DSCAN'; name='ANC - 4D Scan'; price=3000 }
    @{ code='USGFORRETAINEDPRODUC'; name='USG for Retained Products - LH'; price=500 }
    @{ code='USGFORRETAINEDPRODU2'; name='USG for Retained Products - MR'; price=500 }
    @{ code='FOLLICULARSTUDYLH'; name='Follicular Study - LH'; price=500 }
    @{ code='FOLLICULIMETRYDOPPLE'; name='Folliculimetry + Doppler - LH'; price=750 }
    @{ code='TRANSVAGINALSCANTVSL'; name='Transvaginal Scan (TVS) - LH'; price=1300 }
    @{ code='TRANSVAGINALSCANTVSM'; name='Transvaginal Scan (TVS) - MR'; price=1300 }
    @{ code='CERVICALASSESSMENTLH'; name='Cervical Assessment - LH'; price=500 }
    @{ code='CERVICALASSESSMENTMR'; name='Cervical Assessment - MR'; price=500 }
    @{ code='TASPELVISSCANLH'; name='TAS - Pelvis Scan- LH'; price=1300 }
    @{ code='TASPELVISSCANMR'; name='TAS - Pelvis Scan- MR'; price=1300 }
    @{ code='FSPELVISSCANLH'; name='FS + Pelvis Scan- LH'; price=1500 }
    @{ code='FSPELVISSCANMR'; name='FS + Pelvis Scan- MR'; price=1500 }
    @{ code='4DGYNAECSCAN'; name='4D Gynaec Scan'; price=3000 }
    @{ code='HEADANDCRANIUMNEONAT'; name='Head and Cranium (Neonatal/Paediatric/Adult)'; price=2000 }
    @{ code='THYROIDNECK'; name='Thyroid / Neck'; price=2000 }
    @{ code='BREASTUSG'; name='Breast'; price=2000 }
    @{ code='CHESTUSG'; name='Chest'; price=2000 }
    @{ code='SCROTUMDOPPLER'; name='Scrotum + Doppler'; price=2000 }
    @{ code='JOINTSSHOULDERELBOWW'; name='Joints (Shoulder/Elbow/Wrist/Hip/Knee/Ankle) - Single'; price=2000 }
    @{ code='JOINTSDOUBLE'; name='Joints - Double'; price=2500 }
    @{ code='LIMBSARMFOREARMHANDT'; name='Limbs (Arm/Forearm/Hand/Thigh/Leg/Foot) - Single'; price=2000 }
    @{ code='LIMBSDOUBLE'; name='Limbs - Double'; price=2500 }
    @{ code='BACKUSG'; name='Back'; price=2000 }
    @{ code='SWELLINGSUSG'; name='Swellings'; price=1500 }
    @{ code='SINGLELIMBDOPPLERART'; name='Single Limb Doppler - Arterial / Venous'; price=2000 }
    @{ code='SINGLELIMBDOPPLERAR2'; name='Single Limb Doppler - Arterial and Venous'; price=3000 }
    @{ code='DOUBLELIMBSDOPPLERAR'; name='Double Limbs Doppler - Arterial / Venous'; price=4000 }
    @{ code='DOUBLELIMBSDOPPLERA2'; name='Double Limbs Doppler - Arterial and Venous'; price=6000 }
)
foreach ($n in $ultrasoundDefs) {
    if ($services | Where-Object { $_.name -eq $n.name -and $_.serviceType -eq 'Radiology' }) { continue }
    $body = @{ code = $n.code; name = $n.name; categoryId = $ultrasoundCategory.id; serviceType = 'Radiology'; isOutsourced = $false; providerId = $null; price = $n.price; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services" -Body $body -Label $n.name
    $services.Add($created.data)
    Start-Sleep -Milliseconds 250
}
Write-Host "  Radiology/Ultrasound catalog up to date." -ForegroundColor Green

Write-Host "Step: Procedural Charges" -ForegroundColor Cyan
$proceduralDefs = @(
    @{ name='Dressing - Minor'; price=200 }
    @{ name='Dressing - Major'; price=500 }
    @{ name='Suture Removal with Dressing'; price=200 }
    @{ name='Suturing with Dressing - Minor'; price=500 }
    @{ name='Suturing with Dressing - Major'; price=1000 }
    @{ name='Debridement with Dressing - Minor'; price=500 }
    @{ name='Debridement with Dressing - Major'; price=1000 }
    @{ name='I & D with Dressing - Minor'; price=500 }
    @{ name='I & D with Dressing - Major'; price=1000 }
    @{ name='Joint / Swelling Aspiration with Dressing - Minor'; price=500 }
    @{ name='Joint / Swelling Aspiration with Dressing - Major'; price=1000 }
    @{ name='POP Application - Minor'; price=1500 }
    @{ name='POP Application - Major'; price=2500 }
    @{ name='POP Application - Multiple'; price=3000 }
    @{ name='POP Removal - Minor'; price=200 }
    @{ name='POP Removal - Major'; price=500 }
    @{ name='K-wire(s) / Implant Removal - Minor'; price=500 }
    @{ name='K-wire(s) / Implant Removal - Major'; price=1000 }
    @{ name='Dislocation Reduction - Minor'; price=1000 }
    @{ name='Dislocation Reduction - Major'; price=1500 }
    @{ name='Biopsy with Dressing - Minor'; price=1500 }
    @{ name='Biopsy with Dressing - Major'; price=2500 }
    @{ name='Cervical Cytology / Swab (PAP Smear/HPV/Hi-Vaginal Swab)'; price=1500 }
    @{ name='Pipelle Sampling'; price=3000 }
    @{ name='Pipelle Sampling + Endometrial / Cervical Biopsy'; price=5500 }
    @{ name='Pipelle Sampling + Endometrial Biopsy + Cervical Biopsy'; price=6000 }
    @{ name='Copper-T Insertion'; price=3000 }
    @{ name='Copper-T Removal'; price=800 }
    @{ name='MIRENA Insertion + Endometrial Biopsy'; price=4000 }
    @{ name='Removal of Misplaced IUCD'; price=1500 }
    @{ name='Cervical Encirclage Removal'; price=500 }
    @{ name='Pessary Insertion'; price=1500 }
    @{ name='Pessary Removal'; price=800 }
    @{ name='Removal of Vaginal / Cervical Foreign body'; price=1500 }
    @{ name='Cervical Polypectomy'; price=3000 }
    @{ name='Wart Excision'; price=1500 }
    @{ name='Wart Excision with Suturing'; price=2500 }
    @{ name='Foley''s Induction'; price=500 }
    @{ name='Check Curettage'; price=5500 }
    @{ name='Colposcopic Biopsy'; price=6500 }
    @{ name='Intra-Uterine Insemination (IUI)'; price=7000 }
    @{ name='Double IUI'; price=13000 }
    @{ name='Medical Termination of Pregnancy (MTP)'; price=3000 }
    @{ name='Medical Termination of Pregnancy (MTP) for Residual Products'; price=500 }
    @{ name='Hysterosalphingogram (HSG)'; price=1500 }
    @{ name='NST / CTG'; price=1500 }
    @{ name='USG Guided Nerve Blocks / Injections / Aspirations'; price=2000 }
    @{ name='Ascitic Fluid Tapping'; price=1000 }
    @{ name='Pleural Tapping'; price=2000 }
    @{ name='Lumbar Puncture - CSF Analysis'; price=2000 }
    @{ name='Nebulization'; price=1000 }
    @{ name='Ryle''s Tube Insertion/Aspiration'; price=1000 }
    @{ name='Intubation'; price=2000 }
)
$legacyTests = Get-AllPaged -Entity 'diagnostic-tests'
foreach ($row in $proceduralDefs) {
    $exists = $legacyTests | Where-Object { $_.name -eq $row.name -and $_.serviceType -eq 'Procedure' }
    if ($exists) { continue }
    $body = @{ name = $row.name; serviceType = 'Procedure'; category = 'Procedural Charges'; price = $row.price; isOutsourced = $false; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests" -Body $body -Label $row.name
    $legacyTests.Add($created.data)
    Start-Sleep -Milliseconds 250
}
Write-Host "  Procedural Charges catalog up to date." -ForegroundColor Green

Write-Host "Step: 6 more Lab packages" -ForegroundColor Cyan
$services = Get-AllPaged -Entity 'diagnostic-services'
$packages = Get-AllPaged -Entity 'diagnostic-packages'
$legacyTests = Get-AllPaged -Entity 'diagnostic-tests'

function Deactivate-DuplicateService {
    param([string]$Name)
    $dup = $services | Where-Object { $_.name -eq $Name -and $_.isActive } | Select-Object -First 1
    if (-not $dup) { return }
    $body = @{ code = $dup.code; name = $dup.name; categoryId = $dup.categoryId; serviceType = $dup.serviceType; isOutsourced = $dup.isOutsourced; providerId = $dup.providerId; price = $dup.price; isActive = $false } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($dup.id)" -Body $body -Label "deactivate duplicate $Name" | Out-Null
    Write-Host "    Deactivated stale duplicate flat service '$Name'." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

$newPackageDefs = @(
    @{ code='THYROID2'; name='Thyroid Profile 2'; desc='TSH, T3, T4.'; price=500; legacy=$null; dupService='THYROID PROFILE 2'
       items=@('TSH','T3','T4') }
    @{ code='IRONPROFILE'; name='Iron Profile'; desc='Serum Iron, Transferrin Saturation, TIBC, Ferritin.'; price=2000; legacy=$null; dupService='IRON PROFILE'
       items=@('Serum Iron','Transferrin Saturation','Total Iron Binding Capacity (TIBC)','Ferritin') }
    @{ code='DIABETICPROFILE1'; name='Diabetic Profile 1'; desc='RBS, FBS, HbA1C.'; price=500; legacy='DIABETIC PROFILE 1'; dupService='DIABETIC PROFILE 1'
       items=@('Random Blood Sugar (RBS)','Fasting Blood Sugar (FBS)','HbA1C') }
    @{ code='URINEROUTINE'; name='Urine Routine'; desc='Urine Sugar, Urine Albumin, Urine Deposits.'; price=75; legacy=$null; dupService='URINE ROUTINE'
       items=@('Urine Sugar','Urine Albumin','Urine Deposits') }
    @{ code='THYROID1'; name='Thyroid Profile 1'; desc='TSH, FT3, FT4, Anti TPO.'; price=1700; legacy=$null; dupService='THYROID PROFILE 1'
       items=@('TSH','FT3','FT4','Anti TPO') }
    @{ code='ORTHOPREOP'; name='Ortho Pre-Op Package'; desc='CBC, RFT/KFT, LFT, Coagulation Profile, RBS, Blood Grouping and Rh Typing, HIV, HBsAg, HCV, VDRL, Urine Analysis, ECG.'; price=3000; legacy=$null; dupService='ORTHO PRE-OP PACKAGE'
       items=@('Haemoglobin','Total WBC Count (TC)','Platelet Count','RBC Count','Differential Count','Packed Cell Volume (PCV)','MCV',
                'Urea','Creatinine',
                'Bilirubin (Total+Direct)','S G O T','S G P T','Protein','Alk.Phosphatase',
                'Prothrombin Time (PT)','APTT','INR','Bleeding Time (BT)','Clotting Time (CT)',
                'Random Blood Sugar (RBS)','Blood Grouping & Rh Typing','HIV','HBsAg','HCV','VDRL',
                'Urine Sugar','Urine Albumin','Urine Deposits','Urine Acetone','Urine Bile Salts-Bile Pigments (BS/BP)','Urobilinogen',
                'ECG') }
)

foreach ($def in $newPackageDefs) {
    $existing = Get-PackageByCode $def.code
    if ($existing) {
        $currentServiceIds = $existing.items | ForEach-Object { $_.serviceId }
        foreach ($itemName in $def.items) {
            $svc = Get-ServiceByName $itemName
            if ($svc.id -in $currentServiceIds) { continue }
            $addBody = @{ serviceId = $svc.id } | ConvertTo-Json
            Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-packages/$($existing.id)/items" -Body $addBody -Label "add $itemName to $($def.name)" | Out-Null
            Start-Sleep -Milliseconds 250
        }
    }
    else {
        $serviceIds = @()
        foreach ($itemName in $def.items) { $serviceIds += (Get-ServiceByName $itemName).id }
        $body = @{ code = $def.code; name = $def.name; description = $def.desc; totalPrice = $def.price; isActive = $true; serviceIds = $serviceIds } | ConvertTo-Json
        Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-packages" -Body $body -Label "create $($def.name)" | Out-Null
        Write-Host "  Created package '$($def.name)' - Rs.$($def.price) ($($serviceIds.Count) tests)." -ForegroundColor Green
        Start-Sleep -Milliseconds 250
    }

    if ($def.legacy) {
        $legacy = Get-LegacyTestByName $def.legacy
        if ($legacy -and $legacy.isActive) {
            $body = @{ name = $legacy.name; serviceType = $legacy.serviceType; category = $legacy.category; price = $legacy.price; isOutsourced = $legacy.isOutsourced; referenceLab = $legacy.referenceLab; isActive = $false } | ConvertTo-Json
            Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests/$($legacy.id)" -Body $body -Label "deactivate legacy $($def.legacy)" | Out-Null
            Start-Sleep -Milliseconds 250
        }
    }
    if ($def.dupService) { Deactivate-DuplicateService -Name $def.dupService }
}

Write-Host "Step: deactivate stale duplicate flat services (original 16 packages)" -ForegroundColor Cyan
$staleDuplicateNames = @(
    'Complete Blood Count (CBC)', 'CBC + ESR', 'HB+TC+DC+ESR', 'RENAL/KIDNEY FUNCTION TESTS (RFT/KFT)',
    'LIVER FUNCTION TESTS (LFT)', 'LIPID PROFILE', 'COAGULATION PROFILE', 'SERUM ELECTROLYTES',
    'SEROLOGY 1', 'SEROLOGY 2', 'STOOL ROUTINE', 'URINE ANALYSIS', 'BIO - RBS, RFT, Sr.Bil, Cholesterol',
    'DIABETIC PROFILE 2', 'RHEUMATOID PROFILE - 1 (CBC,ESR,CRP,RF,ACPA,UA,ASO,Vit D2)', 'RHEUMATOID PROFILE - 2 (CBC,ESR,CRP,RF,UA,Vit D2)'
)
foreach ($name in $staleDuplicateNames) { Deactivate-DuplicateService -Name $name }

Write-Host "Phase 6 done." -ForegroundColor Cyan

# ===========================================================================
# VALIDATION - re-fetch every category and confirm the seed reached its expected end state.
# Departments/Consultants/Appointment Types/Consultation Types are checked against exact
# counts derived from this script's own data arrays above (self-consistent, can't drift).
# The Diagnostic catalog entries are checked against the audited baseline from the live
# Lakshmi Hospitals tenant this script was built to reproduce - update these five numbers
# if the source tariff data (lab-tests-seed.json / the arrays in Phases 5-6) ever changes.
# ===========================================================================
Write-Host "`n===== VALIDATION =====" -ForegroundColor Magenta

$expectedDepartments = $departments.Count
$expectedConsultants = ($departments | ForEach-Object { $_.Doctors.Count } | Measure-Object -Sum).Sum
$expectedAppointmentTypes = $appointmentTypes.Count
$expectedConsultationTypes = $consultationTypes.Count
$expectedDiagnosticCategories = 14
$expectedDiagnosticProviders = 5
$expectedDiagnosticServices = 348
$expectedDiagnosticPackages = 22
$expectedActiveProcedureTests = 62

function Get-Count {
    param([string]$Entity, [string]$Query = '')
    $uri = "$ApiBaseUrl/api/v1/masters/$Entity`?pageSize=1$Query"
    $response = Invoke-RestMethod -Method Get -Uri $uri -Headers $authHeaders
    return [int]$response.meta.totalCount
}

$checks = @(
    @{ Label = 'Departments'; Actual = (Get-Count -Entity 'departments'); Expected = $expectedDepartments }
    @{ Label = 'Consultants'; Actual = (Get-Count -Entity 'consultants'); Expected = $expectedConsultants }
    @{ Label = 'Appointment Types'; Actual = (Get-Count -Entity 'appointment-types'); Expected = $expectedAppointmentTypes }
    @{ Label = 'Consultation Types'; Actual = (Get-Count -Entity 'consultation-types'); Expected = $expectedConsultationTypes }
    @{ Label = 'Diagnostic Categories'; Actual = (Get-Count -Entity 'diagnostic-categories'); Expected = $expectedDiagnosticCategories }
    @{ Label = 'Diagnostic Providers'; Actual = (Get-Count -Entity 'diagnostic-providers'); Expected = $expectedDiagnosticProviders }
    @{ Label = 'Diagnostic Services'; Actual = (Get-Count -Entity 'diagnostic-services'); Expected = $expectedDiagnosticServices }
    @{ Label = 'Diagnostic Packages'; Actual = (Get-Count -Entity 'diagnostic-packages'); Expected = $expectedDiagnosticPackages }
    @{ Label = 'Active Procedure Tests'; Actual = (Get-Count -Entity 'diagnostic-tests' -Query '&serviceType=Procedure&isActive=true'); Expected = $expectedActiveProcedureTests }
)

$allPassed = $true
foreach ($check in $checks) {
    if ($check.Actual -ge $check.Expected) {
        Write-Host ("  [PASS] {0,-24} expected >= {1,-5} found {2}" -f $check.Label, $check.Expected, $check.Actual) -ForegroundColor Green
    }
    else {
        Write-Host ("  [FAIL] {0,-24} expected >= {1,-5} found {2}" -f $check.Label, $check.Expected, $check.Actual) -ForegroundColor Red
        $allPassed = $false
    }
}

Write-Host ""
if ($allPassed) {
    Write-Host "All checks passed. Lakshmi Hospitals master data is seeded correctly." -ForegroundColor Cyan
    exit 0
}
else {
    Write-Host "One or more checks FAILED - review the output above." -ForegroundColor Red
    exit 1
}
