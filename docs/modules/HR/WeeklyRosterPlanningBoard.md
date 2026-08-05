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
- Department: **do not print the raw `roster.departmentId`** — a real Department
  directory now exists (see [§6](#6-backend-change-since-this-spec-was-written-department-directory-now-exists)
  below). Resolve it to a name the same way §2 already resolves `StaffId` to a name.
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

## 6. Backend change since this spec was written: Department directory now exists

This lands **after** §1–§5 above were originally written, and changes one of their
assumptions: at the time, there was no Department entity anywhere in the system, so
every DepartmentId field was correctly documented as "no directory exists yet, enter
the GUID directly." **That's no longer true.** A full Department directory now exists
on the backend, and every place that still shows a raw GUID text box for department
should be replaced with a picker, the same way `StaffSelect` already replaced raw
StaffId text boxes.

### 6.1 What's new

- `GET /api/v1/departments?search=` — paginated list, `?search=` matches against both
  `code` and `name` (same `ILike` search behavior `GET /api/v1/users` already has,
  which `StaffSelect` relies on).
- `GET /api/v1/departments/{id}` — single department.
- `POST` / `PUT` / `DELETE /api/v1/departments` — full CRUD (create/update/soft-delete),
  same shape as the existing Shifts endpoints.
- Response shape:
  ```ts
  {
    id: string;       // Guid
    code: string;
    name: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
  }
  ```

### 6.2 What to build — four layers, in order

The Staff picker isn't just one React component — it's a small stack, and Department
needs the same stack built from scratch since (unlike Staff, which reused Identity's
existing Users API layer) there is no existing typed client for `/departments` at all
yet. Build in this order; each step mirrors an existing Shift-module file exactly.

**1. DTOs** — `frontend/shared/dtos/hr/department.ts`, mirroring
`frontend/shared/dtos/hr/shift.ts`:

```ts
/** Mirrors HMS.Modules.HR.Contracts.DepartmentResponse. */
export interface Department {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.HR.Contracts.CreateDepartmentRequest. */
export interface CreateDepartmentRequest {
  code: string;
  name: string;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.UpdateDepartmentRequest — no Code, matching the
 * backend (Code is Department's natural key, set only at creation). */
export interface UpdateDepartmentRequest {
  name: string;
  isActive: boolean;
}

/** Mirrors HMS.Modules.HR.Contracts.DepartmentListQuery. */
export interface DepartmentListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
```

Add `export * from './hr/department';` to `frontend/shared/dtos/index.ts`, right next
to the existing `export * from './hr/shift';` line.

**2. Route constants** — add to `frontend/shared/constants/routes.ts`, next to the
existing `shifts` entry:

```ts
/** Mirrors HMS.Modules.HR.Endpoints.DepartmentsController. */
departments: {
  base: '/api/v1/departments',
  byId: (id: string) => `/api/v1/departments/${id}`,
},
```

**3. Typed API class** — `frontend/shared/api-client/services/departmentsApi.ts`,
copying `shiftsApi.ts` (same file) field-for-field:

```ts
import { API_ROUTES } from '../../constants';
import type { CreateDepartmentRequest, Department, DepartmentListQuery, UpdateDepartmentRequest } from '../../dtos';
import type { PaginationMeta } from '../../types';
import type { HttpClient } from '../httpClient';

export interface PagedDepartments {
  items: Department[];
  meta: PaginationMeta;
}

export class DepartmentsApi {
  constructor(private readonly client: HttpClient) {}

  async getDepartments(query: DepartmentListQuery = {}): Promise<PagedDepartments> {
    const response = await this.client.get<Department[]>(API_ROUTES.departments.base, {
      query: { page: query.page, pageSize: query.pageSize, sort: query.sort, search: query.search, isActive: query.isActive },
    });
    return { items: response.data, meta: response.meta as PaginationMeta };
  }

  async getDepartmentById(id: string): Promise<Department> {
    return (await this.client.get<Department>(API_ROUTES.departments.byId(id))).data;
  }

  async createDepartment(request: CreateDepartmentRequest): Promise<Department> {
    return (await this.client.post<Department>(API_ROUTES.departments.base, request)).data;
  }

  async updateDepartment(id: string, request: UpdateDepartmentRequest): Promise<Department> {
    return (await this.client.put<Department>(API_ROUTES.departments.byId(id), request)).data;
  }

  async deleteDepartment(id: string): Promise<void> {
    await this.client.delete(API_ROUTES.departments.byId(id));
  }
}
```

Then register it in `frontend/web/src/services/apiClient.ts`, next to the existing
`export const shiftsApi = new ShiftsApi(httpClient);` line:

```ts
export const departmentsApi = new DepartmentsApi(httpClient);
```

**4. React components** — now `DepartmentSelect` (copying
`frontend/web/src/components/StaffSelect.tsx`) and `DepartmentName` (copying
`StaffName.tsx`) are straightforward, since they're just consuming the client built
above:

```tsx
// frontend/web/src/components/DepartmentSelect.tsx
import { useQuery } from '@tanstack/react-query';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { departmentsApi } from '../services/apiClient';

interface DepartmentSelectProps {
  id: string;
  value: string;
  onValueChange: (value: string) => void;
  ariaLabel?: string;
  disabled?: boolean;
}

export function DepartmentSelect({ id, value, onValueChange, ariaLabel = 'Department', disabled }: DepartmentSelectProps) {
  const { data } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });

  const options = (data?.items ?? []).map((department) => ({
    value: department.id,
    label: department.name,
    keywords: department.code,
  }));

  return (
    <SearchableSelect
      id={id}
      value={value}
      onValueChange={onValueChange}
      options={options}
      placeholder="Select department…"
      searchPlaceholder="Search by name or code…"
      ariaLabel={ariaLabel}
      disabled={disabled}
    />
  );
}
```

```tsx
// frontend/web/src/components/DepartmentName.tsx
import { useQuery } from '@tanstack/react-query';
import { departmentsApi } from '../services/apiClient';

export function DepartmentName({ departmentId }: { departmentId: string }) {
  const { data } = useQuery({
    queryKey: ['departments', 'select-list'],
    queryFn: () => departmentsApi.getDepartments({ pageSize: 100, isActive: true }),
  });

  const department = data?.items.find((item) => item.id === departmentId);
  return department
    ? <>{department.name}</>
    : <span className="font-mono text-xs text-muted-foreground">{departmentId.slice(0, 8)}…</span>;
}
```

### 6.3 Where to wire it in — exact locations

| File | Current state | Change |
|---|---|---|
| `frontend/web/src/features/weeklyRosters/components/WeeklyRosterForm.tsx` (lines 61–68) | Raw `<Input>` + placeholder GUID + "No department directory exists yet" caption | Replace with a `Controller`-wrapped `<DepartmentSelect>`, same pattern as `staffId` in `ShiftAssignmentForm.tsx` lines 63–71. Delete the "No department directory exists yet" caption — it's no longer true. |
| `frontend/web/src/features/shiftAssignments/components/ShiftAssignmentForm.tsx` (lines 83–88) | Same raw `<Input>` + same caption | Same replacement. |
| Weekly Roster planning-board header (§4.1 above) | N/A yet — being built | Use `<DepartmentName departmentId={roster.departmentId} />` directly, no raw id. |
| `WeeklyRosterViewPage.tsx` / `WeeklyRosterTable.tsx` (today's existing pages, not just the new planning board) | Currently print `roster.departmentId` raw, same as the planning board would have | Same `<DepartmentName>` swap — worth fixing here too since it's the same bug, already live in production today. |

### 6.4 Not required, but worth doing while in this area

A "Manage Departments" admin screen (list/create/edit, mirroring the existing Shift
Management pages) doesn't exist yet either. The backend fully supports it — this spec
doesn't require building it, but without one, whoever creates the *first* department
still has to do it via Swagger/API rather than the UI. Flagging it, not scoping it in.
