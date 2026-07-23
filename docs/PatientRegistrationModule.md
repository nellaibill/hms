# Patient Registration Module — Complete UX Specification

## Purpose
This document is the complete UX specification for the Patient Registration Module, designed with an Epic EMR mindset: search-before-create as a hard gate, fast keyboard-driven flows, an eMPI-style duplicate engine, and an unforgiving audit trail. It exists so the module's most complex and highest-traffic screen is fully specified before implementation.

## Scope
Covers Information Architecture, Form Sections, Step Wizard, Validation, Field Grouping, Auto Save, Keyboard Navigation, Search, Patient Merge, Duplicate Detection, Emergency Registration, File Upload, Photo Capture, Billing Integration, Audit Trail, Accessibility, Error Handling, Success Flow, and Responsive Layout for this module specifically.

**Out of scope:** the broader Information Architecture, Design System tokens/components, and Layout Framework this module builds on — see the linked documents below for those.

## When to Update This Document
- Whenever the registration form's field set or section grouping changes.
- Whenever the duplicate-detection algorithm or merge workflow changes.
- Whenever a new failure mode or edge case is discovered in production and needs a defined handling behavior.

## Recommended Sections
Information Architecture · Form Sections · Step Wizard · Validation · Field Grouping · Auto Save · Keyboard Navigation · Search · Patient Merge · Duplicate Detection · Emergency Registration · File Upload · Photo Capture · Billing Integration · Audit Trail · Accessibility · Error Handling · Success Flow · Responsive Layout

Built on [InformationArchitecture.md](InformationArchitecture.md), [DesignSystem.md](DesignSystem.md), [LayoutFramework.md](LayoutFramework.md), and the Receptionist journey in [UserJourneyMaps.md](UserJourneyMaps.md).

---

## 1. Information Architecture

Registration sits under **Patient Management** in the IA, alongside Patient Enquiry. Entry points:
- Sidebar → Patient Management → Reception & Registration
- Global Search with no confident match → "Register new patient" CTA in the empty-results state
- Dashboard Quick Action (Reception Dashboard → "New Patient Registration")
- Old Patient flow via Patient Enquiry → Patient 360 → "Update Registration"

Screen sequence: **Search/Identify** (gate, §8) → **New Patient Wizard** *or* **Old Patient Update** → **Registration Details** → **Document Upload** → **Review & Billing** → **Success/Confirmation**. On completion, the record surfaces immediately in Patient 360 and routes into the relevant clinical queue per encounter type (§18).

## 2. Form Sections

Seven data groups, each an independently addressable step and an ARIA fieldset:

