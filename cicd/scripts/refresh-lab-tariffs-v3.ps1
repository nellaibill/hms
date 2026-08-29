<#
.SYNOPSIS
    One-time data refresh for Central Laboratory against "LAB TESTS AND TARIFFS (3).xlsm":
    fixes the duplicate Q-LAB provider, updates 2 changed prices, adds 2 new outsourced
    services, creates 16 real DiagnosticPackages (deactivating their source flat legacy
    DiagnosticTest row where one exists), and adds Injection Charges / Files as Procedure-type
    legacy DiagnosticTest rows (Procedure Billing already reads that catalog unchanged).

.DESCRIPTION
    Safe to re-run — every step checks for existing state first (same idempotency approach as
    migrate-diagnostic-tests-to-services.ps1 and seed-appointment-and-consultation-types.ps1).

.PARAMETER HospitalCode
    The tenant to update (e.g. 'lhs').

.PARAMETER Username / Password
    That hospital's Super Admin credentials.

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.EXAMPLE
    ./refresh-lab-tariffs-v3.ps1 -HospitalCode lhs -Username lhsadmin -Password 'Lakshmi@123'
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
if (-not $token) { throw "Login succeeded but no token was returned." }
Write-Host "Signed in as $($loginResponse.data.user.username)." -ForegroundColor Green
$authHeaders = @{ 'Content-Type' = 'application/json'; Authorization = "Bearer $token" }

function Get-AllPaged {
    param([string]$Entity)
    $all = [System.Collections.Generic.List[object]]::new()
    $page = 1
    do {
        $cacheBust = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
        $uri = "$ApiBaseUrl/api/v1/masters/$Entity`?page=$page&pageSize=100&_=$cacheBust"
        $response = Invoke-RestMethod -Method Get -Uri $uri -Headers $authHeaders
        # No leading-comma trick here - see migrate-diagnostic-tests-to-services.ps1's own
        # comment on why that trick, applied at this exact spot, silently under-fetched before.
        $all.AddRange([object[]]@($response.data))
        $totalPages = $response.meta.totalPages
        $page++
    } while ($page -le $totalPages)
    return , $all
}

