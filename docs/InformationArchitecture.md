# Information Architecture — Hospital Management System

## Purpose
This document defines the enterprise Information Architecture for the HMS, derived from [BusinessRequirementsAnalysis.md](BusinessRequirementsAnalysis.md), so navigation structure, module hierarchy, and cross-cutting services (search, notifications, breadcrumbs, user profile) are agreed before any UI design work begins.

## Scope
Covers primary/secondary navigation, module hierarchy tree, parent-child relationships, cross-module navigation, breadcrumb strategy, global search behavior, notification architecture, user profile architecture, and a role-based navigation matrix.

**Out of scope:** visual/UI design (screens, layouts, components) and detailed functional specs for modules marked `(proposed)` below — those remain tracked as gaps in [BusinessRequirementsAnalysis.md](BusinessRequirementsAnalysis.md) pending discovery.

## When to Update This Document
- Whenever a `(proposed)` module gets a confirmed functional spec — update its place in the tree and remove the marker.
- Whenever a new module or domain is added or reorganized.
- Whenever the role-based navigation matrix changes (new role, changed access).

## Recommended Sections
- IA Principles
- Primary Navigation
- Secondary Navigation
- Module Hierarchy (Full Tree)
- Parent-Child Relationship Rules
- Cross-Module Navigation
- Breadcrumb Strategy
- Global Search Behavior
- Notification Architecture
- User Profile Architecture
- Role-Based Navigation Matrix

---

## IA Principles

1. **Group by business domain, not by literal menu list.** The source brief's 21-item flat sidebar doesn't scale past a handful of users — it's clustered into ~10 primary domains here.
2. **Patient-centric, not module-centric.** A patient's data is touched by 8+ modules; navigation must let staff follow the *patient*, not force a re-search in every module.
3. **Decouple "where am I" from "who am I working on."** Breadcrumb = location in the module tree. Patient Context Bar = the active patient/encounter. Conflating these (as most legacy HMIS do) breaks multi-tasking.
4. **Cross-cutting services aren't modules.** Billing, Notifications, and Activity Log are engines invoked *from* other modules, not silos users navigate *to* by default (though each has a system-of-record rollup view).
5. **RBAC drives visibility, not just permission.** Navigation itself is filtered per role — progressive disclosure, not "show everything, block on click."
6. **Undefined scope is marked, not invented.** Anything the source brief didn't specify is labeled `(proposed)` — structurally placed, not functionally designed.

## Primary Navigation

Ten domains + Home, replacing the flat 21-item sidebar. This is the persistent top-level rail/menu.

| Domain | Absorbs source modules |
|---|---|
| **Home** | Dashboard |
| **Patient Management** | Patient Enquiry, Reception & Registration |
| **Clinical Care** | OPD, IPD, OT |
| **Diagnostics & Ancillary** | Central Laboratory, Radiology, Blood Bank |
| **Pharmacy** | Pharmacy |
| **Support Services** | Ambulance, Hospital Inventory Management |
| **Finance & Billing** | Accounts and Finance (+ billing rollup from all modules) |
| **Records & Compliance** | Records and Certificates, E-MRD |
| **Workforce & Administration** | HR, Activity Log, Settings |
| **Engagement** | Programmes and Calendar, Messages and Notifications |
| **Reports & Analytics** | Reports |

Primary nav is **role-filtered** (see Role-Based Navigation Matrix) — a Receptionist never sees "Workforce & Administration"; a Consultant sees "Clinical Care" pre-expanded to their own queue.

## Secondary Navigation

Contextual, appears only once a Primary domain is selected — rendered as a left-rail or tab set scoped to that domain. It is never global. Pattern: **Domain → Module → View**. E.g., selecting *Clinical Care* surfaces secondary nav for OPD / IPD / OT; selecting *OPD* surfaces tertiary nav for Admin View vs. Consultant View. Secondary nav never contains cross-module jumps — those are handled by contextual anchors (see Cross-Module Navigation), keeping the static hierarchy clean.

## Module Hierarchy — Full Tree

