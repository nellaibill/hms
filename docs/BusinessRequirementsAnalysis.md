# Business Requirements Analysis — Hospital Management System

## Purpose
This document captures a Business Analyst / Solution Architect review of the source requirement brief ("LH Software" — Lakshmi Hospitals HMIS) received from the client, so the modules, actors, entities, gaps, and risks are on record before any UI or architecture work begins.

## Scope
Covers the analysis of the client-provided sidebar/wireframe brief: business modules, actors, functional breakdown, entities, module dependencies, reusable features, missing requirements, ambiguities, risks, and future scalability recommendations.

**Out of scope:** UI design (intentionally deferred — see Recommended Next Steps), and detailed specs for modules the source brief does not define (tracked here only as gaps, not designed).

## When to Update This Document
- Whenever the client provides an updated or expanded version of the requirement brief.
- Whenever an ambiguity listed here is resolved with the client — move it from "Open Ambiguities" into the relevant module's spec.
- Whenever a module goes from "name only" to specified, update the Business Modules table.

## Recommended Sections
- Executive Summary
- Business Modules
- Actors
- Functional Breakdown (Workflows, Dependencies, Reusable Features)
- Entity List
- Missing Functionalities
- Risk Analysis
- Future Scalability
- Open Ambiguities
- Recommended Next Steps

---

## Source Document
Client file: `LH Software (1).docx` (received 2026-07-23). Describes itself as "LH Website" — a sidebar of 21 modules plus a common top bar, with detailed specification for Dashboard, Patient Enquiry, and Reception & Registration, and partial specification for OPD.

## Executive Summary
The source document is a **navigation-driven wireframe brief**, not a full requirements specification. It defines the application's information architecture (a 21-item sidebar + common top bar) and provides functional depth for exactly one end-to-end workflow: **patient registration → OP consultation initiation → point-of-service billing**. Everything downstream of registration (clinical care delivery, ancillary services, back-office operations) is named but undefined.

This is a strong start on the **patient intake and front-desk domain**, but as an enterprise HMIS spec it is roughly 15–20% complete. It should be treated as the seed for a discovery phase, not a build-ready spec — an Oracle Health / Epic / SAP-grade system requires formal requirement workshops for the remaining 18 modules before UI or architecture work starts.

## Business Modules

| # | Module | Spec depth in source doc |
|---|---|---|
| 1 | Dashboard | Defined (KPIs, charts) |
| 2 | Patient Enquiry | Defined (search) |
| 3 | Reception & Registration | Fully defined |
| 4 | OPD | Partially defined |
| 5 | IPD | Name only |
| 6 | Operation Theatre (OT) | Name only |
| 7 | Pharmacy | Name only |
| 8 | Central Laboratory | Name only |
| 9 | Radiology | Name only |
| 10 | Blood Bank | Name only |
| 11 | Ambulance | Name only |
| 12 | Accounts and Finance | Name only |
| 13 | Records and Certificates | Name only |
| 14 | Human Resource Management | Name only |
| 15 | Activity Log | Name only |
| 16 | Hospital Inventory Management | Name only |
| 17 | Programmes and Calendar | Name only |
| 18 | Messages and Notifications | Name only |
| 19 | Reports | Name only |
| 20 | E-MRD | Name only (meaning ambiguous — see Open Ambiguities) |
| 21 | Settings | Name only |
| — | **Common Services** (cross-cutting) | Patient/Doctor Search, Language, Notifications, Calendar, Calculator, Pending Tasks, User/Login |

## Actors

**Explicitly named in the source doc:**
- Super Admin
- Admin
- Receptionist
- Consultant / Doctor (individual workspace, distinct from admin views)
- Patient (data subject only, not a system user)

**Implied but never named** (required for the modules that exist only as sidebar entries):
- Nurse / Ward staff (IPD)
- OT Team — Surgeon, Anesthetist, OT Nurse, OT Scheduler
- Pharmacist / Store staff
- Lab Technician / Pathologist
- Radiologist / Radiology Technician
- Blood Bank Officer
- Ambulance Dispatcher / Driver
- Accounts/Finance Officer, Cashier
- Medical Records Officer (MRD)
- HR Officer / Payroll Admin
- Inventory/Store Manager
- IT Administrator (Settings/user management)
- Government/regulatory bodies (indirect actor — MLC reporting, statutory certificates)

No role/permission matrix exists in the source doc — only an informal split between "Super-admin/Admin/Receptionist" and "individual consultant" views inside OPD.

## Functional Breakdown

### Core workflow fully specified — Patient Intake
1. Receptionist opens Reception & Registration → chooses New or Old Patient.
2. **New patient**: captures demographics, address, contacts, profession, blood group, emergency contact, allergy details, mode of arrival (referenced as an external form, not detailed inline).
3. Selects encounter type — OP / IP / Emergency / Day-care-Observation — each capturing Department, Consultant, Admission type (MLC/NMLC), Referral source, Category, and an auto-generated OP No/IP No. UHID is auto-generated once, at the patient level.
4. Uploads patient photo + ID proof.
5. Saves record → triggers point-of-service billing (OP/Radiology/Lab/Procedure billing blocks, each with department, consultant, charges, discount, payment confirmation).
6. **Old patient**: search → demographic update → same registration-details block → billing block (labeled "Consultation Billing" instead of "OP Billing" — naming inconsistency, see Open Ambiguities).

