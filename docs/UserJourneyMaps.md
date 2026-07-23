# User Journey Maps — Hospital Management System

## Purpose
This document defines end-to-end UX journey maps for the nine primary personas of the HMS, built on [InformationArchitecture.md](InformationArchitecture.md)'s module tree and the workflows/gaps identified in [BusinessRequirementsAnalysis.md](BusinessRequirementsAnalysis.md), so behavior expectations (actions, system responses, errors, success states) are agreed before UI design work begins.

## Scope
Covers journey maps for: Receptionist, Doctor/Consultant, Nurse, Lab Technician, Radiologist, Pharmacist, HR Officer, Accounts/Finance Officer, and Hospital Administrator. Each includes goals, pain points, a staged journey (actions, screens visited, system responses, decision points, errors), and success states.

**Out of scope:** visual/UI design (screens, layouts, components). Journeys that touch modules marked `(proposed)` in the IA are designed to enterprise HMIS convention but remain flagged for validation, not treated as confirmed specs.

## When to Update This Document
- Whenever a `(proposed)` module referenced in a journey gets a confirmed functional spec.
- Whenever a persona's workflow changes due to new requirements or IA changes.
- Whenever a new persona/role is introduced to the system.

## Recommended Sections
- One section per persona: Goals, Pain Points, Journey table (Stage / Actions / Screens Visited / System Response / Decision Point / Possible Errors), Success State.

---

## 1. Receptionist

**Goals:** Register patients fast and accurately; avoid duplicate UHIDs; route patients to the correct queue; collect correct payment at point of service.

**Pain Points:** No visibility into whether a "new" patient already exists elsewhere in the system (duplicate-UHID risk); MLC/NMLC and referral fields add friction during emergencies; billing is split across four near-duplicate blocks instead of one; long forms slow down high-volume walk-in periods.

| Stage | Actions | Screens Visited (IA ref) | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Patient arrives | Ask name/phone, search existing record | Patient Management → Patient Enquiry | Returns matches ranked by name/UHID/phone | New vs. existing patient? | Search returns near-duplicate matches — must confirm before creating new UHID |
| Registration | Enter demographics, address, contacts, allergy details, mode of arrival | Patient Management → Reception & Registration → New/Old Patient | Validates mandatory fields inline; auto-generates UHID | OP / IP / Emergency / Day-care? | Missing mandatory field (allergy status, contact) blocks save |
| Encounter setup | Select department, consultant, admission type (MLC/NMLC), category | Registration Details panel | Auto-generates OP No / IP No | MLC (legal reporting required) vs NMLC? | MLC selected without referral/contact details — validation error |
| Document capture | Upload patient photo, ID proof | Document Upload panel | Confirms file type/size accepted | — | Upload fails (unsupported format, file too large) |
| Billing | Select service lines, apply discount, confirm payment | → Billing (contextual, Finance & Billing) | Generates invoice, prints receipt | Cash / card / insurance? | Payment gateway timeout; discount exceeds authorized limit (routes to Accounts approval) |
| Handoff | Route patient to OPD queue / IPD admission | Cross-module anchor to Clinical Care | Patient appears in Consultant's queue in real time | — | Queue doesn't update (sync delay) — patient waits without visibility |

**Success State:** Patient has a confirmed UHID, correct encounter type, uploaded documents, a settled or recorded-pending invoice, and appears in the receiving module's queue within seconds.

---

## 2. Doctor / Consultant

**Goals:** Review complete patient history quickly; document consultations accurately; prescribe and order tests without duplicate data entry; safely escalate to IPD/OT when needed.

**Pain Points:** Patient history fragmented across visits with no single timeline; no drug-allergy interaction alerting at prescribing time; unclear whether the four "Consult" sub-actions (form, prescribe, order, upload) save as one transaction or independently.