function Invoke-WithRetry {
    param([string]$Method, [string]$Uri, [string]$Body, [string]$Label)
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

Write-Host "Fetching current services/categories/providers/packages/legacy tests..." -ForegroundColor Cyan
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

# ---------------------------------------------------------------------------
# Step 1: merge the duplicate Q-LAB provider (Q-LAB Diagnostics -> Q - LAB)
# ---------------------------------------------------------------------------
Write-Host "`nStep 1: Q-LAB provider merge" -ForegroundColor Cyan
$canonicalQLab = Get-ProviderByCode 'QLAB2'   # "Q - LAB" - 121 real services already reference it
$duplicateQLab = Get-ProviderByCode 'QLAB'    # "Q-LAB Diagnostics" - only my own manual test service

if ($canonicalQLab -and $duplicateQLab -and $duplicateQLab.isActive) {
    $affected = $services | Where-Object { $_.providerId -eq $duplicateQLab.id }
    foreach ($svc in $affected) {
        $body = @{
            code = $svc.code; name = $svc.name; categoryId = $svc.categoryId; serviceType = $svc.serviceType
            isOutsourced = $svc.isOutsourced; providerId = $canonicalQLab.id; price = $svc.price; isActive = $svc.isActive
        } | ConvertTo-Json
        Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($svc.id)" -Body $body -Label "repoint $($svc.name)" | Out-Null
        Write-Host "  Repointed '$($svc.name)' from duplicate Q-LAB to canonical." -ForegroundColor Green
        Start-Sleep -Milliseconds 350
    }
    $deactivateBody = @{ code = $duplicateQLab.code; name = $duplicateQLab.name; contactDetails = $duplicateQLab.contactDetails; isActive = $false } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-providers/$($duplicateQLab.id)" -Body $deactivateBody -Label 'deactivate duplicate Q-LAB' | Out-Null
    Write-Host "  Deactivated duplicate provider 'Q-LAB Diagnostics'." -ForegroundColor Green
} else {
    Write-Host "  Already merged or nothing to do - skipping." -ForegroundColor DarkGray
}

# ---------------------------------------------------------------------------
# Step 2: price updates
# ---------------------------------------------------------------------------
Write-Host "`nStep 2: price updates" -ForegroundColor Cyan
$priceUpdates = @(
    @{ name = 'Total Cholesterol'; price = 100 }
    @{ name = 'PAP Smear'; price = 800 }
)
# refresh services list after step 1's repoints
$services = Get-AllPaged -Entity 'diagnostic-services'
foreach ($u in $priceUpdates) {
    $svc = Get-ServiceByName $u.name
    if ($svc.price -eq $u.price) {
        Write-Host "  '$($u.name)' already at Rs.$($u.price) - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{
        code = $svc.code; name = $svc.name; categoryId = $svc.categoryId; serviceType = $svc.serviceType
        isOutsourced = $svc.isOutsourced; providerId = $svc.providerId; price = $u.price; isActive = $svc.isActive
    } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($svc.id)" -Body $body -Label "price update $($u.name)" | Out-Null
    Write-Host "  '$($u.name)': Rs.$($svc.price) -> Rs.$($u.price)" -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

# ---------------------------------------------------------------------------
# Step 3: two new outsourced services
# ---------------------------------------------------------------------------
Write-Host "`nStep 3: new services" -ForegroundColor Cyan
$endocrinology = Get-CategoryByCode 'ENDOCRINOLOGY'
$anderson = Get-ProviderByCode 'ANDERSON'
$newServices = @(
    @{ code = 'ANTIB2MICRO'; name = 'Anti-Beta 2 (β2) Microglobulin' }
    @{ code = 'ANTIB2GLYIGM'; name = 'Anti-Beta 2 (β2) Glycoprotein IgM' }
)
foreach ($n in $newServices) {
    if ($services | Where-Object { $_.name -eq $n.name }) {
        Write-Host "  '$($n.name)' already exists - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{
        code = $n.code; name = $n.name; categoryId = $endocrinology.id; serviceType = 'Laboratory'
        isOutsourced = $true; providerId = $anderson.id; price = 1400; isActive = $true
    } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services" -Body $body -Label "create $($n.name)"
    $services.Add($created.data)
    Write-Host "  Created '$($n.name)' - Rs.1400, outsourced to ANDERSON." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

# ---------------------------------------------------------------------------
# Step 4: the 16 packages
# ---------------------------------------------------------------------------
Write-Host "`nStep 4: packages" -ForegroundColor Cyan

# Each entry: package Code/Name/Description/TotalPrice, the member service names (resolved
# against the live DB by exact name), and the source legacy DiagnosticTest row's name to
# deactivate afterward (null if this package never existed as its own flat legacy row - only
# "Complete Blood Count (CBC)" alone falls in that case; see the plan's own reasoning for why
# each composition was chosen).
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
        # Already exists (e.g. "Lipid Profile" was created manually during earlier testing) -
        # don't skip outright, reconcile its item list against the intended one instead. Found
        # live: the manual Lipid Profile package was missing Triglycerides.
        Write-Host "  '$($def.name)' already exists - reconciling items." -ForegroundColor DarkGray
        $currentServiceIds = $existing.items | ForEach-Object { $_.serviceId }
        foreach ($itemName in $def.items) {
            $svc = Get-ServiceByName $itemName
            if ($svc.id -in $currentServiceIds) { continue }
            $addBody = @{ serviceId = $svc.id } | ConvertTo-Json
            Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-packages/$($existing.id)/items" -Body $addBody -Label "add $itemName to $($def.name)" | Out-Null
            Write-Host "    Added missing item '$itemName'." -ForegroundColor Green
            Start-Sleep -Milliseconds 350
        }
    } else {
        $serviceIds = @()
        foreach ($itemName in $def.items) {
            $svc = Get-ServiceByName $itemName
            $serviceIds += $svc.id
        }
        $body = @{ code = $def.code; name = $def.name; description = $def.desc; totalPrice = $def.price; isActive = $true; serviceIds = $serviceIds } | ConvertTo-Json
        Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-packages" -Body $body -Label "create $($def.name)" | Out-Null
        Write-Host "  Created package '$($def.name)' - Rs.$($def.price), $($serviceIds.Count) tests." -ForegroundColor Green
        Start-Sleep -Milliseconds 350
    }

    if ($def.legacy) {
        $legacy = Get-LegacyTestByName $def.legacy
        if ($legacy -and $legacy.isActive) {
            $body = @{ name = $legacy.name; serviceType = $legacy.serviceType; category = $legacy.category; price = $legacy.price; isOutsourced = $legacy.isOutsourced; referenceLab = $legacy.referenceLab; isActive = $false } | ConvertTo-Json
            Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests/$($legacy.id)" -Body $body -Label "deactivate legacy $($def.legacy)" | Out-Null
            Write-Host "    Deactivated source legacy row '$($def.legacy)'." -ForegroundColor Green
            Start-Sleep -Milliseconds 350
        }
    }
}

# ---------------------------------------------------------------------------
# Step 5: Injection Charges + Files -> legacy DiagnosticTest catalog, ServiceType=Procedure
# ---------------------------------------------------------------------------
Write-Host "`nStep 5: Injection Charges + Files (Procedure catalog)" -ForegroundColor Cyan
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
    if ($exists) {
        Write-Host "  '$($row.name)' already exists - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{ name = $row.name; serviceType = 'Procedure'; category = $row.category; price = $row.price; isOutsourced = $false; isActive = $true } | ConvertTo-Json
    Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests" -Body $body -Label "create $($row.name)" | Out-Null
    Write-Host "  Created '$($row.name)' [$($row.category)] - Rs.$($row.price)." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

Write-Host "`nDone." -ForegroundColor Cyan
