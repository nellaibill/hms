<#
.SYNOPSIS
    One-time data load: sets CostPrice ("Running Cost") on 378 individual tests/services from
    "LAB TESTS AND TARIFFS (3).xlsm" — every section EXCEPT Consultation Charges.

.DESCRIPTION
    Two data blocks, because the workbook uses two different column layouts and two different
    cost formulas:

    Block 1 (260 items) — In-house + Outsourced Blood/Urine/Stool/Semen/Body Fluids/
    Histopathology (Laboratory tests). Row 1's header applies here: TYPE/PACKAGES/TESTS/
    RUNNING COST/HOSPITAL TEST COST/PROFIT/PROFIT%, and CostPrice = RUNNING COST directly.
    Verified Price - Cost = Profit exactly on all 260 rows.

    Block 2 (118 items) — Radiology, Procedural Charges, Injection Charges, and Files. These
    sections have their OWN header row (row 328, not row 1): TYPE/SUBTYPE/INVESTIGATION NAME/
    DOCTOR'S CHARGES/HOSPITAL RUNNING COST/HOSPITAL TEST COST/HOSPITAL INCOME (Test cost -
    Doctor's charges)/PROFIT/PROFIT % — one column further right than Block 1's layout, which
    is what made this section look inconsistent at first (an earlier pass misread "Hospital
    Running Cost" as "Hospital Test Cost" here, off by one column). "Doctor's Charges" is a
    separate professional fee (e.g. a visiting radiologist/surgeon) netted out before Profit is
    computed: Hospital Income = Test Cost - Doctor's Charges, Profit = Hospital Income - Running
    Cost. Since our schema has no separate Doctor's Charges field and Price already matches
    Hospital Test Cost (confirmed against the live DB - zero price mismatches across 116 matched
    rows), CostPrice here = Running Cost + Doctor's Charges, so Price - CostPrice reproduces the
    workbook's own Profit figure exactly with no schema change.

    Consultation Charges is NOT included — it's priced through the separate ConsultationType
    master, which has no CostPrice field yet; adding one there would be its own small follow-up.

    Most items live in the newer DiagnosticService catalog (Laboratory/Radiology billing's real,
    current catalog); Procedural/Injection/File items and 2 Laboratory-section outliers
    ("Intra-Uterine Insemination (IUI)", "Double IUI" - gynaecological procedures, not lab tests,
    despite sitting under the workbook's Semen Analysis heading) are legacy DiagnosticTest rows
    instead. The `table` field on each entry says which, and the script calls the matching API.

    "Hysterosalphingogram (HSG)" legitimately appears in both blocks with different costs - it's
    two distinct real records (a plain-imaging Radiology service and a doctor-performed
    Procedural test), not a duplicate/conflict.

    Matches by name, case-insensitive and whitespace-collapsed (so a test name split across
    multiple lines in the original spreadsheet cell still matches the DB's single-line stored
    name). Safe to re-run - already-correct CostPrice values are skipped, and each PUT
    round-trips every other field on the record unchanged (same idempotent, full-record-PUT
    approach as refresh-lab-tariffs-v3.ps1).

.PARAMETER HospitalCode
    The tenant to update (e.g. 'lhs').

.PARAMETER Username / Password
    That hospital's Super Admin (or other identity-administration/diagnostics-permitted)
    credentials.

.PARAMETER ApiBaseUrl
    Defaults to the local dev API.

.PARAMETER WhatIf
    Preview only - logs every match/mismatch/skip without calling any PUT endpoint.

.EXAMPLE
    ./update-diagnostic-cost-prices.ps1 -HospitalCode lhs -Username lhsadmin -Password '...'

.EXAMPLE
    ./update-diagnostic-cost-prices.ps1 -HospitalCode lhs -Username lhsadmin -Password '...' -WhatIf
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

    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Data extracted from "LAB TESTS AND TARIFFS (3).xlsm" - TESTS name + RUNNING COST, for every
# row in the In-house/Outsourced Blood/Urine/Stool/Semen/Body Fluids/Histopathology sections
# (rows 2-328) where Price - Cost = Profit held exactly. `table` says which entity this test
# actually lives in today: 'service' = DiagnosticService (the normal case), 'test' = the
# legacy DiagnosticTest catalog.
# ---------------------------------------------------------------------------
$costUpdates = @(
    @{ name = 'Haemoglobin'; cost = 35; table = 'service' }
    @{ name = 'Total WBC Count (TC)'; cost = 35; table = 'service' }
    @{ name = 'Platelet Count'; cost = 70; table = 'service' }
    @{ name = 'RBC Count'; cost = 35; table = 'service' }
    @{ name = 'Differential Count'; cost = 35; table = 'service' }
    @{ name = 'Packed Cell Volume (PCV)'; cost = 40; table = 'service' }
    @{ name = 'MCV'; cost = 25; table = 'service' }
    @{ name = 'ESR'; cost = 25; table = 'service' }
    @{ name = 'Blood Grouping & Rh Typing'; cost = 70; table = 'service' }
    @{ name = 'Mantoux Test'; cost = 50; table = 'service' }
    @{ name = 'CRP'; cost = 150; table = 'service' }
    @{ name = 'Uric Acid (UA)'; cost = 40; table = 'service' }
    @{ name = 'RA Factor (RF)'; cost = 100; table = 'service' }
    @{ name = 'ASO Titre'; cost = 100; table = 'service' }
    @{ name = 'Urea'; cost = 35; table = 'service' }
    @{ name = 'Creatinine'; cost = 35; table = 'service' }
    @{ name = 'Calcium'; cost = 50; table = 'service' }
    @{ name = 'Lactate Dehydrogenase (LDH)'; cost = 100; table = 'service' }
    @{ name = 'Serum Amylase'; cost = 100; table = 'service' }
    @{ name = 'Serum Lipase'; cost = 200; table = 'service' }
    @{ name = 'Bilirubin (Total+Direct)'; cost = 75; table = 'service' }
    @{ name = 'S G O T'; cost = 75; table = 'service' }
    @{ name = 'S G P T'; cost = 75; table = 'service' }
    @{ name = 'Protein'; cost = 70; table = 'service' }
    @{ name = 'Albumin'; cost = 20; table = 'service' }
    @{ name = 'Alk.Phosphatase'; cost = 75; table = 'service' }
    @{ name = 'A/G Ratio'; cost = 70; table = 'service' }
    @{ name = 'Total Cholesterol'; cost = 30; table = 'service' }
    @{ name = 'Triglycerides'; cost = 50; table = 'service' }
    @{ name = 'HDL'; cost = 70; table = 'service' }
    @{ name = 'Prothrombin Time (PT)'; cost = 70; table = 'service' }
    @{ name = 'APTT'; cost = 50; table = 'service' }
    @{ name = 'INR'; cost = 30; table = 'service' }
    @{ name = 'Bleeding Time (BT)'; cost = 15; table = 'service' }
    @{ name = 'Clotting Time (CT)'; cost = 15; table = 'service' }
    @{ name = 'Capillary Blood Glucose (CBG) - Strip test'; cost = 15; table = 'service' }
    @{ name = 'Random Blood Sugar (RBS)'; cost = 20; table = 'service' }
    @{ name = 'Fasting Blood Sugar (FBS)'; cost = 20; table = 'service' }
    @{ name = 'Post Prandial Blood Sugar (PPBS)'; cost = 20; table = 'service' }
    @{ name = 'HbA1C'; cost = 150; table = 'service' }
    @{ name = 'Glucose Challenge Test (GCT)'; cost = 75; table = 'service' }
    @{ name = 'Glucose Tolerance Test (GTT)'; cost = 200; table = 'service' }
    @{ name = 'Serum Iron'; cost = 150; table = 'service' }
    @{ name = 'Transferrin Saturation'; cost = 550; table = 'service' }
    @{ name = 'Total Iron Binding Capacity (TIBC)'; cost = 150; table = 'service' }
    @{ name = 'Sodium'; cost = 70; table = 'service' }
    @{ name = 'Potassium'; cost = 70; table = 'service' }
    @{ name = 'Chloride'; cost = 70; table = 'service' }
    @{ name = 'Bicarbonate'; cost = 70; table = 'service' }
    @{ name = 'Anti-CCP'; cost = 700; table = 'service' }
    @{ name = '25-OH-Vitamin D2'; cost = 800; table = 'service' }
    @{ name = 'D-Dimer'; cost = 380; table = 'service' }
    @{ name = 'Ferritin'; cost = 450; table = 'service' }
    @{ name = 'Vitamin B12'; cost = 600; table = 'service' }
    @{ name = 'T3'; cost = 80; table = 'service' }
    @{ name = 'T4'; cost = 80; table = 'service' }
    @{ name = 'TSH'; cost = 80; table = 'service' }
    @{ name = 'FT3'; cost = 100; table = 'service' }
    @{ name = 'FT4'; cost = 100; table = 'service' }
    @{ name = 'Anti TPO'; cost = 600; table = 'service' }
    @{ name = 'Beta HCG'; cost = 300; table = 'service' }
    @{ name = 'LH'; cost = 160; table = 'service' }
    @{ name = 'FSH'; cost = 160; table = 'service' }
    @{ name = 'E2'; cost = 280; table = 'service' }
    @{ name = 'FE3'; cost = 290; table = 'service' }
    @{ name = 'Progesterone'; cost = 285; table = 'service' }
    @{ name = '17-OH-Progesterone'; cost = 700; table = 'service' }
    @{ name = 'Total Testosterone'; cost = 320; table = 'service' }
    @{ name = 'DHEAS'; cost = 1800; table = 'service' }
    @{ name = 'AMH'; cost = 1000; table = 'service' }
    @{ name = 'Prolactin'; cost = 230; table = 'service' }
    @{ name = 'HIV'; cost = 100; table = 'service' }
    @{ name = 'HBsAg'; cost = 45; table = 'service' }
    @{ name = 'HCV'; cost = 120; table = 'service' }
    @{ name = 'VDRL'; cost = 40; table = 'service' }
    @{ name = 'Dengue NS1 Ag + IgG-M Ab'; cost = 300; table = 'service' }
    @{ name = 'Urine Pregnancy Test (UPT)'; cost = 50; table = 'service' }
    @{ name = 'Urine Sugar'; cost = 10; table = 'service' }
    @{ name = 'Urine Albumin'; cost = 10; table = 'service' }
    @{ name = 'Urine Deposits'; cost = 15; table = 'service' }
    @{ name = 'Urine Acetone'; cost = 15; table = 'service' }
    @{ name = 'Urine Bile Salts-Bile Pigments (BS/BP)'; cost = 15; table = 'service' }
    @{ name = 'Urobilinogen'; cost = 10; table = 'service' }
    @{ name = 'Stool for Occult Blood'; cost = 50; table = 'service' }
    @{ name = 'Stool for Ova / Cyst'; cost = 20; table = 'service' }
    @{ name = 'Semen Analysis'; cost = 100; table = 'service' }
    # Intra-Uterine Insemination (IUI) / Double IUI are deliberately NOT here even though they
    # sit physically in this part of the workbook (under Semen Analysis) - they're gynaecological
    # procedures, not lab tests, and the workbook's OWN more complete entry for them (under
    # PROCEDURAL CHARGES, with a proper Doctor's Charges breakdown) gives a very different,
    # more trustworthy cost. See the second data block below.
    @{ name = 'Absolute Eosinophil Count'; cost = 100; table = 'service' }
    @{ name = 'Reticulocyte Count'; cost = 180; table = 'service' }
    @{ name = 'Peripheral Smear (Path)'; cost = 200; table = 'service' }
    @{ name = 'Hb Electrophoresis'; cost = 550; table = 'service' }
    @{ name = 'Serum Electrophoresis'; cost = 550; table = 'service' }
    @{ name = 'hs CRP'; cost = 200; table = 'service' }
    @{ name = 'Serum Lactate'; cost = 200; table = 'service' }
    @{ name = 'Ammonia'; cost = 400; table = 'service' }
    @{ name = 'Serum Homocysteine'; cost = 750; table = 'service' }
    @{ name = 'Gamma Glutamyl Transferase (GGT)'; cost = 150; table = 'service' }
    @{ name = 'Acid Phosphatase'; cost = 150; table = 'service' }
    @{ name = 'Serum Bile Acid'; cost = 1250; table = 'service' }
    @{ name = 'Cholinesterase'; cost = 380; table = 'service' }
    @{ name = 'Magnesium'; cost = 150; table = 'service' }
    @{ name = 'Phosphorus'; cost = 50; table = 'service' }
    @{ name = 'Lithium'; cost = 450; table = 'service' }
    @{ name = 'Zinc'; cost = 450; table = 'service' }
    @{ name = 'Serum Copper'; cost = 500; table = 'service' }
    @{ name = 'Serum Cerruloplasmin'; cost = 480; table = 'service' }
    @{ name = 'Heavy Metal Screening'; cost = 3500; table = 'service' }
    @{ name = 'Direct Coombs Test'; cost = 160; table = 'service' }
    @{ name = 'Indirect Coombs Test'; cost = 160; table = 'service' }
    @{ name = 'Influenza Screening'; cost = 3500; table = 'service' }
    @{ name = 'Widal'; cost = 150; table = 'service' }
    @{ name = 'Malarial Optimal Card'; cost = 100; table = 'service' }
    @{ name = 'Smear for MP'; cost = 180; table = 'service' }
    @{ name = 'Smear for MF'; cost = 180; table = 'service' }
    @{ name = 'Smear for Gram Stain'; cost = 100; table = 'service' }
    @{ name = 'Smear for AFB'; cost = 100; table = 'service' }
    @{ name = 'Smear for Fungal Stain'; cost = 150; table = 'service' }
    @{ name = 'Blood Culture and Sensitivity'; cost = 550; table = 'service' }
    @{ name = 'Mycobacterium TB Culture'; cost = 600; table = 'service' }
    @{ name = 'Anti-HAV IgM Antibody'; cost = 250; table = 'service' }
    @{ name = 'HIV - ELISA'; cost = 200; table = 'service' }
    @{ name = 'HIV - Western Blot'; cost = 1000; table = 'service' }
    @{ name = 'HBV - ELISA'; cost = 200; table = 'service' }
    @{ name = 'HBeAg'; cost = 600; table = 'service' }
    @{ name = 'HBV-DNA PCR Quantitative'; cost = 2800; table = 'service' }
    @{ name = 'HCV - ELISA'; cost = 250; table = 'service' }
    @{ name = 'HCV RNA PCR (Qualitative)'; cost = 1900; table = 'service' }
    @{ name = 'HCV RNA PCR (Quantitative)'; cost = 2700; table = 'service' }
    @{ name = 'Anti-HEV IgM Antibody'; cost = 250; table = 'service' }
    @{ name = 'Anti-HEV IgG Antibody'; cost = 250; table = 'service' }
    @{ name = 'HSV I & II IgM & IgG'; cost = 2800; table = 'service' }
    @{ name = 'Serological Test for Syphilis (STS)'; cost = 200; table = 'service' }
    @{ name = 'TPHA for Syphilis'; cost = 600; table = 'service' }
    @{ name = 'TB PCR'; cost = 1400; table = 'service' }
    @{ name = 'TB Gold Assay'; cost = 1450; table = 'service' }
    @{ name = 'Anti TB IgM'; cost = 250; table = 'service' }
    @{ name = 'Anti TB IgA'; cost = 250; table = 'service' }
    @{ name = 'Anti TB IgG'; cost = 250; table = 'service' }
    @{ name = 'Leptospiral Antibody IgM / IgG (ICT)'; cost = 450; table = 'service' }
    @{ name = 'Scrub Typhus'; cost = 300; table = 'service' }
    @{ name = 'Scrub Typhus ELISA IgM / IgG'; cost = 750; table = 'service' }
    @{ name = 'Chikungunya IgM'; cost = 400; table = 'service' }
    @{ name = 'TORCH Panel IgM'; cost = 600; table = 'service' }
    @{ name = 'TORCH Panel IgM + IgG'; cost = 1200; table = 'service' }
    @{ name = 'IgA Antibody'; cost = 270; table = 'service' }
    @{ name = 'IgE Antibody'; cost = 280; table = 'service' }
    @{ name = 'IgG Antibody'; cost = 270; table = 'service' }
    @{ name = 'IgM Antibody'; cost = 270; table = 'service' }
    @{ name = 'cf DNA'; cost = 12000; table = 'service' }
    @{ name = 'DS DNA - ELISA'; cost = 800; table = 'service' }
    @{ name = 'ANA - ELISA'; cost = 400; table = 'service' }
    @{ name = 'ANA - IF'; cost = 600; table = 'service' }
    @{ name = 'ANA BLOT'; cost = 1500; table = 'service' }
    @{ name = 'ANCA (C+P)'; cost = 2200; table = 'service' }
    @{ name = 'Myoglobin'; cost = 250; table = 'service' }
    @{ name = 'Smooth Muscular Antibody (SMA)'; cost = 2800; table = 'service' }
    @{ name = 'Anti-Beta 2 (?2) Microglobulin'; cost = 700; table = 'service' }
    @{ name = 'Anti-Beta 2 (?2) Glycoprotein IgM'; cost = 700; table = 'service' }
    @{ name = 'Anti-Cardiolipin Antibody IgM / IgA / IgG'; cost = 280; table = 'service' }
    @{ name = 'Lipoprotein (a)'; cost = 600; table = 'service' }
    @{ name = 'Anti GBM Antibody'; cost = 1800; table = 'service' }
    @{ name = 'Anti Thyroglobulin Antibody'; cost = 750; table = 'service' }
    @{ name = 'Thyroglobulin'; cost = 550; table = 'service' }
    @{ name = 'TSH Receptor Antibody'; cost = 3300; table = 'service' }
    @{ name = 'APLA IgM'; cost = 450; table = 'service' }
    @{ name = 'APLA IgG'; cost = 450; table = 'service' }
    @{ name = 'Anti Mitochondrial Antibody (AMA)'; cost = 1000; table = 'service' }
    @{ name = 'Anti LKM'; cost = 800; table = 'service' }
    @{ name = 'ASMA'; cost = 800; table = 'service' }
    @{ name = 'Lupus Anticoagulant'; cost = 600; table = 'service' }
    @{ name = 'Double Marker'; cost = 750; table = 'service' }
    @{ name = 'Quadruple Marker'; cost = 950; table = 'service' }
    @{ name = 'Triple Marker'; cost = 900; table = 'service' }
    @{ name = 'NIPT (Non Invasive Pregnancy Test)'; cost = 7000; table = 'service' }
    @{ name = 'Free Testosterone'; cost = 600; table = 'service' }
    @{ name = 'Androgen'; cost = 1500; table = 'service' }
    @{ name = 'Alpha Feto Protein (AFP)'; cost = 300; table = 'service' }
    @{ name = 'Growth Hormone (GH)'; cost = 780; table = 'service' }
    @{ name = 'EGFR'; cost = 80; table = 'service' }
    @{ name = 'Adreno Cortico Trophic Hormone (ACTH)'; cost = 1500; table = 'service' }
    @{ name = 'Cortisol'; cost = 300; table = 'service' }
    @{ name = 'Serum Insulin'; cost = 450; table = 'service' }
    @{ name = 'Folic Acid'; cost = 370; table = 'service' }
    @{ name = 'C Antibody'; cost = 750; table = 'service' }
    @{ name = 'C3 Complement'; cost = 250; table = 'service' }
    @{ name = 'C4 Complement'; cost = 350; table = 'service' }
    @{ name = 'NT Pro BNP'; cost = 1750; table = 'service' }
    @{ name = 'Interleukin (IL) - 6'; cost = 1300; table = 'service' }
    @{ name = 'Procalcitonin'; cost = 700; table = 'service' }
    @{ name = 'Vitamin D3 - 1,25 OH'; cost = 600; table = 'service' }
    @{ name = 'Intact PTH (Para Thyroid Hormone)'; cost = 900; table = 'service' }
    @{ name = 'HLA B27'; cost = 900; table = 'service' }
    @{ name = 'C-Peptide'; cost = 450; table = 'service' }
    @{ name = 'Serum Protein Electrophoresis'; cost = 450; table = 'service' }
    @{ name = 'Protein C'; cost = 1500; table = 'service' }
    @{ name = 'Protein S'; cost = 1500; table = 'service' }
    @{ name = 'Anti Thrombin III'; cost = 1500; table = 'service' }
    @{ name = 'Factor VIII Inhibitor'; cost = 2000; table = 'service' }
    @{ name = 'CA-15.3'; cost = 1000; table = 'service' }
    @{ name = 'CA-19.9'; cost = 600; table = 'service' }
    @{ name = 'CA-125'; cost = 400; table = 'service' }
    @{ name = 'CEA (Carcino Embryonic Antigen)'; cost = 600; table = 'service' }
    @{ name = 'Plasma Fibrinogen'; cost = 280; table = 'service' }
    @{ name = 'FDP (Fibrin Degradation Products)'; cost = 500; table = 'service' }
    @{ name = 'Digoxin'; cost = 750; table = 'service' }
    @{ name = 'Prostate Specific Antigen (PSA) - Total'; cost = 280; table = 'service' }
    @{ name = 'Prostate Specific Antigen (PSA) - Free'; cost = 780; table = 'service' }
    @{ name = 'Troponin T'; cost = 600; table = 'service' }
    @{ name = 'Troponin I'; cost = 500; table = 'service' }
    @{ name = 'CPK Total'; cost = 200; table = 'service' }
    @{ name = 'CPK MB'; cost = 110; table = 'service' }
    @{ name = 'Rh Antibody Titre'; cost = 500; table = 'service' }
    @{ name = 'POC (Products Of Conception) for TORCH'; cost = 7000; table = 'service' }
    @{ name = 'POC for QF PCR'; cost = 8500; table = 'service' }
    @{ name = 'Fetal POC Test - 5 Chr'; cost = 4950; table = 'service' }
    @{ name = 'Chromosome Analysis / Karyotyping'; cost = 1250; table = 'service' }
    @{ name = 'Arterial Blood Gas Analysis (ABG)'; cost = 1000; table = 'service' }
    @{ name = 'Serum Osmolarity'; cost = 650; table = 'service' }
    @{ name = 'Blood pH - Gel'; cost = 150; table = 'service' }
    @{ name = 'Urine For Micro-albumin'; cost = 200; table = 'service' }
    @{ name = '24 Hrs Urinary Creatinine'; cost = 180; table = 'service' }
    @{ name = '24 Hrs Urinary Protein'; cost = 75; table = 'service' }
    @{ name = 'Urine Spot Sodium'; cost = 180; table = 'service' }
    @{ name = '24 Hrs Urinary Sodium'; cost = 100; table = 'service' }
    @{ name = '24 Hrs Urinary Potasium'; cost = 120; table = 'service' }
    @{ name = '24 Hrs Urinary Copper'; cost = 1000; table = 'service' }
    @{ name = 'Bence - Jones Protein'; cost = 75; table = 'service' }
    @{ name = 'Urine Osmolarity'; cost = 450; table = 'service' }
    @{ name = 'Urine PCR'; cost = 55; table = 'service' }
    @{ name = 'Urine VMA Spot'; cost = 1000; table = 'service' }
    @{ name = 'Urine VMA 24 Hours'; cost = 2800; table = 'service' }
    @{ name = 'Stone Analysis'; cost = 800; table = 'service' }
    @{ name = 'Urine Culture and Sensitivity'; cost = 125; table = 'service' }
    @{ name = 'Stool Culture & Sensitivity'; cost = 150; table = 'service' }
    @{ name = 'Stool for Rotavirus'; cost = 600; table = 'service' }
    @{ name = 'Stool Reducing Substances'; cost = 10; table = 'service' }
    @{ name = 'Sputum / Aspirate / Swab / Tip - Pus C/S'; cost = 150; table = 'service' }
    @{ name = 'Sputum / Aspirate / Swab for Mycobacterium TB Culture'; cost = 600; table = 'service' }
    @{ name = 'Sputum / Aspirate / Swab for Gram Stain'; cost = 100; table = 'service' }
    @{ name = 'Sputum / Aspirate / Swab for AFB'; cost = 100; table = 'service' }
    @{ name = 'Sputum / Aspirate / Swab for Fungal Stain'; cost = 150; table = 'service' }
    @{ name = 'Cervical-Vaginal Swab / PAP for Human Papilloma Virus (HPV)'; cost = 1200; table = 'service' }
    @{ name = 'Fungal C/S'; cost = 250; table = 'service' }
    @{ name = 'Body Fluid (Pleural-Ascitic-Synovial fluid/ CSF) - C/S'; cost = 150; table = 'service' }
    @{ name = 'Body Fluid (Pleural-Ascitic-Synovial fluid/ CSF) - Mycobacterium TB Culture'; cost = 600; table = 'service' }
    @{ name = 'Body Fluid (Pleural-Ascitic-Synovial fluid/ CSF) - Cell Count'; cost = 190; table = 'service' }
    @{ name = 'Body Fluid (Pleural-Ascitic-Synovial fluid/ CSF) - Protein'; cost = 110; table = 'service' }
    @{ name = 'Body Fluid (Pleural-Ascitic-Synovial fluid/ CSF) - Sugar / Glucose'; cost = 40; table = 'service' }
    @{ name = 'Body Fluid (Pleural-Ascitic-Synovial fluid/ CSF) - Adenosine Deaminase (ADA) for MTB'; cost = 400; table = 'service' }
    @{ name = 'Body Fluid (Pleural-Ascitic-Synovial fluid/ CSF) - Lactate Dehydrogenase (LDH)'; cost = 400; table = 'service' }
    @{ name = 'Body Fluid (Pleural-Ascitic-Synovial fluid/ CSF) - Malignant Cells'; cost = 190; table = 'service' }
    @{ name = 'Biopsy Small (Appendix/Tubes/Scopy specimens/Colon/ Synovium/Curettage/Nodes/Small Bone Tumors/ Trucut/Swellings/Skin/Small Wide local excisions)'; cost = 400; table = 'service' }
    @{ name = 'Biopsy Medium (Uterus + Cx + Ovaries/Huge Myomas/ Thyroid/Testes/Medium Bone Tumors/Fingers and Toes)'; cost = 580; table = 'service' }
    @{ name = 'Biopsy Large (MRM with Nodes/Large Uterine-Ovarian Tumors/Large Bone Tumors/Large Body parts)'; cost = 1000; table = 'service' }
    @{ name = 'Emergency Biopsies - Trucut / Frozen section'; cost = 1200; table = 'service' }
    @{ name = 'PAP Smear'; cost = 450; table = 'service' }
    @{ name = 'Swabs / Aspirates / Other Samples HPE'; cost = 350; table = 'service' }
    @{ name = 'D&C Sample - Endometrial Curetage'; cost = 500; table = 'service' }
    @{ name = 'FNAC - 4 slides'; cost = 500; table = 'service' }

    # --- Block 2: Radiology, Procedural Charges, Injection Charges, Files (see .DESCRIPTION) ---
    @{ name = 'Digital X-ray'; cost = 100; table = 'service' }
    @{ name = 'Digital X-ray Large films for Femur/Tibia/Humerus'; cost = 150; table = 'service' }
    @{ name = 'Digital X-ray Long Leg View Scannogram'; cost = 200; table = 'service' }
    @{ name = 'Hysterosalphingogram (HSG)'; cost = 300; table = 'service' }
    @{ name = 'ECG'; cost = 50; table = 'service' }
    @{ name = 'ECHO (Adult)'; cost = 900; table = 'service' }
    @{ name = 'Neonatal / Paediatric / Complex Fetal ECHO'; cost = 4200; table = 'service' }
    @{ name = 'Abdomen and Pelvis (Adult/Neonatal/Paediatric)'; cost = 900; table = 'service' }
    @{ name = 'ANC - Early Pregnancy Scan'; cost = 900; table = 'service' }
    @{ name = 'ANC - Early Pregnancy Scan - LH'; cost = 200; table = 'service' }
    @{ name = 'ANC - Early Pregnancy Scan - MR'; cost = 1300; table = 'service' }
    @{ name = 'ANC - NT Scan'; cost = 1000; table = 'service' }
    @{ name = 'ANC - Twins NT Scan'; cost = 1800; table = 'service' }
    @{ name = 'ANC - NT Scan - LH'; cost = 200; table = 'service' }
    @{ name = 'ANC - NT Scan - MR'; cost = 1300; table = 'service' }
    @{ name = 'ANC - Anomaly Scan'; cost = 1400; table = 'service' }
    @{ name = 'ANC - Fetal ECHO'; cost = 1700; table = 'service' }
    @{ name = 'ANC - Complex Fetal ECHO (Dr.Balaganesh)'; cost = 4200; table = 'service' }
    @{ name = 'ANC - Growth Scan'; cost = 1000; table = 'service' }
    @{ name = 'ANC - Growth Scan - LH'; cost = 200; table = 'service' }
    @{ name = 'ANC - Growth Scan - MR'; cost = 1300; table = 'service' }
    @{ name = 'ANC - Growth Scan + Doppler'; cost = 1200; table = 'service' }
    @{ name = 'ANC - Growth Scan + Doppler - LH'; cost = 200; table = 'service' }
    @{ name = 'ANC - Twins Growth Scan'; cost = 1800; table = 'service' }
    @{ name = 'ANC - Twins Growth + Doppler Scan'; cost = 2200; table = 'service' }
    @{ name = 'ANC - Liquor and FH - LH'; cost = 500; table = 'service' }
    @{ name = 'ANC - Liquor and FH - MR'; cost = 500; table = 'service' }
    @{ name = 'ANC - 4D Scan'; cost = 1700; table = 'service' }
    @{ name = 'USG for Retained Products - LH'; cost = 500; table = 'service' }
    @{ name = 'USG for Retained Products - MR'; cost = 500; table = 'service' }
    @{ name = 'Follicular Study - LH'; cost = 500; table = 'service' }
    @{ name = 'Folliculimetry + Doppler - LH'; cost = 750; table = 'service' }
    @{ name = 'Transvaginal Scan (TVS) - LH'; cost = 200; table = 'service' }
    @{ name = 'Transvaginal Scan (TVS) - MR'; cost = 1300; table = 'service' }
    @{ name = 'Cervical Assessment - LH'; cost = 500; table = 'service' }
    @{ name = 'Cervical Assessment - MR'; cost = 500; table = 'service' }
    @{ name = 'TAS - Pelvis Scan- LH'; cost = 200; table = 'service' }
    @{ name = 'TAS - Pelvis Scan- MR'; cost = 1300; table = 'service' }
    @{ name = 'FS + Pelvis Scan- LH'; cost = 200; table = 'service' }
    @{ name = 'FS + Pelvis Scan- MR'; cost = 1500; table = 'service' }
    @{ name = '4D Gynaec Scan'; cost = 1700; table = 'service' }
    @{ name = 'Head and Cranium (Neonatal/Paediatric/Adult)'; cost = 1200; table = 'service' }
    @{ name = 'Thyroid / Neck'; cost = 1200; table = 'service' }
    @{ name = 'Breast'; cost = 1200; table = 'service' }
    @{ name = 'Chest'; cost = 1200; table = 'service' }
    @{ name = 'Scrotum + Doppler'; cost = 1200; table = 'service' }
    @{ name = 'Joints (Shoulder/Elbow/Wrist/Hip/Knee/Ankle) - Single'; cost = 1200; table = 'service' }
    @{ name = 'Joints - Double'; cost = 1700; table = 'service' }
    @{ name = 'Limbs (Arm/Forearm/Hand/Thigh/Leg/Foot) - Single'; cost = 1200; table = 'service' }
    @{ name = 'Limbs - Double'; cost = 1700; table = 'service' }
    @{ name = 'Back'; cost = 1200; table = 'service' }
    @{ name = 'Swellings'; cost = 900; table = 'service' }
    @{ name = 'Single Limb Doppler - Arterial / Venous'; cost = 1200; table = 'service' }
    @{ name = 'Single Limb Doppler - Arterial and Venous'; cost = 2200; table = 'service' }
    @{ name = 'Double Limbs Doppler - Arterial / Venous'; cost = 2200; table = 'service' }
    @{ name = 'Double Limbs Doppler - Arterial and Venous'; cost = 4200; table = 'service' }
    @{ name = 'Dressing - Minor'; cost = 150; table = 'test' }
    @{ name = 'Dressing - Major'; cost = 350; table = 'test' }
    @{ name = 'Suture Removal with Dressing'; cost = 150; table = 'test' }
    @{ name = 'Suturing with Dressing - Minor'; cost = 350; table = 'test' }
    @{ name = 'Suturing with Dressing - Major'; cost = 700; table = 'test' }
    @{ name = 'Debridement with Dressing - Minor'; cost = 350; table = 'test' }
    @{ name = 'Debridement with Dressing - Major'; cost = 700; table = 'test' }
    @{ name = 'I & D with Dressing - Minor'; cost = 350; table = 'test' }
    @{ name = 'I & D with Dressing - Major'; cost = 700; table = 'test' }
    @{ name = 'Joint / Swelling Aspiration with Dressing - Minor'; cost = 350; table = 'test' }
    @{ name = 'Joint / Swelling Aspiration with Dressing - Major'; cost = 700; table = 'test' }
    @{ name = 'POP Application - Minor'; cost = 1100; table = 'test' }
    @{ name = 'POP Application - Major'; cost = 1700; table = 'test' }
    @{ name = 'POP Application - Multiple'; cost = 2000; table = 'test' }
    @{ name = 'POP Removal - Minor'; cost = 150; table = 'test' }
    @{ name = 'POP Removal - Major'; cost = 350; table = 'test' }
    @{ name = 'K-wire(s) / Implant Removal - Minor'; cost = 350; table = 'test' }
    @{ name = 'K-wire(s) / Implant Removal - Major'; cost = 700; table = 'test' }
    @{ name = 'Dislocation Reduction - Minor'; cost = 700; table = 'test' }
    @{ name = 'Dislocation Reduction - Major'; cost = 1100; table = 'test' }
    @{ name = 'Biopsy with Dressing - Minor'; cost = 1100; table = 'test' }
    @{ name = 'Biopsy with Dressing - Major'; cost = 1700; table = 'test' }
    @{ name = 'Cervical Cytology / Swab (PAP Smear/HPV/Hi-Vaginal Swab)'; cost = 1100; table = 'test' }
    @{ name = 'Pipelle Sampling'; cost = 2600; table = 'test' }
    @{ name = 'Pipelle Sampling + Endometrial / Cervical Biopsy'; cost = 5100; table = 'test' }
    @{ name = 'Pipelle Sampling + Endometrial Biopsy + Cervical Biopsy'; cost = 5600; table = 'test' }
    @{ name = 'Copper-T Insertion'; cost = 2600; table = 'test' }
    @{ name = 'Copper-T Removal'; cost = 600; table = 'test' }
    @{ name = 'MIRENA Insertion + Endometrial Biopsy'; cost = 3600; table = 'test' }
    @{ name = 'Removal of Misplaced IUCD'; cost = 1100; table = 'test' }
    @{ name = 'Cervical Encirclage Removal'; cost = 100; table = 'test' }
    @{ name = 'Pessary Insertion'; cost = 1100; table = 'test' }
    @{ name = 'Pessary Removal'; cost = 600; table = 'test' }
    @{ name = 'Removal of Vaginal / Cervical Foreign body'; cost = 1100; table = 'test' }
    @{ name = 'Cervical Polypectomy'; cost = 2600; table = 'test' }
    @{ name = 'Wart Excision'; cost = 1100; table = 'test' }
    @{ name = 'Wart Excision with Suturing'; cost = 2100; table = 'test' }
    @{ name = 'Foley''s Induction'; cost = 100; table = 'test' }
    @{ name = 'Check Curettage'; cost = 5100; table = 'test' }
    @{ name = 'Colposcopic Biopsy'; cost = 6100; table = 'test' }
    @{ name = 'Intra-Uterine Insemination (IUI)'; cost = 6100; table = 'test' }
    @{ name = 'Double IUI'; cost = 10100; table = 'test' }
    @{ name = 'Medical Termination of Pregnancy (MTP)'; cost = 3000; table = 'test' }
    @{ name = 'Medical Termination of Pregnancy (MTP) for Residual Products'; cost = 500; table = 'test' }
    @{ name = 'Hysterosalphingogram (HSG)'; cost = 1100; table = 'test' }
    @{ name = 'NST / CTG'; cost = 1100; table = 'test' }
    @{ name = 'USG Guided Nerve Blocks / Injections / Aspirations'; cost = 300; table = 'test' }
    @{ name = 'Ascitic Fluid Tapping'; cost = 300; table = 'test' }
    @{ name = 'Pleural Tapping'; cost = 300; table = 'test' }
    @{ name = 'Lumbar Puncture - CSF Analysis'; cost = 300; table = 'test' }
    @{ name = 'Nebulization'; cost = 200; table = 'test' }
    @{ name = 'Ryle''s Tube Insertion/Aspiration'; cost = 200; table = 'test' }
    @{ name = 'Intubation'; cost = 300; table = 'test' }
    @{ name = 'Injections - IM / SC / ID'; cost = 100; table = 'test' }
    @{ name = 'Injections - Direct IV'; cost = 200; table = 'test' }
    @{ name = 'Injections - IV as Drip'; cost = 300; table = 'test' }
    @{ name = 'Injections - Intra-articular / Specific Sites'; cost = 1100; table = 'test' }
    @{ name = 'Blood Transfusion'; cost = 300; table = 'test' }
    @{ name = 'General Blue File'; cost = 27; table = 'test' }
    @{ name = 'ANC File'; cost = 19; table = 'test' }
    @{ name = 'Neonatal File'; cost = 8; table = 'test' }
    @{ name = 'Green File'; cost = 14.5; table = 'test' }
)

function Normalize-Name {
    param([string]$Name)
    if (-not $Name) { return '' }
    return (($Name -replace '\s+', ' ').Trim().ToLowerInvariant())
}

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

Write-Host "Fetching current diagnostic services and legacy tests..." -ForegroundColor Cyan
$services = Get-AllPaged -Entity 'diagnostic-services'
$legacyTests = Get-AllPaged -Entity 'diagnostic-tests'

$servicesByName = @{}
foreach ($svc in $services) {
    $servicesByName[(Normalize-Name $svc.name)] = $svc
}
$testsByName = @{}
foreach ($t in $legacyTests) {
    $testsByName[(Normalize-Name $t.name)] = $t
}

Write-Host "Loaded $($services.Count) diagnostic services and $($legacyTests.Count) legacy diagnostic tests.`n" -ForegroundColor DarkGray

$updated = 0
$skippedAlready = 0
$notFound = [System.Collections.Generic.List[string]]::new()
$errors = [System.Collections.Generic.List[string]]::new()

foreach ($entry in $costUpdates) {
    $key = Normalize-Name $entry.name

    if ($entry.table -eq 'test') {
        $record = $testsByName[$key]
        if (-not $record) {
            Write-Host "  NOT FOUND (legacy test): '$($entry.name)'" -ForegroundColor Red
            $notFound.Add($entry.name)
            continue
        }
        if ([decimal]$record.costPrice -eq [decimal]$entry.cost) {
            Write-Host "  '$($entry.name)' already at Rs.$($entry.cost) - skipping." -ForegroundColor DarkGray
            $skippedAlready++
            continue
        }
        $body = @{
            name = $record.name; serviceType = $record.serviceType; category = $record.category
            price = $record.price; costPrice = $entry.cost; isOutsourced = $record.isOutsourced
            referenceLab = $record.referenceLab; isActive = $record.isActive
        } | ConvertTo-Json
        $uri = "$ApiBaseUrl/api/v1/masters/diagnostic-tests/$($record.id)"
    }
    else {
        $record = $servicesByName[$key]
        if (-not $record) {
            Write-Host "  NOT FOUND (service): '$($entry.name)'" -ForegroundColor Red
            $notFound.Add($entry.name)
            continue
        }
        if ([decimal]$record.costPrice -eq [decimal]$entry.cost) {
            Write-Host "  '$($entry.name)' already at Rs.$($entry.cost) - skipping." -ForegroundColor DarkGray
            $skippedAlready++
            continue
        }
        $body = @{
            code = $record.code; name = $record.name; categoryId = $record.categoryId; serviceType = $record.serviceType
            isOutsourced = $record.isOutsourced; providerId = $record.providerId; price = $record.price
            costPrice = $entry.cost; isActive = $record.isActive
        } | ConvertTo-Json
        $uri = "$ApiBaseUrl/api/v1/masters/diagnostic-services/$($record.id)"
    }

    if ($WhatIf) {
        Write-Host "  [WhatIf] '$($entry.name)': Rs.$($record.costPrice) -> Rs.$($entry.cost)" -ForegroundColor Cyan
        $updated++
        continue
    }

    try {
        Invoke-WithRetry -Method Put -Uri $uri -Body $body -Label "cost update $($entry.name)" | Out-Null
        Write-Host "  '$($entry.name)': Rs.$($record.costPrice) -> Rs.$($entry.cost)" -ForegroundColor Green
        $updated++
    }
    catch {
        Write-Host "  ERROR updating '$($entry.name)': $($_.Exception.Message)" -ForegroundColor Red
        $errors.Add($entry.name)
    }
    Start-Sleep -Milliseconds 350
}

Write-Host "`n---" -ForegroundColor Cyan
Write-Host "Updated:        $updated" -ForegroundColor Green
Write-Host "Already correct: $skippedAlready" -ForegroundColor DarkGray
Write-Host "Not found:      $($notFound.Count)" -ForegroundColor $(if ($notFound.Count -gt 0) { 'Red' } else { 'DarkGray' })
foreach ($n in $notFound) { Write-Host "  - $n" -ForegroundColor Red }
Write-Host "Errors:         $($errors.Count)" -ForegroundColor $(if ($errors.Count -gt 0) { 'Red' } else { 'DarkGray' })
foreach ($n in $errors) { Write-Host "  - $n" -ForegroundColor Red }
Write-Host "`nDone." -ForegroundColor Cyan