```
HMIS Root
├── 0. Global Chrome (persistent, all modules)
│   ├── System Logo / Name
│   ├── Global Search
│   ├── Patient Context Bar (conditional — see Breadcrumb Strategy)
│   ├── Notification Center
│   ├── Calendar (quick view)
│   ├── Calculator (utility)
│   ├── Pending Tasks
│   ├── Language Selector
│   └── User Profile Menu
│
├── 1. Home / Dashboard
│   ├── Census Widget (OP/IP)
│   ├── Department Income & Expense Chart
│   ├── Calendar — Notifications & Events
│   ├── Present HR Widget
│   └── Plans & Projects Status
│
├── 2. Patient Management
│   ├── 2.1 Patient Enquiry
│   │   ├── Search (Name / Age / UHID / Phone)
│   │   └── Patient 360 View → cross-links to Registration, Clinical, Billing, Records
│   └── 2.2 Reception & Registration
│       ├── New Patient Registration
│       │   ├── Patient Details
│       │   ├── Emergency Contact Details
│       │   ├── Mode of Arrival
│       │   ├── Allergy Details
│       │   ├── Registration Details (OP / IP / Emergency / Day-care)
│       │   ├── Document Upload (Photo, ID Proof)
│       │   └── → Billing (contextual child; owned by Finance)
│       └── Old Patient Registration
│           ├── Patient Search & Demographic Update
│           ├── Registration Details
│           └── → Billing (contextual child)
│
├── 3. Clinical Care
│   ├── 3.1 OPD
│   │   ├── Admin/Receptionist View
│   │   │   ├── Patient List
│   │   │   ├── Consultants List
│   │   │   ├── Investigations List
│   │   │   ├── Procedures List
│   │   │   └── Admissions List
│   │   └── Consultant View
│   │       ├── Consultant Profile
│   │       ├── Patient Queue
│   │       └── Consult Workspace
│   │           ├── OP Consultation Form
│   │           ├── Prescribe Drugs → cross-link to Pharmacy
│   │           ├── Order Investigations → cross-link to Lab/Radiology
│   │           └── Upload Documents
│   ├── 3.2 IPD (proposed — undefined in source brief)
│   │   ├── Bed/Ward Management
│   │   ├── Admission / Transfer / Discharge
│   │   ├── Nursing Charting
│   │   └── Doctor Rounds
│   └── 3.3 Operation Theatre (proposed)
│       ├── OT Scheduling
│       ├── Consent Management
│       ├── Surgical Team Assignment
│       └── Anesthesia / Operative Notes
│
├── 4. Diagnostics & Ancillary
│   ├── 4.1 Central Laboratory (proposed)
│   │   ├── Test Order Queue
│   │   ├── Sample Collection / Tracking
│   │   └── Results Entry / Release
│   ├── 4.2 Radiology (proposed)
│   │   ├── Modality Worklist
│   │   ├── Report Entry
│   │   └── Image Reference
│   └── 4.3 Blood Bank (proposed)
│       ├── Donor Management
│       ├── Blood Unit Inventory
│       └── Issue / Crossmatch
│
├── 5. Pharmacy (proposed)
│   ├── Drug Master
│   ├── Prescription Fulfillment Queue
│   └── Stock / Batch / Expiry
│
├── 6. Support Services
│   ├── 6.1 Ambulance (proposed)
│   │   ├── Dispatch
│   │   └── Trip Log / Billing
│   └── 6.2 Hospital Inventory Management (proposed)
│       ├── Item Master
│       ├── Stock & Reorder
│       └── Vendor / Purchase Orders
│
├── 7. Finance & Billing
│   └── 7.1 Accounts and Finance
│       ├── Unified Invoice Ledger (rolls up OP/Radiology/Lab/Procedure/Consultation billing)
│       ├── Payments & Refunds
│       ├── Insurance / TPA (proposed)
│       └── General Ledger / Financial Reports
│
├── 8. Records & Compliance
│   ├── 8.1 Records and Certificates (proposed)
│   │   ├── Certificate Issuance
│   │   └── MRD Retrieval
│   └── 8.2 E-MRD (proposed — scope pending clarification, see BusinessRequirementsAnalysis.md Open Ambiguities)
│
├── 9. Workforce & Administration
│   ├── 9.1 Human Resource Management (proposed)
│   ├── 9.2 Activity Log (system-wide audit — cross-cutting, read-only rollup)
│   └── 9.3 Settings
│       ├── Master Data (Departments, Consultants, Dropdown lists)
│       ├── Roles & Permissions
│       └── System Configuration
│
├── 10. Engagement
│   ├── 10.1 Programmes and Calendar (proposed)
│   └── 10.2 Messages and Notifications (feeds Notification Center)
│
└── 11. Reports & Analytics (proposed)
    ├── Operational Reports
    ├── Clinical Reports
    ├── Financial Reports
    └── Statutory / Regulatory Reports
```

