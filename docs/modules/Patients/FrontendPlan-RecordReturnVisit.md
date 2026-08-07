# Frontend Plan: Recording a Returning Patient's Visit

## Is this the right approach? Yes — with one dependency called out below.

The backend already models a patient having many visits — `Patient.Registrations` is a
collection in the domain, not a single field — and two endpoints that expose it were built
and tested earlier:

- `POST /api/v1/patients/{id}/registrations` — records a new visit for an existing patient
- `GET /api/v1/patients/{id}/registrations` — lists a patient's visit history, newest first

No new database schema, no new DTOs, no new backend work. This plan is purely about wiring
the frontend up to what already exists. That's the right shape for this fix: the "first
visit vs. return visit" distinction is already handled by *not* re-registering the patient
and instead adding a second `PatientRegistration` row to the same `Patient` — consistent
with the "derive, don't store" approach already used for `Age` and for telling first-time
registrations apart from later visits (both computed, never stored as a flag).

**The one thing this plan depends on:** a receptionist can only choose "add a visit" instead
of "register a new patient" if they can actually *find* the existing patient first. Today,
patient search is silently broken — `PatientListQuery.cs` on the backend has no `Name` /
`Age` / `Uhid` / `Phone` properties (it only inherits `PagedRequest`), so the four search
boxes the frontend already has send query params that never bind to anything and are
silently dropped. This is a backend-only fix (add the four properties to
`PatientListQuery.cs` and wire matching `WHERE` clauses into `PatientRepository.GetPagedAsync`
— no frontend change needed for it), and it should land *before* or *alongside* this plan,
not as an afterthought — without it, "search for the patient, then add a visit" isn't
actually possible, and the whole point of this feature is to stop receptionists from
re-registering people. Flag to your backend contact if that fix isn't already in flight.

---

## Recommended reception desk workflow

1. Receptionist searches by phone/name/UHID (once the search fix above lands).
2. **Found** → open that patient → click **Record New Visit** → fill just the visit fields
   (Encounter Type, Mode of Arrival, Department, Consultant, etc.) → done. No demographics
   re-entry, no new UHID.
3. **Not found** → "New Patient Registration" (existing flow, unchanged).

---

## 1. `frontend/shared` changes

### `frontend/shared/constants/routes.ts`

Add one line to the existing `patients` block:

```ts
patients: {
  base: '/api/v1/patients',
  byId: (id: string) => `/api/v1/patients/${id}`,
  photo: (id: string) => `/api/v1/patients/${id}/photo`,
  idProof: (id: string) => `/api/v1/patients/${id}/id-proof`,
  registrations: (id: string) => `/api/v1/patients/${id}/registrations`, // new
},
```

### `frontend/shared/api-client/services/patientsApi.ts`

Add two methods to `PatientsApi`, matching the style of every other method in that class:

```ts
async addRegistration(patientId: string, request: PatientRegistrationDetailsRequest): Promise<PatientRegistration> {
  const response = await this.client.post<PatientRegistration>(API_ROUTES.patients.registrations(patientId), request);
  return response.data;
}

async getRegistrations(patientId: string): Promise<PatientRegistration[]> {
  const response = await this.client.get<PatientRegistration[]>(API_ROUTES.patients.registrations(patientId));
  return response.data;
}
```

Add `PatientRegistration` and `PatientRegistrationDetailsRequest` to the existing type-only
import from `'../../dtos'` at the top of the file — both types already exist and already
match these endpoints exactly (`frontend/shared/dtos/patients/patient.ts:13-24` and `:71-79`);
nothing new to define there.

---

## 2. `frontend/web` changes

### New hooks — `frontend/web/src/features/patients/hooks/`

Mirror the existing `usePatientMutations.ts` / `usePatientQuery.ts` conventions exactly
(including the `NetworkError` → mock-store fallback pattern those use — see the note on the
mock store below for whether to bother wiring that up here).

**`useAddPatientRegistrationMutation.ts`** (or added to `usePatientMutations.ts`):

