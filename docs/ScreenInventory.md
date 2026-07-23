# Screen Inventory — HMIS Complete Catalog

## Purpose
This document is the complete screen catalog for the HMS, built on [InformationArchitecture.md](InformationArchitecture.md)'s module tree, so every module's screen set (main, create, edit, view, search, dashboard, reports, settings, dialogs, popups, print views, export views) is enumerated before wireframing begins.

## Scope
Covers a full screen inventory for all 21 modules plus Dashboard and cross-cutting chrome (authentication, global search, notifications, user profile, system/error screens).

**Out of scope:** visual/UI design of the listed screens, and detailed field-level specs for modules marked `(proposed)` — those remain tracked as gaps in [BusinessRequirementsAnalysis.md](BusinessRequirementsAnalysis.md) pending discovery.

## When to Update This Document
- Whenever a `(proposed)` module gets a confirmed functional spec — validate and update its screen list.
- Whenever a new screen type is added to an existing module (e.g., a new dialog or report).
- Whenever a module is added, removed, or restructured in the Information Architecture.

## Recommended Sections
- Cross-Cutting Screens
- One section per domain, with one table per module (Main / Create / Edit / View / Search / Dashboard / Reports / Settings / Dialogs / Popups / Print Views / Export Views)

**Legend:** "—" means this screen type doesn't apply to that module (e.g., a system-generated log has no Create screen).

---

## Cross-Cutting Screens (not owned by any single module)

| Screen Type | Screens |
|---|---|
| Authentication | Login Screen, Forgot Password Screen, MFA Verification Screen, Session Timeout Dialog |
| Global Search | Global Search Results Screen (grouped by entity type) |
| Notification | Notification Center Dropdown Popup (bell icon quick view) |
| Utility | Pending Tasks Panel, Calculator Widget Popup, Calendar Quick View Popup |
| User Profile | My Profile Screen, Change Password Screen, Facility/Branch Switcher Dialog |
| System/Error | 403 Forbidden Screen, 404 Not Found Screen, 500 System Error Screen, Offline/Connectivity Banner |

---

## Home Domain

### Dashboard
| Screen Type | Screens |
|---|---|
| Main Screen | Executive Dashboard (Home) |
| Create Screen | — (read-only aggregation) |
| Edit Screen | Dashboard Layout Customization Screen |
| View Screen | KPI Widget Drill-Down View |
| Search Screen | — |
| Dashboard | *(this module is the dashboard)* |
| Reports | Dashboard Summary Report |
| Settings | Widget Configuration Screen |
| Dialogs | Add/Remove Widget Dialog, Date Range Picker Dialog |
| Popups | KPI Tooltip Popup, Alert Popup |
| Print Views | Dashboard Summary Print View |
| Export Views | Dashboard Export (PDF/Excel) |

---

## Patient Management Domain

### Patient Enquiry
| Screen Type | Screens |
|---|---|
| Main Screen | Patient Enquiry Landing |
| Create Screen | — (creation owned by Registration) |
| Edit Screen | Quick Update Patient Details Dialog |
| View Screen | Patient 360 View |
| Search Screen | Patient Search (Name/Age/UHID/Phone) |
| Dashboard | Patient Enquiry Summary Widget (visit counts) |
| Reports | Patient Visit History Report |
| Settings | — |
| Dialogs | Confirm Patient Match Dialog (duplicate resolution) |
| Popups | Recent Visits Popup |
| Print Views | Patient Summary Print View |
| Export Views | Patient Data Export (PDF/CSV) |

### Reception & Registration
| Screen Type | Screens |
|---|---|
| Main Screen | Registration Landing (New/Old tabs) |
| Create Screen | New Patient Registration Screen |
| Edit Screen | Old Patient Registration / Update Screen |
| View Screen | Registration Detail View |
| Search Screen | Existing Patient Search Screen |
| Dashboard | Daily Registration Count Widget |
| Reports | Registration Report (daily/monthly) |
| Settings | Registration Form Configuration (mandatory fields, dropdown masters) |
| Dialogs | Duplicate UHID Confirmation Dialog, MLC Confirmation Dialog, Allergy Confirmation Dialog |
| Popups | Upload Success/Failure Popup |
| Print Views | Registration Slip Print View, Patient ID Card Print View |
| Export Views | Registration Data Export |

---

## Clinical Care Domain