| Stage | Actions | Screens Visited | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Login & queue view | Open OPD Consultant View | Clinical Care → OPD → Consultant View → Patient Queue | Shows today's queue ordered by appointment time | — | Queue stale if Registration sync lags |
| Patient review | Open Patient 360 (history, allergies, prior visits, results) | Patient Management → Patient Enquiry → Patient 360 | Aggregates encounters, labs, radiology, prescriptions | — | Missing historical data if cross-module link broken |
| Consultation | Complete OP Consultation Form | Consult Workspace → OP Consultation Form | Autosaves draft | Diagnosis needs lab/imaging? Admit? Discharge? | Note left incomplete — cannot close encounter |
| Prescribing | Select drugs, dosage | Consult Workspace → Prescribe Drugs | Checks against recorded allergy; sends to Pharmacy queue | Override allergy conflict (with justification)? | Allergy conflict blocks silent save — requires explicit override |
| Ordering | Select lab/radiology tests | Consult Workspace → Order Investigations | Sends order to Diagnostics & Ancillary queue | Urgent vs routine priority? | Order sent to wrong department (misconfigured test-to-department mapping) |
| Referral/Admission | Refer to IPD or OT if required | Cross-module anchor to Clinical Care → IPD/OT | Creates admission/OT-booking request | Admit now or schedule? | Bed/OT slot unavailable — request queued pending capacity |
| Close encounter | Finalize and move to next patient | Consult Workspace | Locks record, timestamps consultation | — | System lag during peak hours delays queue advance |

**Success State:** Consultation documented, prescription routed to Pharmacy, investigation orders routed to Lab/Radiology, referral/admission created if needed, and the queue advances to the next patient.

---

## 3. Nurse

**Goals:** Maintain accurate ward records; execute doctor's orders correctly and on time; escalate abnormal readings immediately; support smooth admission/discharge.

**Pain Points:** No centralized care-plan view distinct from doctor's orders; medication administration currently has no cross-check against an active order; discharge readiness isn't tracked as a checklist.

| Stage | Actions | Screens Visited | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Shift start | View assigned ward/bed list | Clinical Care → IPD → Bed/Ward Management | Shows patients assigned to nurse's ward | — | Bed list out of sync with latest admission/transfer |
| Order review | Review doctor's orders and care plan | Clinical Care → IPD → Doctor Rounds (read-only) | Displays active orders per patient | — | Orders not yet visible if doctor hasn't finalized rounds entry |
| Vitals & charting | Record vitals, nursing notes | Clinical Care → IPD → Nursing Charting | Validates against physiological range | Reading normal or critical? | Out-of-range value — system prompts confirm-or-escalate |
| Medication administration | Administer per Pharmacy-dispensed order | Nursing Charting → Medication Administration | Cross-checks against active order and dispense record | Proceed, hold, or flag contraindication? | Attempted administration without matching active order is blocked |
| Escalation | Acknowledge and escalate critical reading | Notification Center | Routes critical alert to attending doctor | — | Alert unacknowledged past SLA — auto-escalates to Hospital Administrator |
| Discharge support | Complete discharge checklist items | Clinical Care → IPD → Admission/Transfer/Discharge | Marks checklist item complete; blocks discharge until all items done | Patient ready for discharge? | Discharge attempted with incomplete checklist — blocked with itemized list |

**Success State:** Vitals and notes logged on schedule, medications administered against valid orders with no conflicts, critical readings acknowledged by a doctor, and discharge checklist fully completed before patient leaves the ward.

---

## 4. Lab Technician

**Goals:** Process test orders without error; track samples reliably; enter results promptly; flag critical values without delay.

**Pain Points:** No barcode/positive sample-ID tracking specified; manual order intake risks mismatched samples; critical-value communication depends entirely on the notification engine being correctly configured.

| Stage | Actions | Screens Visited | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Order intake | View incoming test orders | Diagnostics & Ancillary → Central Laboratory → Test Order Queue | Shows orders grouped by priority (urgent/routine) | Accept or query the order? | Order missing required clinical detail — must query ordering consultant before accepting |
| Sample handling | Receive and log sample | Sample Collection/Tracking | Assigns/verifies sample ID against order | Sample acceptable or reject? | Sample rejected (hemolyzed, insufficient volume) — order bounces back to ordering clinician with reason |
| Testing | Perform test, enter raw results | Results Entry/Release | Validates entered values against plausible physiological range | — | Entry rejected (value outside possible range — likely transcription error) |
| Critical value check | Compare result to critical thresholds | Results Entry/Release | Auto-flags critical values | Critical — trigger immediate alert? | Critical flag not raised if threshold table is misconfigured |
| Release | Release final report | Results Entry/Release | Publishes to ordering consultant's Patient 360; notifies if critical | — | Report released to wrong encounter (duplicate patient search match) |

