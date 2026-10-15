# Appointments

## Module Purpose
Lets reception book OPD patients ahead of time into a consultant's slots, keep the day's diary (tokens, reschedules, cancellations, no-shows, doctor leave), and check booked patients in when they arrive. Backend: `HMS.Modules.Appointments` (schema `appointments`). Web: `/clinical/appointments`. Decision record: [ADR-100](../../DecisionLog.md).

## Turning it on
Appointments is an optional feature per hospital. A Platform Admin enables **Appointments** under the hospital's *Manage features* page; that migrates the `appointments` schema. Then set each consultant's clinic days, visiting hours and (optionally) **Slot Length** and **Patients per Slot** in *Masters → Consultants → Appointment Slots*.

## Functional Requirements
- Book a registered patient (search by name, UHID or phone) or a first-time caller (name and 10-digit phone).
- Pick department, consultant, date (today up to 90 days ahead) and a free slot from the consultant's slot grid.
- Each booking gets an appointment number (`APT-yyyy-nnnnnn`) and a token number.
- Day diary: filter by department, consultant, status and search (name, phone, UHID, appointment number); counts by status; slot grid for the selected consultant, where clicking a free slot books it.
- Reschedule to another slot of the same consultant; cancel with a reason; mark no-show on or after the day.
- Doctor leave: block a whole day or one session over a date range; the bookings that now fall inside it are listed for follow-up.
- Check in on the day: opens Add Visit (registered patient) or New Patient Registration (first-time caller) pre-filled from the booking. Saving the visit marks the appointment Arrived. A "new caller" who turns out to be registered can be checked in against their existing record instead.
- Patient Overview tab shows the patient's upcoming appointments, with a Book shortcut.

## Business Rules
| Rule | Where |
|---|---|
| Slots are the consultant's visiting sessions cut into `AppointmentSlotMinutes` (default 15) pieces; a partial last slot is dropped | `SlotPlanner` |
| Tokens run through the day in time order: slot position × patients per slot + seat; session 2 continues after session 1 | `PlannedSlot.TokenFor` |
| A booking must be on a clinic day, on a real slot start, not in the past, within 90 days, outside any leave block, in a slot with a free seat | `AppointmentService.AssignSlotAsync` |
| Two desks can't take the same seat: unique index on consultant, date, start time and seat for Booked/Arrived rows | `ux_appointments_consultant_date_start_seat` |
| One Booked appointment per patient per consultant per day (callers matched by phone and name) | `HasBookingForSameDayAsync` |
| Only Booked appointments change. No-show only on or after the day; check-in only on the day | `Appointment`, `AppointmentService` |
| Leave blocks never cancel bookings; they are returned to reception to handle | `CreateBlockAsync` |

## Permissions
| Action | Permission |
|---|---|
| View diary, slots, counts, leave | `patient-management.view` or `clinical-care.view` |
| Book | `patient-management.create` |
| Reschedule, cancel, no-show, check in, leave | `patient-management.edit` |

## API Reference
| Method | Path | Purpose |
|---|---|---|
| GET | `/api/v1/appointments` | Paged list (`fromDate`, `toDate`, `consultantId`, `departmentId`, `patientId`, `status`, `search`, `sort`) |
| GET | `/api/v1/appointments/{id}` | One appointment |
| GET | `/api/v1/appointments/slots?consultantId=&date=` | The consultant's slots for a day, with seats booked |
| GET | `/api/v1/appointments/summary?date=` | Counts by status (optional `consultantId`, `departmentId`) |
| POST | `/api/v1/appointments` | Book |
| POST | `/api/v1/appointments/{id}/reschedule` | Move to another slot |
| POST | `/api/v1/appointments/{id}/cancel` | Cancel with a reason |
| POST | `/api/v1/appointments/{id}/no-show` | Mark no-show |
| POST | `/api/v1/appointments/{id}/arrive` | Link the recorded visit (`patientId`, `visitId`) |
| GET | `/api/v1/appointments/blocks` | Leave blocks overlapping a range |
| POST | `/api/v1/appointments/blocks` | Add leave; returns the bookings inside it |
| DELETE | `/api/v1/appointments/blocks/{id}` | Remove leave |

Error codes are `APPOINTMENTS.*` (see `AppointmentErrorCodes`). `SLOT_FULL` and `INVALID_STATUS` return 409; not-found returns 404; the rest 400.

## Not yet included
- SMS confirmation and reminders (needs an SMS provider and DLT-registered templates).
- Online self-booking / patient portal.
- A waiting-room token display screen.

## Change History
| Date | Change |
|---|---|
| 2026-10-15 | First version: booking, slots and tokens, reschedule, cancel, no-show, doctor leave, check-in, patient Overview card. |
