# Weekly Roster Planning Board — Frontend Integration Spec

Audience: frontend developer implementing the redesigned Weekly Roster page. This is a
**UI composition spec only** — it does not add, remove, or change any backend API,
entity, or database table. Every data point below is already returned by an endpoint
that exists today. One backend change *is* included ([§5](#5-backend-change-shipped-with-this-spec-isnightshift-validation)) but it is
validation-only and does not change any response shape or add any field.

## 1. Goal

Turn the Weekly Roster detail page from a header-only summary into the primary
planning screen: a Shift × Day-of-week matrix showing every staff assignment for the
selected week, with Publish/Copy actions and week navigation, built entirely from data
the existing HR endpoints already expose.

## 2. Data sources — no new entities

| Data | Endpoint | Notes |
|---|---|---|
| Roster header (week, department, published state, dates) | `GET /api/v1/weekly-rosters/{id}` | Existing, used today by `WeeklyRosterViewPage` |
| Shift catalog (id → name/timing/night-flag) | `GET /api/v1/shifts?pageSize=100` | Used to label rows and resolve `shiftId` → shift name |
| Assignments for the week | `GET /api/v1/shift-assignments?pageSize=100` (+ client-side filter, see [§3](#3-the-real-constraint-shiftassignment-has-no-weeklyrosterid)) | See constraint below |

No dedicated `Staff` module exists yet — `ShiftAssignment.StaffId` is actually an
Identity `User.Id` — but staff names **are** already resolvable today: reuse the
existing `<StaffName staffId={...} />` component
(`frontend/web/src/components/StaffName.tsx`), which looks the id up against the
same cached `GET /api/v1/users` list `StaffSelect` populates and renders
`"{firstName} {lastName}"` (falling back to a truncated id only if the user isn't
found). This is already used in the Shift Assignments table today — reuse it here
rather than showing a raw id.

## 3. The real constraint: `ShiftAssignment` has no `WeeklyRosterId`

This is the load-bearing fact for this whole page, and it's why the matrix can't be a
single server call.

- `ShiftAssignment` (`Contracts/ShiftAssignmentContracts.cs`) has `StaffId`,
  `DepartmentId`, `ShiftId`, `RosterDate`, `Status`, `Remarks` — **no
  `WeeklyRosterId` field, no FK to `WeeklyRoster` at all.**
- `ShiftAssignmentListQuery` and `WeeklyRosterListQuery` both inherit only the generic
  `PagedRequest` (`Page`, `PageSize`, `Sort`, `Search`) — **neither has a
  `DepartmentId` or date-range query parameter.**
- Per this task's constraints, no new API/query parameter may be added.

So the only way to associate assignments with a given `WeeklyRoster` is:

```
assignment belongs to roster  ⟺  assignment.departmentId === roster.departmentId
                               AND roster.weekStartDate <= assignment.rosterDate
                               AND assignment.rosterDate <= roster.weekStartDate + 6 days
```

**How to fetch it in practice:**

1. `GET /api/v1/weekly-rosters/{id}` → get `weekStartDate` + `departmentId`.
2. `GET /api/v1/shift-assignments?pageSize=100&search=` → fetch a page of assignments
   (see caveat below), then filter client-side using the rule above.
3. `GET /api/v1/shifts?pageSize=100` → build a `Map<shiftId, ShiftResponse>` once
   (cache it — the shift catalog is small and rarely changes) to resolve each
   assignment's shift name/row.

**Caveat to flag to the team, not to silently work around:** `GET /shift-assignments`
is paginated with no department/date filter, so "fetch page 1 of 100" is a
best-effort approach that can miss assignments once the table grows past one page.
For now (early-stage data volume) this is acceptable; if it becomes a real gap, the
fix is a backend change (add `DepartmentId`/date-range filtering to
`ShiftAssignmentListQuery`, or add `WeeklyRosterId` to `ShiftAssignment`) — explicitly
out of scope for this task since "no new APIs" was a hard constraint.

## 4. Page composition

### 4.1 Header (reuse existing design-system `Card`/`Badge`/`Button`, same as today's `WeeklyRosterViewPage`)

- Back button → `/admin/hr/weekly-rosters` (unchanged).
- Title: `Week of {weekStartDate}`.
- Department: resolve `roster.departmentId` the same way the current page already
  does today (it currently just prints the raw id — keep that; a Department module
  lookup is out of scope here).
- `Draft` / `Published` badge — already implemented (`roster.published`).
- Created date — already implemented (`roster.createdAt`).
- Published date — already implemented (`roster.publishedDate`, shown when present).

### 4.2 Publish button

No change needed beyond what exists: `WeeklyRosterViewPage.tsx` already renders
Publish only `{!roster.published && (...)}` and calls
`usePublishWeeklyRosterMutation()` → `POST /api/v1/weekly-rosters/{id}/publish`. Keep
this exact behavior; it already satisfies "disabled if already published" (by hiding
rather than disabling — either satisfies the spec's intent, no change required).

### 4.3 Copy button

Also already implemented: opens `CopyWeeklyRosterDialog`, prompts for
`targetWeekStartDate`, calls `useCopyWeeklyRosterMutation()` →
`POST /api/v1/weekly-rosters/{id}/copy` with `{ targetWeekStartDate }`, then navigates
to the new roster's id on success. No change required — just keep it visible on the
new planning-board layout.

### 4.4 Week navigation (Previous / Current / Next)

There is no "get roster by department + week" endpoint. Build navigation from
`GET /api/v1/weekly-rosters?pageSize=100`, filtered client-side by
`departmentId === roster.departmentId`, sorted by `weekStartDate`. From that sorted
list:
- **Previous week** = the entry with the largest `weekStartDate` less than the
  current roster's `weekStartDate`.
- **Next week** = the entry with the smallest `weekStartDate` greater than it.
- **Current week** = compute this week's Monday client-side and look for a matching
  entry in the same list; if none exists, disable the button (there is no roster to
  navigate to — do not auto-create one, that's out of scope).

Same pagination caveat as §3 applies once roster counts grow past one page.

### 4.5 The matrix

Rows: `Morning`, `Evening`, `Night` — derived from each assignment's resolved
`Shift.name` (or `Shift.isNightShift`/timing if you need a heuristic mapping from
arbitrary shift names to these three buckets; confirm the mapping rule with the team
since `Shift` has no "category" field, only a name and a night-shift flag).
Columns: Monday–Sunday, i.e. `weekStartDate` through `weekStartDate + 6`.

For each `(shiftRow, day)` cell: list every filtered assignment where
`shift maps to shiftRow` and `assignment.rosterDate === that day`, rendering
`<StaffName staffId={assignment.staffId} />` (see §2) per entry, multiple entries
stacked. Empty cell → render `—` or `Unassigned`, never blank.

### 4.6 Reserved (not implemented) space

Add empty/placeholder regions — e.g. a slim strip per day column or per cell — for:
Leave indicators, Holiday indicators, Staff availability, Swap requests, Conflict
warnings. Leave them visually inert (no data wiring) so a future phase can populate
them without a layout change.

### 4.7 Explicitly do NOT implement

Automatic scheduling, drag-and-drop, conflict validation, leave validation, holiday
logic, overtime logic, approval workflow. None of these have backend support today;
adding client-side versions would create logic that has to be thrown away or
reconciled later.

## 5. Backend change shipped with this spec: IsNightShift validation

Unrelated to the planning board, but shipped in the same backend change set: creating
or updating a `Shift` now rejects a request where `isNightShift` disagrees with the
times:

```
isNightShift must equal (endTime < startTime)
```

- `isNightShift: true` is now only accepted when `endTime < startTime` (the shift
  crosses midnight — e.g. 22:00–06:00).
- `isNightShift: false` is now only accepted when `endTime >= startTime` (e.g.
  09:00–17:00).
- This closes it in **both directions**: a same-day shift mislabeled as "night" (e.g.
  09:00–17:00 saved with `isNightShift: true`) is rejected, and — just as
  important — a genuine overnight shift (e.g. 22:00–06:00) can no longer be edited to
  `isNightShift: false` while keeping the same crossing times. That second case was
  the actual bug report: creating a 22:00–06:00 shift as Night worked correctly, but
  editing it afterward to toggle Night *off* without touching the times used to
  succeed silently, leaving an internally-contradictory shift (crosses midnight, but
  not flagged as night). Both directions now return HTTP 400 with a validation error
  on field `IsNightShift`.

**Frontend impact — confirmed bug, action required:** the existing Shift create/edit
form already sends both fields, and correctly *receives* the `400` with a validation
error on field `IsNightShift` when the user picks a contradictory combination — but it
currently fails **silently**. Root cause, confirmed by reading the code:

`frontend/web/src/features/shifts/components/ShiftForm.tsx`'s `useEffect` (around
line 43) already maps every server-side field error onto react-hook-form's error
state via `setError(fieldName, ...)` — this part works, and does get called for
`isNightShift`. But every *other* field in that form (`code`, `name`, `startTime`,
`endTime`, `breakMinutes`, `graceMinutes`) has a rendering line right under its input:

```tsx
{errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
```

The "Night shift" `Switch` block (around line 105–112) is missing this line entirely
— so the error is set in form state but never displayed. That's the exact symptom
reported: submitting a contradictory combination (e.g. 22:00–06:00 with Night toggled
off) does nothing visible — no save, no error, no explanation.

**Fix:** add one line, matching the existing pattern exactly, right after the
`isNightShift` `Controller`/`Switch` block in `ShiftForm.tsx`:

```tsx
{errors.isNightShift && <p className="text-sm text-destructive">{errors.isNightShift.message}</p>}
```

No new field, no response shape change, no contract change, no new component — just
this one missing render line, in the same file, following the same pattern already
used for every other field.