**Success State:** Sample tracked end-to-end without mislabeling, result validated and released to the correct patient record, and any critical value triggers an immediate notification to the ordering consultant.

---

## 5. Radiologist

**Goals:** Prioritize and interpret studies efficiently; produce timely, accurate reports; flag urgent findings without delay.

**Pain Points:** No PACS/RIS integration defined in current scope (manual image reference only); no formal urgent-finding escalation path; report turnaround has no SLA tracking.

| Stage | Actions | Screens Visited | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Worklist review | View pending studies | Diagnostics & Ancillary → Radiology → Modality Worklist | Lists studies by order priority and time | Which study next — urgent first? | Worklist doesn't reflect true urgency if priority wasn't set at order time |
| Image review | Open study images | Report Entry → Image Reference | Displays linked images (external viewer/PACS if integrated) | — | Broken image link — study exists but no viewable image attached |
| Reporting | Dictate/enter findings | Report Entry | Autosaves draft report | Normal / abnormal / needs repeat imaging? | Report saved as draft but never released — sits invisible to consultant |
| Urgent finding | Flag critical/urgent finding | Report Entry | Triggers immediate notification to ordering consultant | Escalate now vs. include in routine report? | Urgent flag missed if not explicitly selected — no automatic detection from free-text |
| Release | Finalize and release report | Report Entry | Marks study complete in worklist | — | Patient/study ID mismatch — report released against wrong encounter |

**Success State:** Study reviewed within SLA, report finalized and released, urgent findings immediately routed to the ordering consultant, and the worklist correctly reflects completion.

---

## 6. Pharmacist

**Goals:** Dispense prescriptions accurately; catch allergy/interaction conflicts before dispensing; keep stock and expiry data current.

**Pain Points:** No real-time sync with central Hospital Inventory specified; expired/near-expiry batches aren't proactively flagged; prescription-to-stock matching is manual.

| Stage | Actions | Screens Visited | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Queue review | View incoming prescriptions | Pharmacy → Prescription Fulfillment Queue | Lists prescriptions by encounter/priority | — | Prescription arrives without full dosage detail — must query prescriber |
| Safety check | Verify against patient allergy/history | Prescription Fulfillment Queue → Patient 360 (cross-link) | Displays allergy flags already set at registration/consult | Dispense as-is or flag conflict back to doctor? | Allergy conflict blocks dispensing until doctor overrides or amends |
| Stock check | Check availability, batch, expiry | Drug Master → Stock/Batch/Expiry | Shows available stock, nearest-expiry batch first | Full dispense, partial, or substitute? | Expired batch blocked from selection automatically |
| Dispensing | Dispense and record | Prescription Fulfillment Queue | Decrements stock; generates billing line | — | Stock count goes negative — indicates unreconciled inventory, blocks dispense |
| Handoff | Confirm dispense to ward (IPD) or patient (OPD) | Cross-module anchor to Clinical Care / Patient Management | Updates prescription status to "Dispensed" | — | Nurse administration screen doesn't reflect dispense in time (sync delay) |

**Success State:** Prescription dispensed with no unresolved allergy conflict, stock accurately decremented, billing line generated, and the prescribing consultant/nurse notified of fulfillment.

---

## 7. HR Officer

**Goals:** Keep staff records and rosters accurate; process leave fairly against staffing needs; track license/credential expiry proactively.

**Pain Points:** No automated license-expiry alerting specified; roster conflicts currently caught manually; leave approval isn't linked to minimum-staffing rules.

| Stage | Actions | Screens Visited | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Staff record maintenance | Update staff/consultant profile, credentials | Workforce & Administration → HR → Staff Directory | Confirms save; recalculates license expiry countdown | — | Duplicate staff entry created if search-before-create step is skipped |
| Roster planning | Build shift roster | HR → Roster/Shift Assignment | Flags double-booked staff or coverage gaps | Assign replacement or accept gap? | Roster conflict (same staff double-booked) blocks publish |
| Leave management | Review and decide on leave requests | HR → Leave Management | Checks requested dates against roster coverage | Approve, reject, or request alternate dates? | Approval would breach minimum staffing — system warns before confirming |
| Credentialing | Monitor license/registration expiry | HR → Credentialing | Surfaces expiry alerts at defined lead time | Renew on file or restrict from clinical duty? | License expired with no renewal on file — compliance flag raised, should block assignment |
| Reporting | Confirm present-staff count feeds Dashboard | Home/Dashboard (cross-link) | "Present HR" widget updates from roster + attendance | — | Widget shows stale count if roster wasn't published in time |

