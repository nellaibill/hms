<#
.SYNOPSIS
    Closes the remaining gaps against "LAB TESTS AND TARIFFS (3).xlsm" left after
    refresh-lab-tariffs-v3.ps1: fixes Consultation Type prices, imports the full Radiology
    (Ultrasound/Doppler) catalog plus 2 stale-price fixes / 1 rename / 1 missing HSG row, imports
    the 53-row Procedural Charges catalog into the legacy Procedure DiagnosticTest catalog, and
    creates 6 more real DiagnosticPackages (Thyroid Profile 1/2, Iron Profile, Diabetic Profile 1,
    Urine Routine, Ortho Pre-Op Package).

    Also fixes a data-quality bug discovered while verifying this pass: every one of the 16
    DiagnosticPackages built by refresh-lab-tariffs-v3.ps1 (and these 6 new ones) still had a
    stale, duplicate flat DiagnosticService active under its original ALL-CAPS name - a leftover
    from the very first migrate-diagnostic-tests-to-services.ps1 run, which moved every
    Laboratory/Radiology row (including package-priced ones, before the Package concept existed)
    into DiagnosticService as a flat, individually priced "test". refresh-lab-tariffs-v3.ps1's
    package step only deactivated the source row in the legacy DiagnosticTest catalog - it never
    checked for this separate duplicate - so Laboratory Billing's Services list has been showing
    both the clean package (e.g. "Lipid Profile") and its orphaned flat twin (e.g. "LIPID
    PROFILE") side by side since that pass shipped. This script deactivates all 22 of them.

.DESCRIPTION
    Safe to re-run - every step checks for existing state first, same idempotent shape as
    refresh-lab-tariffs-v3.ps1.

.PARAMETER HospitalCode
    The tenant to update (e.g. 'lhs').

.PARAMETER Username / Password
    That hospital's Super Admin credentials.

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.EXAMPLE
    ./import-radiology-procedures-v3.ps1 -HospitalCode lhs -Username lhsadmin -Password 'Lakshmi@123'
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
        # No leading-comma trick here - see refresh-lab-tariffs-v3.ps1's own comment on why that
        # trick, applied at this exact spot, silently under-fetched before.
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

Write-Host "Fetching current services/categories/providers/packages/legacy tests/consultation types..." -ForegroundColor Cyan
$services = Get-AllPaged -Entity 'diagnostic-services'
$categories = Get-AllPaged -Entity 'diagnostic-categories'
$packages = Get-AllPaged -Entity 'diagnostic-packages'
$legacyTests = Get-AllPaged -Entity 'diagnostic-tests'
$consultationTypes = Get-AllPaged -Entity 'consultation-types'

function Get-ServiceByName {
    param([string]$Name)
    $svc = $services | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if (-not $svc) { throw "Service not found by exact name: '$Name' - check spelling against the DB." }
    return $svc
}
function Get-CategoryByCode { param([string]$Code) return ($categories | Where-Object { $_.code -eq $Code } | Select-Object -First 1) }
function Get-PackageByCode { param([string]$Code) return ($packages | Where-Object { $_.code -eq $Code } | Select-Object -First 1) }
function Get-LegacyTestByName { param([string]$Name) return ($legacyTests | Where-Object { $_.name -eq $Name } | Select-Object -First 1) }

function Deactivate-DuplicateService {
    param([string]$Name)
    $dup = $services | Where-Object { $_.name -eq $Name -and $_.isActive } | Select-Object -First 1
    if (-not $dup) {
        Write-Host "    No active duplicate service named '$Name' - skipping." -ForegroundColor DarkGray
        return
    }
    $body = @{
        code = $dup.code; name = $dup.name; categoryId = $dup.categoryId; serviceType = $dup.serviceType
        isOutsourced = $dup.isOutsourced; providerId = $dup.providerId; price = $dup.price; isActive = $false
    } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($dup.id)" -Body $body -Label "deactivate duplicate $Name" | Out-Null
    Write-Host "    Deactivated stale duplicate flat service '$Name'." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

# ---------------------------------------------------------------------------
# Step 1: Consultation Type prices
# ---------------------------------------------------------------------------
Write-Host "`nStep 1: Consultation Type prices" -ForegroundColor Cyan
$consultationUpdates = @(
    @{ name = "Doctor's Consultation (In-house) - Regular"; amount = 250 }
    @{ name = "Doctor's Consultation (In-house) - Priority"; amount = 400 }
)
foreach ($u in $consultationUpdates) {
    $ct = $consultationTypes | Where-Object { $_.name -eq $u.name } | Select-Object -First 1
    if (-not $ct) { Write-Host "  '$($u.name)' not found - skipping." -ForegroundColor Yellow; continue }
    if ($ct.amount -eq $u.amount) {
        Write-Host "  '$($u.name)' already at Rs.$($u.amount) - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{ name = $ct.name; amount = $u.amount; isActive = $ct.isActive } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/consultation-types/$($ct.id)" -Body $body -Label "price update $($u.name)" | Out-Null
    Write-Host "  '$($u.name)': Rs.$($ct.amount) -> Rs.$($u.amount)" -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

# ---------------------------------------------------------------------------
# Step 2: Radiology - fix 2 stale prices, rename 1, add missing HSG + ECHO (Adult),
#         create the Ultrasound category, import 49 Ultrasound/Doppler services
# ---------------------------------------------------------------------------
Write-Host "`nStep 2: Radiology catalog" -ForegroundColor Cyan

$radiologyPriceFixes = @(
    @{ name = 'Digital X-ray Large films for Femur/Tibia/Humerus'; price = 500 }
    @{ name = 'Digital X-ray Long Leg View Scannogram'; price = 800 }
)
foreach ($u in $radiologyPriceFixes) {
    $svc = Get-ServiceByName $u.name
    if ($svc.price -eq $u.price) {
        Write-Host "  '$($u.name)' already at Rs.$($u.price) - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{
        code = $svc.code; name = $svc.name; categoryId = $svc.categoryId; serviceType = $svc.serviceType
        isOutsourced = $svc.isOutsourced; providerId = $svc.providerId; price = $u.price; isActive = $svc.isActive
    } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($svc.id)" -Body $body -Label "price fix $($u.name)" | Out-Null
    Write-Host "  '$($u.name)': Rs.$($svc.price) -> Rs.$($u.price)" -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

$staleName = 'Neonatal / Paediatric ECHO'
$correctName = 'Neonatal / Paediatric / Complex Fetal ECHO'
$staleEcho = $services | Where-Object { $_.name -eq $staleName } | Select-Object -First 1
if ($staleEcho) {
    $body = @{
        code = $staleEcho.code; name = $correctName; categoryId = $staleEcho.categoryId; serviceType = $staleEcho.serviceType
        isOutsourced = $staleEcho.isOutsourced; providerId = $staleEcho.providerId; price = $staleEcho.price; isActive = $staleEcho.isActive
    } | ConvertTo-Json
    Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($staleEcho.id)" -Body $body -Label 'rename Neonatal ECHO' | Out-Null
    Write-Host "  Renamed '$staleName' -> '$correctName'." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
} else {
    Write-Host "  '$staleName' not found (already renamed?) - skipping." -ForegroundColor DarkGray
}

$ultrasoundCategory = Get-CategoryByCode 'ULTRASOUND'
if (-not $ultrasoundCategory) {
    $body = @{ code = 'ULTRASOUND'; name = 'Ultrasound'; description = 'Ultrasound and Doppler studies (USG/Doppler subtypes).'; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-categories" -Body $body -Label 'create Ultrasound category'
    $categories.Add($created.data)
    $ultrasoundCategory = $created.data
    Write-Host "  Created category 'Ultrasound'." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
} else {
    Write-Host "  Category 'Ultrasound' already exists - skipping." -ForegroundColor DarkGray
}

$digitalXray = Get-CategoryByCode 'DIGITALXRAY'
$cardiology = Get-CategoryByCode 'CARDIOLOGY'

$extraRadiologyServices = @(
    @{ code = 'HSGXRAY'; name = 'Hysterosalphingogram (HSG)'; categoryId = $digitalXray.id; price = 1000 }
    @{ code = 'ECHOADULT'; name = 'ECHO (Adult)'; categoryId = $cardiology.id; price = 2000 }
)
foreach ($n in $extraRadiologyServices) {
    if ($services | Where-Object { $_.name -eq $n.name -and $_.serviceType -eq 'Radiology' }) {
        Write-Host "  '$($n.name)' already exists - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{ code = $n.code; name = $n.name; categoryId = $n.categoryId; serviceType = 'Radiology'; isOutsourced = $false; providerId = $null; price = $n.price; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services" -Body $body -Label "create $($n.name)"
    $services.Add($created.data)
    Write-Host "  Created '$($n.name)' - Rs.$($n.price)." -ForegroundColor Green
    Start-Sleep -Milliseconds 350
}

# Rows 341-391 of the sheet, excluding row 392 ("All Scans and Doppler - Bedside" - a
# "+Rs.200/+Rs.500 extra" surcharge modifier, not a standalone billable service).
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
    if ($services | Where-Object { $_.name -eq $n.name -and $_.serviceType -eq 'Radiology' }) {
        Write-Host "  '$($n.name)' already exists - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{ code = $n.code; name = $n.name; categoryId = $ultrasoundCategory.id; serviceType = 'Radiology'; isOutsourced = $false; providerId = $null; price = $n.price; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-services" -Body $body -Label "create $($n.name)"
    $services.Add($created.data)
    Write-Host "  Created '$($n.name)' - Rs.$($n.price)." -ForegroundColor Green
    Start-Sleep -Milliseconds 250
}

# ---------------------------------------------------------------------------
# Step 3: Procedural Charges -> legacy DiagnosticTest catalog, ServiceType=Procedure
# ---------------------------------------------------------------------------
Write-Host "`nStep 3: Procedural Charges (Procedure catalog)" -ForegroundColor Cyan
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
foreach ($row in $proceduralDefs) {
    $exists = $legacyTests | Where-Object { $_.name -eq $row.name -and $_.serviceType -eq 'Procedure' }
    if ($exists) {
        Write-Host "  '$($row.name)' already exists - skipping." -ForegroundColor DarkGray
        continue
    }
    $body = @{ name = $row.name; serviceType = 'Procedure'; category = 'Procedural Charges'; price = $row.price; isOutsourced = $false; isActive = $true } | ConvertTo-Json
    $created = Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests" -Body $body -Label "create $($row.name)"
    $legacyTests.Add($created.data)
    Write-Host "  Created '$($row.name)' - Rs.$($row.price)." -ForegroundColor Green
    Start-Sleep -Milliseconds 250
}

# ---------------------------------------------------------------------------
# Step 4: 6 more Lab packages (real item compositions), then deactivate each one's
#         source legacy DiagnosticTest row (if any) and its duplicate flat DiagnosticService
# ---------------------------------------------------------------------------
Write-Host "`nStep 4: 6 more Lab packages" -ForegroundColor Cyan

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
        Write-Host "  '$($def.name)' already exists - reconciling items." -ForegroundColor DarkGray
        $currentServiceIds = $existing.items | ForEach-Object { $_.serviceId }
        foreach ($itemName in $def.items) {
            $svc = Get-ServiceByName $itemName
            if ($svc.id -in $currentServiceIds) { continue }
            $addBody = @{ serviceId = $svc.id } | ConvertTo-Json
            Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-packages/$($existing.id)/items" -Body $addBody -Label "add $itemName to $($def.name)" | Out-Null
            Write-Host "    Added missing item '$itemName'." -ForegroundColor Green
            Start-Sleep -Milliseconds 250
        }
    } else {
        $serviceIds = @()
        $sum = 0
        foreach ($itemName in $def.items) {
            $svc = Get-ServiceByName $itemName
            $serviceIds += $svc.id
            $sum += $svc.price
        }
        $body = @{ code = $def.code; name = $def.name; description = $def.desc; totalPrice = $def.price; isActive = $true; serviceIds = $serviceIds } | ConvertTo-Json
        Invoke-WithRetry -Method Post -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-packages" -Body $body -Label "create $($def.name)" | Out-Null
        Write-Host "  Created package '$($def.name)' - Rs.$($def.price) ($($serviceIds.Count) tests, item sum Rs.$sum)." -ForegroundColor Green
        Start-Sleep -Milliseconds 250
    }

    if ($def.legacy) {
        $legacy = Get-LegacyTestByName $def.legacy
        if ($legacy -and $legacy.isActive) {
            $body = @{ name = $legacy.name; serviceType = $legacy.serviceType; category = $legacy.category; price = $legacy.price; isOutsourced = $legacy.isOutsourced; referenceLab = $legacy.referenceLab; isActive = $false } | ConvertTo-Json
            Invoke-WithRetry -Method Put -Uri "$ApiBaseUrl/api/v1/masters/diagnostic-tests/$($legacy.id)" -Body $body -Label "deactivate legacy $($def.legacy)" | Out-Null
            Write-Host "    Deactivated source legacy row '$($def.legacy)'." -ForegroundColor Green
            Start-Sleep -Milliseconds 250
        }
    }

    if ($def.dupService) {
        Deactivate-DuplicateService -Name $def.dupService
    }
}

# ---------------------------------------------------------------------------
# Step 5: data-quality fix - deactivate the 16 stale duplicate flat services left
#         over from the original 16 packages built in refresh-lab-tariffs-v3.ps1
# ---------------------------------------------------------------------------
Write-Host "`nStep 5: deactivate stale duplicate flat services (original 16 packages)" -ForegroundColor Cyan
$staleDuplicateNames = @(
    'Complete Blood Count (CBC)'
    'CBC + ESR'
    'HB+TC+DC+ESR'
    'RENAL/KIDNEY FUNCTION TESTS (RFT/KFT)'
    'LIVER FUNCTION TESTS (LFT)'
    'LIPID PROFILE'
    'COAGULATION PROFILE'
    'SERUM ELECTROLYTES'
    'SEROLOGY 1'
    'SEROLOGY 2'
    'STOOL ROUTINE'
    'URINE ANALYSIS'
    'BIO - RBS, RFT, Sr.Bil, Cholesterol'
    'DIABETIC PROFILE 2'
    'RHEUMATOID PROFILE - 1 (CBC,ESR,CRP,RF,ACPA,UA,ASO,Vit D2)'
    'RHEUMATOID PROFILE - 2 (CBC,ESR,CRP,RF,UA,Vit D2)'
)
foreach ($name in $staleDuplicateNames) {
    Deactivate-DuplicateService -Name $name
}

Write-Host "`nDone." -ForegroundColor Cyan
