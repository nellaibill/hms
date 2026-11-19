-- =====================================================================================
-- LHS — radiology tariff split, bedside add-on and LH/MR rate options (ADR-140), 2026-11-19
--
-- Run against the LHS hospital database (e.g. hms_lhs) AFTER deploying the ADR-140 build and
-- running Migrate (Masters + Billing) — it needs the new doctor_charge / bedside_* columns and
-- the diagnostic_service_rate_options table.
--
-- 1. Split each radiology service's Running Cost (today doctor's share + running cost added
--    together) into Doctor's share and Running cost, from the tariff sheet. Only a service whose
--    price and combined cost still match the sheet is touched, so a figure changed by hand since
--    is left alone, and re-running changes nothing.
-- 2. Ultrasound scans and Doppler (the sheet's "All Scans and Doppler - Bedside" row) get the
--    bedside add-on: +Rs 500 to the price, +Rs 200 to the doctor's share.
-- 3. Where a scan has a standard service and "- LH" / "- MR" siblings, the siblings become rate
--    options on the standard service and are deactivated (old bills that used them still show).
--    LH/MR services with no standard sibling (e.g. Transvaginal Scan (TVS) - LH / - MR) stay as
--    they are.
--
-- Profit in reports doesn't change: a line's cost is still doctor's share + running cost.
-- The whole script is one transaction; the SELECT at the end lists the result before COMMIT.
-- =====================================================================================

BEGIN;

CREATE TEMP TABLE sheet_radiology (name text, price numeric(10,2), doctor numeric(10,2), running numeric(10,2)) ON COMMIT DROP;
INSERT INTO sheet_radiology VALUES
        ('Digital X-ray', 300.00, 0.00, 100.00),
        ('Digital X-ray Large films for Femur/Tibia/Humerus', 500.00, 0.00, 150.00),
        ('Digital X-ray Long Leg View Scannogram', 800.00, 0.00, 200.00),
        ('Hysterosalphingogram (HSG)', 1000.00, 0.00, 300.00),
        ('ECG', 150.00, 0.00, 50.00),
        ('ECHO (Adult)', 2000.00, 700.00, 200.00),
        ('Neonatal / Paediatric / Complex Fetal ECHO', 5000.00, 4000.00, 200.00),
        ('Abdomen and Pelvis (Adult/Neonatal/Paediatric)', 2000.00, 700.00, 200.00),
        ('ANC - Early Pregnancy Scan', 2000.00, 700.00, 200.00),
        ('ANC - Early Pregnancy Scan - LH', 1300.00, 0.00, 200.00),
        ('ANC - Early Pregnancy Scan - MR', 1300.00, 1300.00, 0.00),
        ('ANC - NT Scan', 2200.00, 800.00, 200.00),
        ('ANC - Twins NT Scan', 3000.00, 1600.00, 200.00),
        ('ANC - NT Scan - LH', 1300.00, 0.00, 200.00),
        ('ANC - NT Scan - MR', 1300.00, 1300.00, 0.00),
        ('ANC - Anomaly Scan', 3000.00, 1200.00, 200.00),
        ('ANC - Fetal ECHO', 3500.00, 1500.00, 200.00),
        ('ANC - Complex Fetal ECHO (Dr.Balaganesh)', 5000.00, 4000.00, 200.00),
        ('ANC - Growth Scan', 2200.00, 800.00, 200.00),
        ('ANC - Growth Scan - LH', 1300.00, 0.00, 200.00),
        ('ANC - Growth Scan - MR', 1300.00, 1300.00, 0.00),
        ('ANC - Growth Scan + Doppler', 2500.00, 1000.00, 200.00),
        ('ANC - Growth Scan + Doppler - LH', 1800.00, 0.00, 200.00),
        ('ANC - Twins Growth Scan', 3200.00, 1600.00, 200.00),
        ('ANC - Twins Growth + Doppler Scan', 4000.00, 2000.00, 200.00),
        ('ANC - Liquor and FH - LH', 500.00, 500.00, 0.00),
        ('ANC - Liquor and FH - MR', 500.00, 500.00, 0.00),
        ('ANC - 4D Scan', 3000.00, 1500.00, 200.00),
        ('USG for Retained Products - LH', 500.00, 500.00, 0.00),
        ('USG for Retained Products - MR', 500.00, 500.00, 0.00),
        ('Follicular Study - LH', 500.00, 500.00, 0.00),
        ('Folliculimetry + Doppler - LH', 750.00, 750.00, 0.00),
        ('Transvaginal Scan (TVS) - LH', 1300.00, 0.00, 200.00),
        ('Transvaginal Scan (TVS) - MR', 1300.00, 1300.00, 0.00),
        ('Cervical Assessment - LH', 500.00, 500.00, 0.00),
        ('Cervical Assessment - MR', 500.00, 500.00, 0.00),
        ('TAS - Pelvis Scan- LH', 1300.00, 0.00, 200.00),
        ('TAS - Pelvis Scan- MR', 1300.00, 1300.00, 0.00),
        ('FS + Pelvis Scan- LH', 1500.00, 0.00, 200.00),
        ('FS + Pelvis Scan- MR', 1500.00, 1500.00, 0.00),
        ('4D Gynaec Scan', 3000.00, 1500.00, 200.00),
        ('Head and Cranium (Neonatal/Paediatric/Adult)', 2000.00, 1000.00, 200.00),
        ('Thyroid / Neck', 2000.00, 1000.00, 200.00),
        ('Breast', 2000.00, 1000.00, 200.00),
        ('Chest', 2000.00, 1000.00, 200.00),
        ('Scrotum + Doppler', 2000.00, 1000.00, 200.00),
        ('Joints (Shoulder/Elbow/Wrist/Hip/Knee/Ankle) - Single', 2000.00, 1000.00, 200.00),
        ('Joints - Double', 2500.00, 1500.00, 200.00),
        ('Limbs (Arm/Forearm/Hand/Thigh/Leg/Foot) - Single', 2000.00, 1000.00, 200.00),
        ('Limbs - Double', 2500.00, 1500.00, 200.00),
        ('Back', 2000.00, 1000.00, 200.00),
        ('Swellings', 1500.00, 700.00, 200.00),
        ('Single Limb Doppler - Arterial / Venous', 2000.00, 1000.00, 200.00),
        ('Single Limb Doppler - Arterial and Venous', 3000.00, 2000.00, 200.00),
        ('Double Limbs Doppler - Arterial / Venous', 4000.00, 2000.00, 200.00),
        ('Double Limbs Doppler - Arterial and Venous', 6000.00, 4000.00, 200.00);

-- 1. Doctor's share / running cost split
UPDATE masters.diagnostic_services s
SET doctor_charge = r.doctor,
    cost_price    = r.running,
    updated_at    = now()
FROM sheet_radiology r
WHERE s.service_type = 'Radiology'
  AND s.is_deleted = false
  AND lower(regexp_replace(btrim(s.name), '\s*-\s*', '-', 'g')) = lower(regexp_replace(r.name, '\s*-\s*', '-', 'g'))
  AND s.price = r.price
  AND s.doctor_charge = 0
  AND s.cost_price = r.doctor + r.running
  AND r.doctor > 0;

-- 2. Bedside add-on for ultrasound scans and Doppler
UPDATE masters.diagnostic_services s
SET bedside_price = 500.00,
    bedside_doctor_charge = 200.00,
    updated_at = now()
FROM masters.diagnostic_categories c
WHERE c.id = s.category_id
  AND lower(btrim(c.name)) = 'ultrasound'
  AND s.service_type = 'Radiology'
  AND s.is_deleted = false
  AND s.bedside_price IS NULL;

-- 3. LH / MR siblings become rate options on the standard scan
CREATE TEMP TABLE sheet_rate_merge (base_name text, sibling_name text, option_name text, sort_order int) ON COMMIT DROP;
INSERT INTO sheet_rate_merge VALUES
        ('ANC - Early Pregnancy Scan', 'ANC - Early Pregnancy Scan - LH', 'LH', 0),
        ('ANC - Early Pregnancy Scan', 'ANC - Early Pregnancy Scan - MR', 'MR', 1),
        ('ANC - NT Scan', 'ANC - NT Scan - LH', 'LH', 0),
        ('ANC - NT Scan', 'ANC - NT Scan - MR', 'MR', 1),
        ('ANC - Growth Scan', 'ANC - Growth Scan - LH', 'LH', 0),
        ('ANC - Growth Scan', 'ANC - Growth Scan - MR', 'MR', 1),
        ('ANC - Growth Scan + Doppler', 'ANC - Growth Scan + Doppler - LH', 'LH', 0);

INSERT INTO masters.diagnostic_service_rate_options (id, diagnostic_service_id, name, price, doctor_charge, cost_price, sort_order)
SELECT gen_random_uuid(), base.id, m.option_name, sib.price, sib.doctor_charge, sib.cost_price, m.sort_order
FROM sheet_rate_merge m
JOIN masters.diagnostic_services base
  ON base.service_type = 'Radiology' AND base.is_deleted = false
 AND lower(regexp_replace(btrim(base.name), '\s*-\s*', '-', 'g')) = lower(regexp_replace(m.base_name, '\s*-\s*', '-', 'g'))
JOIN masters.diagnostic_services sib
  ON sib.service_type = 'Radiology' AND sib.is_deleted = false
 AND lower(regexp_replace(btrim(sib.name), '\s*-\s*', '-', 'g')) = lower(regexp_replace(m.sibling_name, '\s*-\s*', '-', 'g'))
WHERE NOT EXISTS (
    SELECT 1 FROM masters.diagnostic_service_rate_options o
    WHERE o.diagnostic_service_id = base.id AND lower(o.name) = lower(m.option_name));

UPDATE masters.diagnostic_services sib
SET is_active = false,
    updated_at = now()
FROM sheet_rate_merge m
WHERE sib.service_type = 'Radiology'
  AND sib.is_deleted = false
  AND sib.is_active = true
  AND lower(regexp_replace(btrim(sib.name), '\s*-\s*', '-', 'g')) = lower(regexp_replace(m.sibling_name, '\s*-\s*', '-', 'g'))
  AND EXISTS (
      SELECT 1
      FROM masters.diagnostic_services base
      JOIN masters.diagnostic_service_rate_options o ON o.diagnostic_service_id = base.id AND lower(o.name) = lower(m.option_name)
      WHERE base.is_deleted = false
        AND lower(regexp_replace(btrim(base.name), '\s*-\s*', '-', 'g')) = lower(regexp_replace(m.base_name, '\s*-\s*', '-', 'g')));

-- Result: every radiology service with its split, bedside add-on and rate options.
SELECT s.name,
       s.is_active,
       s.price,
       s.doctor_charge,
       s.cost_price AS running_cost,
       s.price - s.doctor_charge - s.cost_price AS profit,
       s.bedside_price,
       s.bedside_doctor_charge,
       (SELECT string_agg(o.name || ' ' || o.price || ' (dr ' || o.doctor_charge || ', run ' || o.cost_price || ')', ' | ' ORDER BY o.sort_order)
          FROM masters.diagnostic_service_rate_options o WHERE o.diagnostic_service_id = s.id) AS rate_options
FROM masters.diagnostic_services s
WHERE s.service_type = 'Radiology' AND s.is_deleted = false
ORDER BY s.name;

COMMIT;