## Parent-Child Relationship Rules

- **Ownership follows the domain that creates the record**, not the domain that triggers it. E.g., "Order Investigations" is initiated *inside* OPD's Consult Workspace, but the resulting order record is owned by Diagnostics & Ancillary — OPD holds a reference/link, not the record.
- **Billing is a child of every billable event, and a rollup under Finance.** Registration, OPD Consult, Lab Order, Radiology Order, and Procedures each spawn a billing line; Finance & Billing is where they converge into one ledger. This resolves the "OP Billing vs. Consultation Billing" naming inconsistency flagged in the requirements analysis — one Invoice/Billing Line entity, multiple entry points.
- **Patient is the root parent of all clinical and financial children** (Encounters, Prescriptions, Orders, Invoices, Documents) regardless of which module created them — this is why Patient 360 (under Patient Enquiry) exists as an aggregation view, not a duplicate data store.
- **Activity Log and Notifications are not children of any domain** — they are horizontal services that attach to every write-transaction system-wide.

## Cross-Module Navigation

Handled as **contextual anchors** — dynamic, data-driven links surfaced inside a record view, deliberately kept out of the static Primary/Secondary nav tree:

| From | Anchors to |
|---|---|
| Patient 360 (Patient Enquiry) | Registration history, Clinical encounters (OPD/IPD/OT), Billing history, Lab/Radiology results, Documents/E-MRD |
| Registration (on save) | OPD Queue / IPD Admission / Billing, depending on encounter type chosen |
| OPD Consult Workspace | Pharmacy (Prescribe), Lab/Radiology (Order Investigations) |
| Lab/Radiology Result (on release) | Ordering consultant's patient chart + encounter (via notification) |
| Any Billing Line | Source encounter/module, for audit traceability |
| Dashboard widgets | Filtered drill-down view in the source module (e.g., census tile → Patient Enquiry filtered list) |

Rule: a contextual anchor **replaces** the breadcrumb with the destination's true hierarchy and adds a transient "← Back to [origin]" affordance — it does not fake the origin module into the destination's breadcrumb path.

## Breadcrumb Strategy

**Location-based, not history-based** — predictable and independent of how the user arrived.

Format: `Home > Domain > Module > Sub-module > Record`
Example: `Home > Clinical Care > OPD > Consultant View > Consult — Rao, R. (UHID 000123)`

- The breadcrumb reflects the **module hierarchy tree**, never the click path.
- The **active patient/encounter is deliberately excluded** from the breadcrumb chain — it lives in a persistent Patient Context Bar instead, because a patient can be reached from Registration, OPD, Billing, or Search, and forcing the breadcrumb to encode "how you got to this patient" would make it unpredictable and non-linkable/bookmarkable.
- Cross-module jumps (see Cross-Module Navigation) update the breadcrumb to the destination's real path, with the transient "back to origin" link handling return navigation separately.

## Global Search Behavior