### OPD
| Screen Type | Screens |
|---|---|
| Main Screen | OPD Landing (Admin/Consultant toggle) |
| Create Screen | New Appointment Screen, New Consultation Screen |
| Edit Screen | Edit Appointment Screen, Edit Consultation Note Screen |
| View Screen | Consultant Profile View, Patient Queue View, Consultation Detail View |
| Search Screen | Patient/Consultant/Investigation Search |
| Dashboard | OPD Census Widget (patients seen, waiting) |
| Reports | OPD Activity Report, Consultant Performance Report |
| Settings | OPD Configuration (slot duration, consultant list) |
| Dialogs | Prescribe Drugs Dialog, Order Investigations Dialog, Allergy Conflict Dialog |
| Popups | Queue Position Popup, Appointment Reminder Popup |
| Print Views | Consultation Form Print View, Prescription Print View |
| Export Views | Consultation Export, Prescription Export (PDF) |

### IPD *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | IPD Landing (Ward/Bed Overview) |
| Create Screen | New Admission Screen, New Nursing Note Screen |
| Edit Screen | Edit Care Plan Screen, Transfer Bed Screen |
| View Screen | Bed/Ward View, Patient Chart View, Doctor Rounds View |
| Search Screen | Admitted Patient Search |
| Dashboard | Bed Occupancy Widget, Admission/Discharge Widget |
| Reports | IPD Census Report, Length-of-Stay Report |
| Settings | Ward/Bed Master Configuration |
| Dialogs | Discharge Checklist Dialog, Critical Vitals Escalation Dialog |
| Popups | Bed Availability Popup |
| Print Views | Discharge Summary Print View, Nursing Chart Print View |
| Export Views | IPD Patient Record Export |

### Operation Theatre (OT) *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | OT Landing (Schedule Overview) |
| Create Screen | New OT Booking Screen, New Consent Form Screen |
| Edit Screen | Edit OT Schedule Screen, Edit Surgical Team Screen |
| View Screen | OT Calendar View, Operative Notes View |
| Search Screen | OT Booking Search |
| Dashboard | OT Utilization Widget |
| Reports | OT Utilization Report, Surgical Outcomes Report |
| Settings | OT Room/Equipment Master |
| Dialogs | Consent Confirmation Dialog, Team Assignment Dialog |
| Popups | OT Slot Conflict Popup |
| Print Views | Consent Form Print View, Operative Note Print View |
| Export Views | OT Case Log Export |

---

## Diagnostics & Ancillary Domain

### Central Laboratory *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Lab Landing (Order Queue) |
| Create Screen | New Test Order Screen (cross-link), Manual Order Entry Screen |
| Edit Screen | Edit/Correct Result Screen |
| View Screen | Sample Tracking View, Result Detail View |
| Search Screen | Order/Sample Search |
| Dashboard | Turnaround Time Widget, Pending Tests Widget |
| Reports | Lab Activity Report, Critical Value Report |
| Settings | Test Catalog Configuration, Critical Threshold Configuration |
| Dialogs | Sample Rejection Dialog, Critical Value Alert Dialog |
| Popups | Result Released Popup |
| Print Views | Lab Report Print View |
| Export Views | Lab Result Export (PDF/HL7) |

### Radiology *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Radiology Landing (Modality Worklist) |
| Create Screen | New Imaging Order Screen (cross-link), Manual Order Entry Screen |
| Edit Screen | Edit Report Draft Screen |
| View Screen | Study/Image View, Report Detail View |
| Search Screen | Study Search |
| Dashboard | Pending Studies Widget, Report Turnaround Widget |
| Reports | Radiology Activity Report |
| Settings | Modality/Study Type Master |
| Dialogs | Urgent Finding Alert Dialog |
| Popups | Report Released Popup |
| Print Views | Radiology Report Print View |
| Export Views | Report Export (PDF/DICOM reference) |

### Blood Bank *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Blood Bank Landing (Inventory Overview) |
| Create Screen | New Donor Registration Screen, New Issue Request Screen |
| Edit Screen | Edit Donor Record Screen, Update Unit Status Screen |
| View Screen | Blood Unit Inventory View, Crossmatch Result View |
| Search Screen | Donor/Unit Search |
| Dashboard | Blood Stock Level Widget (by group) |
| Reports | Stock Report, Wastage Report |
| Settings | Blood Group/Component Master |
| Dialogs | Crossmatch Confirmation Dialog, Low Stock Alert Dialog |
| Popups | Unit Reserved Popup |
| Print Views | Issue Slip Print View |
| Export Views | Blood Bank Register Export |

