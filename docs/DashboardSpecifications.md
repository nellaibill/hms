# Enterprise Hospital Dashboard Suite — 10 Role-Based Dashboards

## Purpose
This document specifies the 10 role-based dashboards for the HMS — each role's landing screen after login — built on [InformationArchitecture.md](InformationArchitecture.md) (Home/Dashboard domain), [UserJourneyMaps.md](UserJourneyMaps.md) (persona goals/pain points), [ScreenInventory.md](ScreenInventory.md), and [LayoutFramework.md](LayoutFramework.md) (Page Header, Quick Actions row, content grid).

## Scope
Covers, for each of the 10 dashboards (Executive, Reception, Doctor, Nurse, HR, Accounts, Lab, Radiology, Pharmacy, Inventory): KPIs, Widgets, Charts, Quick Actions, Alerts, Shortcuts, Tables, Recent Activities, Empty States, and Loading States. Also defines shared conventions applied consistently across all 10.

**Out of scope:** visual styling of these elements (see [DesignSystem.md](DesignSystem.md)) and the underlying data/entity model (see [BusinessRequirementsAnalysis.md](BusinessRequirementsAnalysis.md)).

## When to Update This Document
- Whenever a dashboard's KPI set, widget set, or alert conditions change.
- Whenever a new role/persona is introduced that needs its own dashboard.
- Whenever the shared Empty/Loading state conventions change.

## Recommended Sections
- Shared Dashboard Conventions
- One section per dashboard, each as a table: KPIs / Widgets / Charts / Quick Actions / Alerts / Shortcuts / Tables / Recent Activities / Empty States / Loading States

---

## Shared Dashboard Conventions

To keep all 10 dashboards consistent, every dashboard follows the same anatomy and the same Empty/Loading state rules — only the content differs per role.

**Standard anatomy (top to bottom):** KPI strip → Alert banner zone (conditional) → Widget/Chart grid → Table(s) → Recent Activity feed.

**Empty State pattern (applies to every widget/table/chart below):**
- A neutral illustration or icon + one-line message stating the actual condition (never a generic "No data").
- Where the empty state is a *good* outcome (e.g., zero pending approvals), message is affirmative ("All caught up — no pending approvals") rather than alarming.
- Where the empty state blocks a next step (e.g., no roster published), the empty state includes a direct CTA button to resolve it.

**Loading State pattern (applies to every widget/table/chart below):**
- Staged load, never a single full-page spinner: KPI tiles render first (skeleton shimmer blocks matching final tile dimensions), then charts (skeleton axis/bar outlines), then tables (skeleton rows), then the activity feed last.
- Any single widget that fails to load shows an inline retry state scoped to that widget — one failed widget never blocks the rest of the dashboard from rendering.

---

## 1. Executive Dashboard (Hospital Administrator / Super Admin)

| Element | Content |
|---|---|
| **KPIs** | Today's OP footfall · IP census (occupied/available beds) · Revenue today · Revenue MTD vs. target · Bed occupancy % · Staff present % · Open critical alerts |
| **Widgets** | Department-wise income/expense summary · HR presence summary · Plans & Projects status tracker · Compliance/license expiry summary |
| **Charts** | Monthly OP/IP census trend (line) · Department-wise income vs. expense (grouped bar) · Month-wise income & expense (dual-axis line) · Bed occupancy heatmap by ward |
| **Quick Actions** | View Full Financial Report · Open Settings · Broadcast Announcement · Review Escalations |
| **Alerts** | Critical/compliance notifications (MLC pending, blood stock critical, expired license) · KPI-anomaly alert (e.g., revenue dip >15% day-over-day) |
| **Shortcuts** | Finance & Billing · Workforce & Administration → HR · Reports & Analytics · Settings |
| **Tables** | Top 5 revenue-generating departments · Recent escalations/incidents |
| **Recent Activities** | System-wide Activity Log feed (last 10 entries across all modules, admin-scoped) |
| **Empty States** | "No critical alerts — all systems normal" (checkmark) when escalation queue is empty |
| **Loading States** | KPI tiles load first; census/financial charts stage in after; escalation table loads last given it depends on cross-module aggregation |

## 2. Reception Dashboard

| Element | Content |
|---|---|
| **KPIs** | Patients registered today · Patients currently waiting · OP/IP/Emergency split (today) · Pending payments count |
| **Widgets** | Today's queue overview by department · Upcoming appointments summary · Duplicate-patient-match alerts |
| **Charts** | Hourly registration volume (bar, today) · Encounter-type distribution (donut: OP/IP/Emergency/Day-care) |
| **Quick Actions** | New Patient Registration · Search Patient · Old Patient Update · Print Day-End Report |
| **Alerts** | Duplicate UHID match found · Payment gateway unavailable · MLC registration missing required fields |
| **Shortcuts** | Patient Enquiry · Finance & Billing · OPD Queue |
| **Tables** | Today's registrations (Name, UHID, Encounter type, Status, Payment status) · Pending payments list |
| **Recent Activities** | Last 10 registrations/updates performed by this receptionist |
| **Empty States** | "No patients registered yet today" with New Registration CTA (shift start) |
| **Loading States** | Registration table shows skeleton rows during sync from server; search shows an inline spinner, never blocking the rest of the dashboard |