### Partially specified — OPD
- Admin/Receptionist view: aggregate lists (patients, consultants, investigations, procedures, admissions).
- Consultant's personal view: consultant profile card, patient queue with appointment slot, and a "Consult" action that branches into: OP Consultation Form, Prescribe Drugs, Order Investigations, Upload consultation/prescription docs. The transactional relationship between these four sub-actions is undefined.

### Undefined workflows (require discovery)
Clinical charting/vitals in IPD, bed assignment/transfer/discharge, OT booking and consent, pharmacy dispensing against a prescription, lab order-to-result lifecycle, radiology order-to-report lifecycle, blood issue/crossmatch, ambulance dispatch, GL/TPA claims, certificate issuance, staff roster/leave, stock reorder, and report generation.

### Module Dependencies
- **Reception & Registration** is the upstream dependency for nearly everything: OPD, IPD, OT, Pharmacy, Lab, Radiology, Blood Bank, and Billing all key off the UHID + visit number it creates.
- **Billing** depends on service events emitted by OPD/Radiology/Lab/Procedures — currently modeled as four parallel billing blocks rather than one unified invoice (see Risk Analysis).
- **Dashboard** is a read-aggregate over Registration (census), Accounts (income/expense), and HR (present staff) — cannot be built before those source modules are defined.
- **E-MRD / Records** logically depends on every clinical module (OPD, IPD, OT) as the record-of-truth.
- **Activity Log** and **Messages/Notifications** are cross-cutting concerns that must hook into every module's write path, not modules in their own right.
- **Inventory** logically feeds **Pharmacy** (drug stock) and **OT** (consumables), but no linkage is specified in the source doc.

### Reusable Features (build once, use everywhere)
- Patient/Doctor global search (top bar) — one component, used by Patient Enquiry, Registration, OPD, Billing.
- UHID + patient master service — single source of truth, not per-module data.
- A single **billing/invoice engine** (dept, consultant, charges, discount, payment status) instead of four near-duplicate billing blocks (OP/Radiology/Lab/Procedure/Consultation) currently described.
- Consultant master (name, degree, designation, registration number, code, photo) — shared by OPD, IPD, OT scheduling, Reports.
- Generic file-upload component (ID proof, consultation forms, prescriptions).
- Master-data/dropdown service (title, gender, blood group, relationship, department, district/state/pincode) — centralized reference data, not hardcoded per screen.
- Role-conditional list rendering (Super-admin/Admin/Receptionist view vs. individual-consultant view) as a general RBAC pattern, not a one-off for OPD.
- Calendar widget shared by Dashboard, Programmes & Calendar, and the top bar.

## Entity List

| Entity | Key Attributes |
|---|---|
| **Patient** | UHID (auto), Title, DOB, Age (derived), Gender, Address (4-line + district/state/pincode), Contact numbers (+relations), Profession, Email, Blood Group, Allergy flag/type/severity |
| **Emergency Contact** | Relationship, Name, Phone |
| **Encounter/Visit** | Type (OP/IP/Emergency/Day-care), Department, Consultant, Admission type (MLC/NMLC), Referral source (+contact), Category, Auto-number (OP No/IP No) |
| **Consultant/Doctor** | Name, Degree, Designation, Registration number, Consultant code, Photo |
| **Appointment** | Time, Type, linked Patient + Consultant |
| **Consultation Record** | OP Consultation Form, Prescription, Investigation orders, Attachments |
| **Invoice/Billing Line** | Type (OP/Radiology/Lab/Procedure/Consultation), Department, Consultant, Charges, Discount, Payment status |
| **Department** | Name, associated Consultants/Services |
| **User/Login Account** | Role, Credentials, associated Consultant (if clinical) |
| **Notification/Task** | Type, recipient, status |
| **ID Proof / Document** | Type, file |
| **HR/Staff Record** *(inferred, undefined)* | — |
| **Inventory Item** *(inferred, undefined)* | — |
| **Calendar Event/Programme** *(inferred, undefined)* | — |
| **Report** *(inferred, undefined)* | — |

## Missing Functionalities

**Whole modules with zero functional spec** (18 of 21): IPD, OT, Pharmacy, Central Lab, Radiology, Blood Bank, Ambulance, Accounts & Finance, Records & Certificates, HR, Activity Log, Inventory, Programmes & Calendar, Messages & Notifications, Reports, E-MRD, Settings.