---

## Pharmacy Domain

### Pharmacy *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Pharmacy Landing (Fulfillment Queue) |
| Create Screen | New Stock Entry Screen |
| Edit Screen | Edit Drug Master Screen |
| View Screen | Prescription Detail View, Stock/Batch View |
| Search Screen | Drug/Prescription Search |
| Dashboard | Stock Level Widget, Expiry Alert Widget |
| Reports | Dispensing Report, Expiry/Wastage Report |
| Settings | Drug Master Configuration |
| Dialogs | Allergy Conflict Dialog, Substitute Drug Dialog, Expired Batch Block Dialog |
| Popups | Dispense Confirmation Popup |
| Print Views | Dispensing Label Print View |
| Export Views | Pharmacy Stock Export |

---

## Support Services Domain

### Ambulance *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Ambulance Landing (Fleet/Dispatch Overview) |
| Create Screen | New Dispatch Request Screen |
| Edit Screen | Edit Trip Details Screen |
| View Screen | Trip Log View |
| Search Screen | Trip/Vehicle Search |
| Dashboard | Ambulance Availability Widget |
| Reports | Trip Log Report, Ambulance Billing Report |
| Settings | Vehicle/Driver Master |
| Dialogs | Dispatch Confirmation Dialog |
| Popups | Vehicle Status Popup |
| Print Views | Trip Receipt Print View |
| Export Views | Ambulance Log Export |

### Hospital Inventory Management *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Inventory Landing (Stock Overview) |
| Create Screen | New Item Screen, New Purchase Order Screen |
| Edit Screen | Edit Item/Stock Screen |
| View Screen | Stock Ledger View, Vendor View |
| Search Screen | Item/Vendor Search |
| Dashboard | Reorder Alert Widget, Stock Value Widget |
| Reports | Stock Movement Report, Vendor Performance Report |
| Settings | Item Category/Unit-of-Measure Master |
| Dialogs | Reorder Confirmation Dialog, Stock Adjustment Dialog |
| Popups | Low Stock Popup |
| Print Views | Purchase Order Print View, Goods Received Note Print View |
| Export Views | Inventory Export (Excel) |

---

## Finance & Billing Domain

### Accounts and Finance
| Screen Type | Screens |
|---|---|
| Main Screen | Finance Landing (Unified Invoice Ledger) |
| Create Screen | Manual Invoice Entry Screen, New Refund Request Screen |
| Edit Screen | Edit/Adjust Invoice Screen |
| View Screen | Invoice Detail View, Payment History View |
| Search Screen | Invoice/Payment Search |
| Dashboard | Daily Collection Widget, Department-wise Income Widget |
| Reports | Income & Expense Report, Insurance Claim Status Report, General Ledger Report |
| Settings | Tax/Discount Rule Configuration, Payment Mode Master |
| Dialogs | Discount Approval Dialog, Refund Validation Dialog |
| Popups | Payment Success/Failure Popup |
| Print Views | Invoice/Receipt Print View |
| Export Views | Financial Statement Export (Excel/PDF) |

---

## Records & Compliance Domain

### Records and Certificates *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Records Landing |
| Create Screen | New Certificate Request Screen |
| Edit Screen | Edit Certificate Draft Screen |
| View Screen | Certificate Detail View, MRD Retrieval View |
| Search Screen | Record/Certificate Search |
| Dashboard | Pending Requests Widget |
| Reports | Certificate Issuance Report |
| Settings | Certificate Template Configuration |
| Dialogs | Approval Dialog (legal certificates) |
| Popups | Request Submitted Popup |
| Print Views | Certificate Print View (Birth/Death/Medical) |
| Export Views | Certificate Export (PDF) |

### E-MRD *(proposed — scope pending clarification)*
| Screen Type | Screens |
|---|---|
| Main Screen | E-MRD Landing (Document Repository) |
| Create Screen | New Document Upload Screen |
| Edit Screen | Metadata Edit Screen |
| View Screen | Document Viewer |
| Search Screen | Document Search (by patient/encounter/date) |
| Dashboard | Digitization Progress Widget |
| Reports | Archival Status Report |
| Settings | Retention Policy Configuration |
| Dialogs | Confirm Archive/Delete Dialog |
| Popups | Upload Complete Popup |
| Print Views | Archived Document Print View |
| Export Views | Bulk Document Export |

---

## Workforce & Administration Domain