1. **Patient Identification & Demographics** — Title, Name, DOB/Age, Gender, Blood Group
2. **Address** — 3-line address, District, State, Pincode
3. **Contact Details** — Phone(s) with relation, Email, Profession
4. **Emergency Contact** — Relationship, Name, Phone
5. **Mode of Arrival** — arrival method (walk-in/ambulance/referred)
6. **Allergy Details** — Known allergy Y/N, Type, Severity
7. **Registration/Encounter Details** — Type (OP/IP/Emergency/Day-care), Department, Consultant, Admission Type (MLC/NMLC), Referral, Category
8. **Document Upload** — Photo, ID Proof
9. **Billing** (contextual, owned by Finance & Billing per the IA's parent-child rules, but rendered inline as the final wizard step)

## 3. Step Wizard

A **hybrid navigator**, not a strict linear wizard — closer to Epic's Storyboard pattern than a locked sequence:

- Left-side **Section Navigator** (240px column, per Layout Framework's secondary-nav pattern): lists all 9 sections with a status icon each — completed (check), current (filled dot), upcoming (outline), error (red, incomplete-required).
- **Non-linear jump is allowed** between most sections — a receptionist can capture Allergy Details before Address if that's the order information arrives in.
- **Hard gates** (cannot skip ahead of): Registration Details requires Demographics complete; Billing requires Registration Details complete (mirrors the IA's rule that billing is a child of the encounter).
- A persistent mini patient-summary sits above the navigator once Name+DOB are entered, so the receptionist always sees who they're registering — same pattern as the Patient Context Bar, activated early.

## 4. Validation

| Type | Behavior |
|---|---|
| Field format (phone, email, pincode) | Inline, on blur, blocking only that field |
| Required field | Blocking on step-completion attempt, not on every keystroke |
| Cross-field (MLC requires Referral + contact) | Banner at top of the Registration Details section, blocking submission of that step |
| Async duplicate check | Debounced 500ms, fires once Name + DOB + one of Phone/Gender are present — before the user ever reaches Registration Details (see §10) |
| Validation summary | Each section header shows an error count; clicking it scrolls/focuses the first invalid field |

Never blocks progress on a *warning* (probable duplicate, missing optional field) — only on a true *error* (missing required field, unresolved exact-duplicate, unconfirmed allergy conflict).

## 5. Field Grouping

Each section is a semantic `<fieldset>`/`<legend>` pair (per [Design System §10](DesignSystem.md)). Progressive disclosure inside Registration Details: selecting encounter type **IP** or **Emergency** reveals Admission Type (MLC/NMLC) + Referral fields; selecting **Day-care** reveals Observation Type instead — fields the user doesn't need are never rendered, not just disabled. Pincode entry auto-fills District/State from a master lookup (reduces redundant entry per WCAG 3.3.7).

## 6. Auto Save

- Drafts save every 15 seconds and on every section-navigation event, against a server-side "Draft Registration" keyed to the in-progress session.
- A quiet "Saved · a few seconds ago" indicator sits beside the Section Navigator — never a modal, never interrupts typing.
- If server autosave fails (network drop), entries buffer to local storage and a persistent (non-blocking) banner reads "Changes saved locally — reconnecting…" with a manual "Retry now" action. Data is never silently lost.
- Drafts auto-purge 24 hours after last activity, or immediately on successful final submission.

## 7. Keyboard Navigation

| Key | Action |
|---|---|
| `Tab` / `Shift+Tab` | Move through fields in logical section order |
| `Alt+→` / `Alt+←` | Next / previous section (mouse-free wizard navigation) |
| `Ctrl+S` | Force-save draft immediately |
| `Ctrl+Enter` | Complete current section / submit final step |
| `Esc` | Dismiss the active dialog (duplicate match, allergy confirmation) without losing form data |
| `Ctrl+K` or `/` | Jump focus to the patient search field (consistent with Global Search's shortcut) |

Dropdowns are arrow-key and type-ahead operable; a skip link lets keyboard users jump past the Section Navigator straight into the active form; no keyboard traps anywhere, including inside dialogs.

## 8. Search

**Search is a mandatory gate before "New Patient" is enabled** — mirroring Epic's insistence on an MPI search before chart creation, to suppress duplicates at the source rather than clean them up later.

- Search box: Name, UHID, Phone, or DOB — any combination.
- Results ranked with a **match-confidence indicator** (High/Medium/Low) based on which fields matched.
- Selecting a result routes into **Old Patient Update** instead of creation.
- "Register as New Patient" only becomes enabled after at least one search attempt returns no acceptable match — a deliberate friction point, not an oversight.

## 9. Patient Merge

For duplicates discovered after the fact (e.g., by an Admin auditing UHIDs):

- Admin selects a **Primary (surviving)** UHID and a **Secondary (to-be-merged)** UHID.
- Side-by-side field comparison table highlights conflicts (e.g., differing phone numbers); the admin picks the winning value per conflicting field.
- On confirm, all historical encounters, billing, lab, and radiology records reassign to the surviving UHID; the secondary UHID becomes a permanent redirect/alias — **never hard-deleted**, preserving audit integrity.
- Restricted to Admin/Super Admin role; requires a confirmation dialog stating irreversibility explicitly ("This merges all clinical and billing history into one record — this cannot be undone").
- Fully logged to Audit Trail with a before/after snapshot of both records.

## 10. Duplicate Detection

Three tiers, running at two points (pre-creation search §8, and live during New Patient entry):

| Tier | Trigger | Behavior |
|---|---|---|
| Exact match | Name + DOB + Phone identical | Hard block — must select the existing record or explicitly justify creating a new one (justification logged) |
| Probable match | Name + DOB match with phone differing, or phonetic name match + same DOB | Warning dialog listing candidate(s) with confidence % — receptionist selects the match or overrides with a required reason, logged to Audit Trail |
| Low-confidence | Partial match on fewer fields | Non-blocking inline suggestion chip near the Name field: "2 similar patients found — Review" |

## 11. Emergency Registration

A distinct, abbreviated fast-path so clinical care is never delayed by paperwork:

- Single-screen minimal form: Name (or "Unknown"), approximate age, gender, mode of arrival, MLC/NMLC flag.
- Generates a **temporary UHID** immediately (reserved emergency numbering block, e.g. `TEMP-######`) so the clinical encounter can start within seconds.
- Auto-creates a Pending Task — "Complete registration for TEMP-XXXX" — assigned back to Reception for once the patient/attendant can provide full details.
- When full registration later completes, the temp ID reconciles into a permanent UHID via the **same Patient Merge mechanism** (§9), not a separate one — no encounter or clinical data captured under the temp ID is ever lost or re-entered.

## 12. File Upload

Per [Design System §27](DesignSystem.md): drag-drop zone + equally prominent "Browse files" button, accepted types/size limit shown up front (JPG/PNG/PDF, max 5MB), per-file progress bar, immediate thumbnail preview, remove/retry per file — never a generic "upload failed."

Two upload targets in this module: **Patient Photo** (see §13) and **ID Proof** (type dropdown — Aadhaar/Passport/Driving License/Voter ID/Other — plus the file itself).

## 13. Photo Capture

In-browser webcam capture as an alternative to uploading a file:
- Live preview with an oval face-framing guide, capture button, retake option.
- Captured photo passes through the same crop/confirm step as an uploaded photo before final save.
- Graceful degradation: if no camera is available or permission is denied, the module falls back to file upload automatically with clear messaging ("Camera not available — upload a photo instead") — capture is never the *only* path, satisfying the same input-modality-independence principle as Upload (§12).

## 14. Billing Integration

The final wizard step surfaces the billing block matching the chosen encounter type (OP/IP/Radiology/Lab/Procedure), per the IA's rule that billing is a child of the encounter, rendered inline rather than a separate navigation — registration and initial billing complete as one transaction. A discount request beyond the receptionist's authorization limit triggers the same **Discount Approval Dialog** used in the Accounts journey map, pausing completion until Accounts approves or the amount is adjusted. Emergency/MLC cases can select "Bill later — pending" instead of forcing payment before clinical care begins.

## 15. Audit Trail

Every field change, section save, duplicate-override justification, merge action, and billing confirmation writes an immutable entry to the Activity Log (per the IA's cross-cutting Activity Log module): timestamp, user, field, old→new value (on edits to existing records), and device/session context. Registration-specific events: *Patient created*, *Duplicate match overridden (reason: …)*, *Patient merged (secondary UHID X → primary UHID Y)*, *Emergency temp-registration converted to permanent*. Entries are read-only, visible to Admin/Compliance roles via the Activity Log's Patient Management filter.

## 16. Accessibility

Applies [Design System §28](DesignSystem.md) directly to this module:
- All fields keyboard-operable and screen-reader labeled; no placeholder-as-label.
- Validation errors announced via `aria-live="assertive"` plus inline icon + text — never color alone.
- Stepper/navigator uses `aria-current="step"` and proper landmark roles.
- Duplicate-match and merge dialogs are focus-trapped, with focus returned to the triggering control on close.
- Photo Capture always has a non-camera fallback path (no functionality depends on one input modality).
- Redundant entry is actively avoided (WCAG 3.3.7) — pincode→state autofill, and the Old Patient flow pre-fills every previously captured field rather than re-asking.

## 17. Error Handling

| Failure | Handling |
|---|---|
| Field-level format/required error | Inline, non-blocking to other fields, blocks only that section's completion |
| Cross-field conflict (MLC without referral) | Section-level banner, not a full-page interrupt |
| Autosave failure | Local-storage buffer + non-blocking retry banner; never silently drops data |
| Duplicate-check service timeout | Warning banner ("Duplicate check unavailable — proceed with caution") rather than blocking registration entirely |
| Billing/payment failure at final step | All entered data preserved; offers "Save as pending, complete billing later" instead of forcing a restart |

## 18. Success Flow

On final submission: a confirmation screen (persists until explicitly dismissed — not auto-navigated away) showing the UHID (large, copyable), encounter number, and printable Registration Slip + ID Card. Role-based next step:
- **OP** → "Send to OPD Queue" (patient appears in the Consultant's queue immediately)
- **IP** → "Proceed to Bed Assignment"
- **Emergency** → immediate redirect into the emergency clinical workflow with a persistent temp-UHID banner until reconciled

A brief, non-blocking success toast confirms the save; the confirmation screen itself remains until the receptionist chooses to move on, since the printable outputs matter operationally.

## 19. Responsive Layout

Per [LayoutFramework.md](LayoutFramework.md):
- **Desktop/Laptop:** Section Navigator as a 240px left column beside the form; form content capped at 720px max-width, centered in the remaining space (per the Design System's rule that forms never widen just because screen space allows it).
- **Tablet** (registration desk, touch): navigator collapses to a horizontal top stepper; touch targets grow to 44px; one section visible per screen at a time.
- **Mobile/handheld** (emergency intake): fullscreen single-column, one field group per scroll screen, sticky bottom bar with Back/Next.

---

## Design Intent

Search-before-create, non-linear section access, and duplicate detection running continuously in the background — registration should never force a receptionist into a rigid sequence, but it should also never let a duplicate UHID slip through unnoticed.