## 3. Doctor Dashboard (OPD Consultant)

| Element | Content |
|---|---|
| **KPIs** | Patients in today's queue · Patients seen so far · Avg. consultation time · Pending investigation results to review |
| **Widgets** | Today's patient queue (next-up highlighted) · Critical lab/radiology alert widget · My schedule/appointments |
| **Charts** | Patients seen per day (line, last 7 days) · Diagnosis category breakdown (donut, this month) |
| **Quick Actions** | Start Next Consultation · View Patient 360 · Order Investigation · Write Prescription |
| **Alerts** | Critical lab value received · Allergy conflict on a pending prescription · Patient waiting >30 minutes |
| **Shortcuts** | OPD Queue · IPD Rounds (if applicable) · Patient Enquiry |
| **Tables** | Today's queue (Time, Patient, Age/Sex, Appointment type, Status) · Pending investigation results |
| **Recent Activities** | Recently completed consultations feed |
| **Empty States** | "No patients in queue" (illustration) between appointment slots; "No pending results" as a positive state |
| **Loading States** | Queue skeleton while syncing from Registration; Patient 360 view shows its own inline spinner without blocking the queue list |

## 4. Nurse Dashboard (IPD Ward)

| Element | Content |
|---|---|
| **KPIs** | Patients under care (ward census) · Vitals due count · Medications due count · Open critical alerts |
| **Widgets** | Bed/ward overview grid (occupied/vacant/cleaning) · Doctor's orders pending acknowledgment · Discharge-readiness checklist |
| **Charts** | Vitals compliance rate (gauge/donut — % completed on time today) · Ward occupancy trend (7-day line) |
| **Quick Actions** | Record Vitals · Administer Medication · View Doctor's Orders · Open Discharge Checklist |
| **Alerts** | Critical vitals reading escalation · Medication due now · Discharge checklist incomplete for a scheduled discharge |
| **Shortcuts** | IPD Bed Management · Nursing Charting · Doctor Rounds |
| **Tables** | Assigned patients (Bed, Patient, Diagnosis, Next vitals due, Next medication due) · Overdue tasks |
| **Recent Activities** | Nursing notes/vitals entries logged this shift |
| **Empty States** | "No patients currently assigned to your ward" (shift handover / reassignment state) |
| **Loading States** | Bed-grid tiles show skeleton placeholders; vitals form shows an inline spinner on submit, not a page-level block |

## 5. HR Dashboard

| Element | Content |
|---|---|
| **KPIs** | Staff present today (%) · Open leave requests · Licenses expiring within 30 days · Roster coverage gaps |
| **Widgets** | Today's attendance by department · Leave request queue · Credential expiry watchlist |
| **Charts** | Attendance trend (30-day line) · Staff distribution by department (bar) · Leave utilization by type (stacked bar) |
| **Quick Actions** | Approve Leave Requests · Build Roster · Add Staff Record · Renew Credential |
| **Alerts** | Roster coverage gap for an upcoming shift · License expired with no renewal on file · Leave approval would breach minimum staffing |
| **Shortcuts** | Staff Directory · Roster/Shift Assignment · Credentialing |
| **Tables** | Pending leave requests (Staff, Dates, Reason, Status) · Expiring credentials |
| **Recent Activities** | Recent HR actions (approvals, new hires, roster changes) |
| **Empty States** | "No pending leave requests" (positive state); "No roster published for next week" (warning state with Build Roster CTA) |
| **Loading States** | Attendance chart shows skeleton bars; roster save shows an inline spinner on the affected row/cell only |

## 6. Accounts Dashboard