### Human Resource Management *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | HR Landing (Staff Directory) |
| Create Screen | New Staff Record Screen, New Leave Request Screen |
| Edit Screen | Edit Staff Profile Screen, Edit Roster Screen |
| View Screen | Staff Profile View, Roster View, Leave Balance View |
| Search Screen | Staff Search |
| Dashboard | Present HR Widget, Leave Pending Widget |
| Reports | Attendance Report, Payroll Summary Report |
| Settings | Role/Designation Master, Leave Policy Configuration |
| Dialogs | Leave Approval Dialog, Roster Conflict Dialog |
| Popups | License Expiry Alert Popup |
| Print Views | Staff ID Card Print View, Payslip Print View |
| Export Views | Payroll Export |

### Activity Log
| Screen Type | Screens |
|---|---|
| Main Screen | Activity Log Landing |
| Create Screen | — (system-generated only) |
| Edit Screen | — (immutable) |
| View Screen | Log Entry Detail View |
| Search Screen | Log Search (by user/date/module/action) |
| Dashboard | Recent Activity Widget |
| Reports | Audit Trail Report |
| Settings | Log Retention Configuration |
| Dialogs | — |
| Popups | — |
| Print Views | Audit Report Print View |
| Export Views | Audit Log Export (CSV) |

### Settings
| Screen Type | Screens |
|---|---|
| Main Screen | Settings Landing |
| Create Screen | New Role Screen, New Master Data Entry Screen |
| Edit Screen | Edit Role/Permission Screen, Edit Master Data Screen |
| View Screen | Role Detail View, Master Data View |
| Search Screen | Settings/Configuration Search |
| Dashboard | — |
| Reports | Configuration Change Report |
| Settings | *(sub-areas: Roles & Permissions, Master Data, System Configuration)* |
| Dialogs | Confirm Permission Change Dialog (access-impact warning) |
| Popups | Save Confirmation Popup |
| Print Views | — |
| Export Views | Configuration Export (backup) |

---

## Engagement Domain

### Programmes and Calendar *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Programmes Landing (Calendar View) |
| Create Screen | New Event/Programme Screen |
| Edit Screen | Edit Event Screen |
| View Screen | Event Detail View |
| Search Screen | Event Search |
| Dashboard | Upcoming Events Widget |
| Reports | Programme Participation Report |
| Settings | Event Category Master |
| Dialogs | RSVP/Registration Dialog |
| Popups | Event Reminder Popup |
| Print Views | Event Schedule Print View |
| Export Views | Calendar Export (iCal) |

### Messages and Notifications
| Screen Type | Screens |
|---|---|
| Main Screen | Notification Center Landing (full view) |
| Create Screen | New Message/Broadcast Screen (admin) |
| Edit Screen | Edit Draft Message Screen |
| View Screen | Message Thread View, Notification Detail View |
| Search Screen | Message Search |
| Dashboard | Unread Count Widget |
| Reports | Notification Delivery Report |
| Settings | Notification Rule/Routing Configuration |
| Dialogs | Send Confirmation Dialog |
| Popups | New Message Toast Popup |
| Print Views | — |
| Export Views | Message Log Export |

---

## Reports & Analytics Domain

### Reports *(proposed)*
| Screen Type | Screens |
|---|---|
| Main Screen | Reports Landing (Category Selector) |
| Create Screen | New Custom Report Screen (report builder) |
| Edit Screen | Edit Saved Report Screen |
| View Screen | Report Result View |
| Search Screen | Report Search (by name/category) |
| Dashboard | Analytics Overview Widget |
| Reports | *(sub-types: Operational, Clinical, Financial, Statutory/Regulatory)* |
| Settings | Report Access Configuration |
| Dialogs | Report Parameter Dialog (date range, filters) |
| Popups | Report Generation Progress Popup |
| Print Views | Report Print View |
| Export Views | Report Export (PDF/Excel/CSV) |

---

## Catalog Summary
21 modules + Dashboard + cross-cutting chrome, each carrying up to 12 screen types — roughly 180+ distinct screens system-wide. Modules marked `(proposed)` (IPD, OT, Lab, Radiology, Blood Bank, Pharmacy, Ambulance, Inventory, Records, E-MRD, HR, Programmes) inherit screen types from enterprise HMIS convention and should be validated against the discovery workshops recommended in [BusinessRequirementsAnalysis.md](BusinessRequirementsAnalysis.md) before wireframing begins.
