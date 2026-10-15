# HMS.Modules.Appointments

Appointment booking for OPD consultants: book ahead into a consultant's time slots, reschedule,
cancel, mark no-show, block a consultant's leave days, and check a booked patient in when they
arrive (which records a normal OPD visit through the Patients module).

Layout: `Domain/`, `Application/`, `Infrastructure/`, `Contracts/`, `Endpoints/`.

- Schema: `appointments` (tables `appointments`, `consultant_blocks`).
- Feature key: `appointments` (optional, per hospital).
- Permissions: reuses `patient-management.*` (reception); `clinical-care.view` can also read.
- Slots come from the consultant master: clinic days, one or two visiting sessions, slot length
  and patients per slot. See `docs/modules/Appointments/Appointments.md`.