| Element | Content |
|---|---|
| **KPIs** | Today's collections · Outstanding dues · Pending discount approvals · Claims pending with TPA |
| **Widgets** | Department-wise income/expense summary · Payment mode breakdown · Orphaned/unreconciled charges |
| **Charts** | Daily collection trend (30-day line) · Income vs. expense by department (grouped bar) · Payment mode distribution (donut) |
| **Quick Actions** | Reconcile Ledger · Process Refund · Review Discount Requests · Submit TPA Claim |
| **Alerts** | Orphaned billing line detected · Refund exceeds original payment (validation) · Claim rejected by TPA |
| **Shortcuts** | Unified Invoice Ledger · Insurance/TPA · Financial Reports |
| **Tables** | Pending discount approvals · Outstanding invoices (Patient, Invoice #, Amount due, Age) |
| **Recent Activities** | Recent payments/refunds processed |
| **Empty States** | "No pending approvals" and "No unreconciled charges" as positive/clean-ledger states |
| **Loading States** | Ledger rows show skeleton placeholders; report generation shows a progress indicator scoped to the Reports widget only |

## 7. Lab Dashboard

| Element | Content |
|---|---|
| **KPIs** | Orders pending · Samples in progress · Results released today · Avg. turnaround time (TAT) |
| **Widgets** | Test order queue by priority · Critical value watchlist · Equipment/instrument status |
| **Charts** | TAT trend (7-day line) · Test volume by category (bar: hematology/biochemistry/microbiology) · Critical value frequency (bar) |
| **Quick Actions** | Accept Next Order · Log Sample · Enter Results · Release Report |
| **Alerts** | Sample rejected, pending recollection · Critical value awaiting release confirmation · TAT SLA breach warning |
| **Shortcuts** | Test Order Queue · Sample Tracking · Results Entry |
| **Tables** | Pending orders (Order #, Patient, Test, Priority, Time received) · Critical values awaiting action |
| **Recent Activities** | Recently released results feed |
| **Empty States** | "No pending orders" at shift start/end (positive state) |
| **Loading States** | Queue shows skeleton rows; result entry shows inline validation-spinner against reference ranges before allowing release |

## 8. Radiology Dashboard

| Element | Content |
|---|---|
| **KPIs** | Studies pending · Studies in progress · Reports released today · Avg. report turnaround |
| **Widgets** | Modality worklist (X-ray/CT/MRI/USG) · Urgent finding watchlist · Equipment utilization |
| **Charts** | Study volume by modality (bar) · Report turnaround trend (7-day line) |
| **Quick Actions** | Open Next Study · Enter Report · Flag Urgent Finding · Release Report |
| **Alerts** | Urgent finding pending acknowledgment by ordering consultant · Broken image reference detected · Report SLA breach |
| **Shortcuts** | Modality Worklist · Report Entry |
| **Tables** | Pending studies (Study #, Patient, Modality, Priority, Ordered time) · Draft/unreleased reports |
| **Recent Activities** | Recently released reports feed |
| **Empty States** | "No studies pending" (positive state) |
| **Loading States** | Worklist shows skeleton rows; image reference panel shows its own loading placeholder independent of the worklist |

## 9. Pharmacy Dashboard

| Element | Content |
|---|---|
| **KPIs** | Prescriptions pending fulfillment · Dispensed today · Low-stock items · Batches expiring soon |
| **Widgets** | Fulfillment queue by priority/ward · Stock alert widget · Allergy-conflict watchlist |
| **Charts** | Dispensing volume trend (7-day line) · Top dispensed drugs (bar) · Stock value by category (donut) |
| **Quick Actions** | Dispense Next Prescription · Check Stock · Reorder Stock · Review Allergy Conflict |
| **Alerts** | Stock below reorder threshold · Batch nearing expiry · Allergy conflict blocking a dispense |
| **Shortcuts** | Prescription Fulfillment Queue · Drug Master · Stock/Batch |
| **Tables** | Pending prescriptions (Rx #, Patient, Ward/OPD, Priority, Time received) · Low-stock items |
| **Recent Activities** | Recently dispensed prescriptions feed |
| **Empty States** | "No pending prescriptions" (positive state) |
| **Loading States** | Queue shows skeleton rows; stock check shows an inline spinner scoped to the item being dispensed |

## 10. Inventory Dashboard

| Element | Content |
|---|---|
| **KPIs** | Items below reorder level · Pending purchase orders · Total stock value · Vendor deliveries due today |
| **Widgets** | Reorder alert widget by category · Vendor performance widget · Recent goods-received widget |
| **Charts** | Stock movement trend (30-day line: inbound vs. outbound) · Stock value by category (bar) · Vendor delivery timeliness (bar) |
| **Quick Actions** | Create Purchase Order · Record Stock Adjustment · Receive Goods · View Vendor List |
| **Alerts** | Critical stock-out risk item · Purchase order overdue from vendor · Stock discrepancy detected (adjustment needed) |
| **Shortcuts** | Item Master · Stock Ledger · Vendor/Purchase Orders |
| **Tables** | Items needing reorder (Item, Current stock, Reorder level, Vendor) · Pending purchase orders |
| **Recent Activities** | Recent stock transactions (received/issued/adjusted) |
| **Empty States** | "No items below reorder level" (positive state) |
| **Loading States** | Stock table shows skeleton rows; PO submission shows an inline spinner on the submit button only |

---

## Cross-Dashboard Note
Every "critical" alert type above (allergy conflict, critical lab value, urgent radiology finding, stock-out, roster gap) routes through the same Notification/Alert architecture defined in [InformationArchitecture.md](InformationArchitecture.md) — dashboards surface a summarized view, but the underlying alert record and escalation logic is shared system-wide, not reimplemented per role.