**Success State:** Roster published with no conflicts, leave decisions respect staffing minimums, no staff member works with an expired credential, and the Dashboard's HR widget reflects current, accurate staffing.

---

## 8. Accounts / Finance Officer

**Goals:** Reconcile all billing sources into one ledger; process payments/refunds correctly; manage insurance/TPA claims; produce accurate financial reports.

**Pain Points:** Source brief originally described four separate billing blocks (OP/Radiology/Lab/Procedure) with inconsistent naming — reconciliation risk if not unified; no insurance/TPA workflow defined; discount approvals currently informal.

| Stage | Actions | Screens Visited | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Daily reconciliation | Review unified invoice ledger | Finance & Billing → Accounts and Finance → Unified Invoice Ledger | Aggregates billing lines from Registration, OPD, Lab, Radiology, Procedures | — | Orphaned charge with no linked department/consultant — flagged for investigation |
| Discount approval | Review escalated discount requests | Unified Invoice Ledger → Approval queue | Shows requested discount vs. authorized limit | Approve, reject, or counter-offer? | Approval request exceeds Accounts' own authorization — escalates further to Administrator |
| Payment processing | Process payment or refund | Payments & Refunds | Confirms transaction; updates invoice status | Refund exceeds original payment? | Refund-exceeds-payment validation error blocks the transaction |
| Insurance/TPA | Submit and track claims | Insurance/TPA (proposed) | Tracks claim status (submitted/pending/approved/rejected) | Resubmit rejected claim with correction? | Claim rejected — requires correction before resubmission |
| Reporting | Generate financial reports | General Ledger/Financial Reports | Compiles income/expense by department | — | Report totals don't match ledger (indicates an unreconciled entry upstream) |

**Success State:** All billing lines reconciled with zero orphaned charges, payments and refunds processed correctly, claims submitted and tracked, and financial reports match the ledger exactly.

---

## 9. Hospital Administrator

**Goals:** Maintain hospital-wide operational oversight; act quickly on KPI anomalies and critical escalations; manage system configuration and compliance safely.

**Pain Points:** No single pane of glass existed before the Dashboard/IA consolidation; critical incidents (MLC, blood bank shortage, compliance flags) previously had no guaranteed escalation path; permission changes carry blast-radius risk.

| Stage | Actions | Screens Visited | System Response | Decision Point | Possible Errors |
|---|---|---|---|---|---|
| Daily oversight | Review executive KPIs | Home/Dashboard | Shows census, income/expense, HR presence, plans/projects status | — | KPI data stale if a source module's sync lags |
| Anomaly investigation | Drill into a flagged KPI | Dashboard widget → cross-module anchor (e.g., low census → Patient Enquiry filtered view) | Opens filtered source-module view | Root cause operational or data issue? | Drill-down lands on unfiltered view if anchor misconfigured |
| Escalation handling | Review and resolve critical notifications | Notification Center | Shows unresolved critical/compliance alerts | Resolve directly, delegate, or escalate externally (e.g., MLC to authorities)? | Alert unacknowledged past SLA triggers auto-escalation and audit flag |
| Configuration | Manage roles, permissions, master data | Workforce & Administration → Settings | Applies change; logs to Activity Log | Confirm — change affects other users' access? | Permission change inadvertently revokes access for an active role — requires confirmation step before applying |
| Compliance reporting | Generate reports for board/regulators | Reports & Analytics | Compiles statutory/regulatory report set | — | Report omits a module not yet reporting data (proposed modules still pending) |

**Success State:** KPI anomalies are investigated and actioned same-day, all critical/compliance notifications are resolved and logged in Activity Log, configuration changes are applied without unintended access loss, and reports are accurate and complete for governance review.
