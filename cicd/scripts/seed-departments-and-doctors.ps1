<#
.SYNOPSIS
    Seeds a hospital's Departments and Consultants (doctors) master data through the real
    Masters API - not a database migration, since this is tenant-specific reference data
    (every hospital has a different doctor roster), not a schema change every tenant should
    share.

.DESCRIPTION
    Logs in as the given hospital's Super Admin, then for each department in the seed list
    below: creates it if a department with that exact name doesn't already exist, then
    creates each doctor under it as a Consultant (code, name stored exactly as given,
    departmentId linking back to the department) if a consultant with that exact name
    doesn't already exist. Safe to re-run - already-seeded rows are skipped, not duplicated.

    Goes through the same POST /api/v1/masters/departments and
    /api/v1/masters/consultants endpoints the Masters UI itself uses, so this exercises the
    same validation and business logic a person clicking through the UI would - no
    reimplemented insert logic, no direct database writes.

.PARAMETER HospitalCode
    The tenant to seed (e.g. 'dev', 'qa', 'production' - whichever hospital was registered
    through the Platform Portal).

.PARAMETER Username
    That hospital's Super Admin username.

.PARAMETER Password
    That hospital's Super Admin password.

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.EXAMPLE
    ./seed-departments-and-doctors.ps1 -HospitalCode dev -Username devadmin -Password 'Dev@Hosp123'
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

# Department -> doctor roster, stored exactly as given (full name including title and
# degrees goes into Consultant.name as-is - there's no separate "qualifications" field on
# this entity, and splitting the degrees out would be inventing structure that isn't there).
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
    # @(...) forces array context even when the API returns exactly zero or one record -
    # ConvertFrom-Json unwraps a single-element JSON array to a bare object in Windows
    # PowerShell 5.1, and appending to that with += later fails with a confusing
    # "does not contain a method named 'op_Addition'" error once it's no longer empty.
    # The leading comma is a second, independent fix: PowerShell auto-unrolls a returned
    # array back down to its bare element(s) as it crosses the function's output stream,
    # so a 0- or 1-element @(...) here would otherwise arrive at the caller as $null or a
    # bare object anyway despite the @() above - the comma operator wraps it one more
    # level so it survives that unrolling intact.
    return ,@($response.data)
}

Write-Host "Fetching existing departments and consultants for idempotency checks..." -ForegroundColor Cyan
# System.Collections.Generic.List, not a plain array + +=, for the same reason as the
# @(...) wrap above - a List's Add() never silently degrades back into a scalar.
$existingDepartments = [System.Collections.Generic.List[object]]::new()
$existingDepartments.AddRange([object[]](Get-AllMasterRecords -Entity 'departments'))
$existingConsultants = [System.Collections.Generic.List[object]]::new()
$existingConsultants.AddRange([object[]](Get-AllMasterRecords -Entity 'consultants'))

$departmentCount = 0
$consultantCount = 0
$skippedDepartmentCount = 0
$skippedConsultantCount = 0

foreach ($dept in $departments) {
    $existingDept = $existingDepartments | Where-Object { $_.name -eq $dept.Name } | Select-Object -First 1

    if ($existingDept) {
        Write-Host "  Department '$($dept.Name)' already exists - skipping." -ForegroundColor DarkGray
        $deptId = $existingDept.id
        $skippedDepartmentCount++
    }
    else {
        $deptBody = @{ code = $dept.Code; name = $dept.Name } | ConvertTo-Json
        $created = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/v1/masters/departments" -Headers $authHeaders -Body $deptBody
        $deptId = $created.data.id
        Write-Host "  Created department '$($dept.Name)' ($($dept.Code))." -ForegroundColor Green
        $departmentCount++
        # New department, so its consultants can't already exist under it - but a
        # consultant sharing that exact name under a different department could; the
        # per-doctor check below still runs regardless.
        $existingDepartments.Add($created.data)
    }

    foreach ($doctorName in $dept.Doctors) {
        $existingDoctor = $existingConsultants | Where-Object { $_.name -eq $doctorName } | Select-Object -First 1
        if ($existingDoctor) {
            Write-Host "    Consultant '$doctorName' already exists - skipping." -ForegroundColor DarkGray
            $skippedConsultantCount++
            continue
        }

        # Consultant.Code was removed from the Masters module this session (see DecisionLog) —
        # CreateConsultantRequest no longer has a Code field at all, so nothing is generated or
        # sent for it here anymore. Two consultants can legitimately share a display name; the
        # UI disambiguates via Specialization instead now.
        $consultantBody = @{ name = $doctorName; departmentId = $deptId } | ConvertTo-Json
        $createdConsultant = Invoke-RestMethod -Method Post -Uri "$ApiBaseUrl/api/v1/masters/consultants" -Headers $authHeaders -Body $consultantBody
        Write-Host "    Created consultant '$doctorName'." -ForegroundColor Green
        $consultantCount++
        $existingConsultants.Add($createdConsultant.data)
    }
}

Write-Host ""
Write-Host "Done. Departments: $departmentCount created, $skippedDepartmentCount already existed." -ForegroundColor Cyan
Write-Host "Consultants: $consultantCount created, $skippedConsultantCount already existed." -ForegroundColor Cyan