```ts
export function useAddPatientRegistrationMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ patientId, request }: { patientId: string; request: PatientRegistrationDetailsRequest }) =>
      patientsApi.addRegistration(patientId, request),
    onSuccess: (_, { patientId }) => {
      queryClient.invalidateQueries({ queryKey: ['patients', 'detail', patientId] });
      queryClient.invalidateQueries({ queryKey: ['patients', 'registrations', patientId] });
    },
  });
}
```

**`usePatientRegistrationsQuery.ts`** (mirrors `usePatientQuery.ts`):

```ts
export function usePatientRegistrationsQuery(patientId: string | undefined) {
  return useQuery({
    queryKey: ['patients', 'registrations', patientId],
    queryFn: () => patientsApi.getRegistrations(patientId as string),
    enabled: Boolean(patientId),
  });
}
```

### UI piece 1 — "Record New Visit" button + form

**Where:** `PatientViewPage.tsx:44-53`, next to the existing Edit button in the banner.

```tsx
<Button asChild variant="outline" className="...">
  <Link to={`/patients/registration/${patient.id}/new-visit`}>
    <CalendarPlus className="h-4 w-4" />
    Record New Visit
  </Link>
</Button>
```

**The form itself doesn't need to be built from scratch.** The registration-details tab in
`PatientRegistrationForm.tsx` (starting at line 818, `TabsContent value="registration-details"`)
already collects exactly these fields — Encounter Type, Mode of Arrival, Department,
Consultant, Admission Type (conditional on IP/Emergency), Referral, Category — and
`registrationDetailsUiSchema` in `patientRegistrationUiValidation.ts` (exported specifically
so it could be reused like this — see its doc comment) already validates them. A new
`RecordVisitForm.tsx` can be a much smaller component that renders just that one section's
fields against `registrationDetailsUiSchema`, with its own small `useForm`, and submits via
`useAddPatientRegistrationMutation`. Route it at `/patients/registration/:id/new-visit`,
following the existing route-naming convention in `frontend/web/src/routes/routes.tsx`.

Don't re-collect demographics — the whole point is that they don't change per visit.

### UI piece 2 — visit history

**Where:** `PatientDetails.tsx:142`, the `<Section title="Current Registration">` block.

Right now this shows only `patient.currentRegistration` — the single most recent visit, and
there is genuinely no other place in the product where a user can see a patient's full visit
history. Two ways to close that, either is fine:

- Replace "Current Registration" with a small table/list from `usePatientRegistrationsQuery`
  (newest first, the current one visually marked), or
- Keep "Current Registration" as-is and add a collapsible "View all visits" section below it.

Both `PatientRegistration` (already imported via `Patient`) and the new query hook give you
everything needed — `registrationNumber`, `encounterType`, `department`, `consultant`,
`createdAt`, etc.

### Mock store — optional, your call

`mockPatientsStore.ts` is explicitly documented as temporary offline-demo scaffolding
("remove alongside the fallback catches once the backend is live" — see its file header) —
it's reasonable to leave this feature live-API-only and skip extending the mock store, same
as was already decided for photo/ID-proof uploads.

---

## Summary checklist

| # | File(s) | Change |
|---|---------|--------|
| 0 | **Backend** `PatientListQuery.cs` + `PatientRepository.cs` | **Prerequisite** — add Name/Age/Uhid/Phone binding so search actually works (not part of this frontend plan, but blocks the workflow it enables) |
| 1 | `frontend/shared/constants/routes.ts` | Add `registrations(id)` route |
| 2 | `frontend/shared/api-client/services/patientsApi.ts` | Add `addRegistration` / `getRegistrations` methods |
| 3 | `frontend/web/.../hooks/` | New `useAddPatientRegistrationMutation`, `usePatientRegistrationsQuery` |
| 4 | `PatientViewPage.tsx` | "Record New Visit" button |
| 5 | New `RecordVisitForm.tsx` + route | Visit-only form reusing `registrationDetailsUiSchema` |
| 6 | `PatientDetails.tsx` | Visit history list/table |

No new DTOs, no new backend endpoints, no schema changes — everything below the prerequisite
row is pure frontend wiring against what already exists and is already tested.