**Cross-cutting gaps even within the specified scope:**
- No patient-facing channel at all — no portal, no app, no self-registration, no online appointment booking, no bill/report download.
- No consent management or e-signature (critical for OT, MLC, blood transfusion).
- No insurance/TPA workflow — pre-authorization, cashless approval, claim submission are entirely absent despite "Category" and "Referral" fields implying payer type.
- No discharge process, IPD final billing, or bed/ward master data.
- No integration requirements: HL7/FHIR, PACS/RIS, LIS, SMS/WhatsApp gateway, payment gateway, biometric/Aadhaar, or **ABDM/ABHA** (India's national digital health mission — a material omission for a 2026 Indian HMIS).
- No non-functional requirements anywhere: performance/concurrency targets, data retention policy, backup/DR, audit granularity, or data-privacy controls under India's **DPDP Act 2023**.
- No drug-allergy interaction check specified, despite allergy data being captured at registration — a patient-safety gap.
- No de-duplication/patient-merge strategy for UHID (a near-universal real-world HMIS defect).
- No MLC (medico-legal case) reporting workflow to police/authorities, despite the field existing.
- "Mode of Arrival Form" is referenced as an external/separate document rather than specified inline — an unresolved dependency.

## Risk Analysis

| Risk | Impact |
|---|---|
| **Scope estimation risk** | 18/21 modules undefined — any timeline/budget quoted against this doc will be wrong by a wide margin. Requires formal discovery workshops per module before scoping. |
| **Regulatory/compliance risk** | Blood Bank (NACO/NABH), MRD retention law (Clinical Establishments Act), MLC reporting, and patient data privacy (DPDP Act) are unaddressed. |
| **Interoperability risk** | No named standard (HL7/FHIR/ABDM) risks building a closed system unable to exchange data with insurers, labs, or government disease-reporting systems. |
| **Revenue leakage risk** | Four parallel, manually-confirmed billing blocks (OP/Radiology/Lab/Procedure) instead of one ledger-backed invoice engine increases risk of missed charges and reconciliation errors. |
| **Patient safety risk** | Allergy data is captured but no alerting/clinical decision support is defined for prescribing. |
| **Single point of failure** | Every module depends on Registration/UHID; it must be designed for high availability from day one. |
| **Data integrity risk** | No patient de-duplication/merge strategy — duplicate UHIDs are one of the most common real-world HMIS failures. |
| **Concurrency risk** | Multiple staff editing the same patient/visit record — no locking/versioning strategy mentioned. |

## Future Scalability
- **ABDM/ABHA integration** for national interoperability — design in from the start, not retrofitted.
- **Multi-facility/multi-branch architecture** — scope UHID and department masters to be facility-aware in case Lakshmi Hospitals expands.
- **Telemedicine** as a natural extension of the existing "Consult" action.
- **Patient portal/mobile app** — self-registration, appointment booking, bill payment, report/document download.
- **Insurance/TPA e-claims** workflow (aligning with India's National Health Claims Exchange).
- **PACS/RIS and LIS integration** for Radiology and Lab instead of manual data entry.
- **API-first/microservices architecture** so Pharmacy, Inventory, and Lab can integrate with external vendors later without a rewrite.
- **Configurable role/permission engine** rather than hardcoded per-module views, so new roles (e.g., OT Nurse, Store Manager) can be added without code changes.
- **Separate OLTP vs. analytics store** so the Dashboard's aggregate queries don't degrade transactional performance as volume grows.
- **i18n from day one** — the "Language" tab is already implied in the top bar.

## Open Ambiguities
1. **"E-MRD"** — undefined. Electronic Medical Records Department (a scanning/archival queue) vs. a full EMR system? This materially changes scope.
2. **"Category"** in Registration Details — patient category, or payer category (General/Insurance/Corporate/Cashless)? Unclear.
3. **Naming inconsistency**: "OP Billing" (new patient) vs. "Consultation Billing" (old patient) — same concept, different label; needs to be reconciled to one entity.
4. **Radiology/Labs under OPD** vs. the top-level **Radiology** and **Central Laboratory** sidebar modules — are these the same module surfaced in two places, or duplicated functionality?
5. **MLC/NMLC** — legal reporting workflow and downstream obligations are referenced but never described.
6. **"Autoupdate request for titles"** — unclear whether this means auto-suggesting a title dropdown based on age/gender, or something else.
7. **Consult button's four sub-actions** (consultation form, prescribe, order investigations, upload) — unclear if these are one atomic transaction or independently saved steps.
8. **"Mode of Arrival Form"** — referenced as an external document, not specified in this brief; must be obtained before Registration can be considered complete.

## Recommended Next Steps
1. Resolve the 8 open ambiguities above with the client/hospital stakeholders.
2. Run dedicated requirement-gathering sessions per undefined module, prioritizing the clinically and regulatory-critical ones first: IPD, OT, Pharmacy, Central Laboratory, Radiology, Blood Bank.
3. Only after (1) and (2), proceed to UI design and architecture for the modules that are fully specified (Dashboard, Registration, and partially OPD).