- One search box, in Global Chrome, always available — not scoped per module. "Patient Search" and "Doctor Search" in the top bar (per source brief) are **entity-type filters on the same engine**, not separate search systems.
- **Pattern-aware query parsing**: numeric-UHID-shaped → Patient by UHID; phone-shaped → Patient by contact; alphabetic → Name match across Patients and Consultants.
- **Results grouped by entity type** (tabs): Patients, Consultants/Doctors, Encounters/Visits, Invoices, Departments — each tab permission-filtered by the logged-in role (a Receptionist's search never surfaces payroll/HR entities).
- **Selecting a Patient result sets the Patient Context Bar**, rather than opening a single page — every subsequent module the user navigates into stays anchored to that patient until the context is explicitly cleared. This is the mechanism that makes cross-module patient-following work without repeated re-search.
- Recent/frequent searches cached per session; keyboard-accessible entry point.

## Notification Architecture

The source brief conflates three concepts that must be architecturally separate:

| Concept | Purpose | Persistence |
|---|---|---|
| **Notifications** | Ephemeral, informational or actionable alerts | Read/unread state, expires or archives |
| **Pending Tasks** | Actionable work-queue items requiring completion (a subset of notifications that block a workflow) | Open until actioned |
| **Activity Log** | Immutable system-wide audit trail | Permanent, admin/audit-only view |

- **Taxonomy by category**: Clinical (critical lab value, allergy conflict), Operational (bed availability, OT schedule change), Administrative (leave approval), Financial (discount/payment approval), System (new-device login).
- **Priority tiers**: Critical (blocking), Actionable (needs response — routes to Pending Tasks), Informational (FYI only).
- **Routing is rule-based**, tied to role + relationship to the record — e.g., a critical lab value routes to the *ordering* consultant specifically, not all consultants; a discount-approval request routes to the role authorized to approve it.
- **Notification Center** (in Global Chrome) groups by category, supports mark-as-read, and deep-links via the same contextual-anchor mechanism into the source record.
- Every notification-worthy event is also written to Activity Log, but Activity Log is never itself surfaced as a notification — it's a separate, audit-only rollup under Workforce & Administration.

## User Profile Architecture

Two distinct entities sit behind one profile menu — the source brief blends them, but they must be modeled separately:

- **User Account** (authentication identity): username/credentials, role, permission set, login history, session/device info.
- **Staff/Consultant Profile** (business identity): name, degree, designation, registration number, photo, department — exists only for clinical/operational roles, linked 1:1 to a User Account when applicable.

**Profile menu structure:**
- Identity header — photo, name, role, facility/branch (multi-facility, future scalability)
- Role-conditional quick actions — Consultant: "My Consultation Queue"; Admin: "User Management"; every role: "My Activity" (a personal, filtered slice of Activity Log)
- Preferences — Language, theme, notification settings
- Security — Change password, MFA/session management
- Facility/Branch switcher (placeholder for multi-facility scalability)
- Logout

## Role-Based Navigation Matrix

RBAC-driven progressive disclosure — navigation itself is filtered per role, not just access blocked on click.

| Domain | Super Admin | Admin | Receptionist | Consultant/Doctor |
|---|---|---|---|---|
| Home | ✓ | ✓ | ✓ | ✓ (own-queue view) |
| Patient Management | ✓ | ✓ | ✓ | ✓ (read + clinical update) |
| Clinical Care | ✓ | ✓ | — (visibility only) | ✓ (own patients) |
| Diagnostics & Ancillary | ✓ | ✓ | — | ✓ (order/view results) |
| Pharmacy | ✓ | ✓ | — | ✓ (prescribe only) |
| Support Services | ✓ | ✓ | — | — |
| Finance & Billing | ✓ | ✓ | ✓ | — |
| Records & Compliance | ✓ | ✓ | ✓ (limited) | ✓ (own patients) |
| Workforce & Administration | ✓ | ✓ (limited) | — | — |
| Engagement | ✓ | ✓ | ✓ | ✓ |
| Reports & Analytics | ✓ | ✓ (scoped) | — | ✓ (own performance only) |

---

This IA resolves two structural issues flagged in the requirements analysis: the flat 21-module sidebar (regrouped into 10 navigable domains), and the "OP Billing vs. Consultation Billing" duplication (resolved into one Finance-owned invoice entity with multiple contextual entry points).
