# Frontend Follow-Up: Patients Module Backend Fixes

## Purpose

The backend Patients module was audited and had zero test coverage plus three real gaps, all now fixed on branch `fix/actor-id-audit-fields` (backend only — see `PatientsController.cs`, `PatientService.cs`, `CreatePatientRequestValidator.cs`/`UpdatePatientRequestValidator.cs`). This document tells the frontend dev exactly what needs to change on the web app (`frontend/web`) and shared package (`frontend/shared`) to stay in sync, and flags one new capability that has **zero frontend support today**.

Three items below are **required** — without them, the app either breaks (a form the server now rejects) or a real backend capability stays invisible to users. One item is **recommended** (UX polish, not a compatibility break). One is **cleanup**.

---

## 1. REQUIRED — Fix phone number validation to match the server

**What changed on the backend:** `PrimaryPhone`, `AlternatePhone`, and `EmergencyContactPhone` used to accept a purely symbolic string like `"----------"` (the regex allowed zero digits). The server now rejects that with a 400 validation error — the regex requires at least one digit.

**Where the frontend currently disagrees:**

`frontend/shared/validation/patients/patientRegistrationUiValidation.ts:24`

```ts
const phonePattern = /^[0-9+\-() ]*$/;
```

This one pattern is reused for **every** phone-shaped field in the module — `primaryPhoneSchema`, `phoneEntrySchema` (used for `additionalPhones`), `emergencyContactPhone`, and the optional referral-column `contactNumber` (line 96) — and it's shared by both forms, since `patientEditUiSchema` is built from the same `demographicsUiSchema` object (lines 172–180). One fix in this file covers the New Patient Registration wizard and the Edit form.

**Why this matters today:** a receptionist can currently type `"---"` or `"()()"` into any phone field, the client says it's valid, the form submits, and the server now bounces it with a 400 — a confusing dead end since the client thought everything was fine.

**Fix:**

```ts
// Requires at least one digit (via lookahead) so a symbol-only string like "----------"
// — which the character class alone accepts, since '*' permits zero digits — is
// rejected; still permits the digits/+/-/()/space characters a real phone number uses.
const phonePattern = /^(?=.*[0-9])[0-9+\-() ]*$/;
```

That's the only line that needs to change — every place that imports `phonePattern` picks up the fix automatically. Match backend wording if you want the error message to be equally clear: server-side it's now `"Phone number must contain at least one digit and only digits/+/-/()/spaces."` (currently the client message is just `"Enter a valid phone number"`, which is fine but vaguer).

---

## 2. REQUIRED — Add support for recording a returning patient's next visit

**What's new on the backend:** two endpoints that didn't exist before:

- `POST /api/v1/patients/{id}/registrations` — records a new encounter/visit for an existing patient
- `GET /api/v1/patients/{id}/registrations` — lists every encounter/visit a patient has had, newest first

**Why this exists:** previously, the *only* way a patient ever got a registration/encounter was the one created automatically during the "New Patient Registration" wizard's single combined submit. There was no way — anywhere in the API — for a returning patient's second, third, etc. visit to ever be recorded. The domain model already supported a patient having many registrations (`Patient.Registrations` is a collection), the API just never exposed a way to add to it. This is now fixed on the backend, but **the frontend has no code path to reach it at all**:

- `PatientsApi` (`frontend/shared/api-client/services/patientsApi.ts`) has exactly 7 methods — `getPatients`, `getPatientById`, `createPatient`, `updatePatient`, `deletePatient`, `uploadPhoto`, `uploadIdProof` — none related to registrations.
- `API_ROUTES.patients` (`frontend/shared/constants/routes.ts:28-33`) has no `registrations` entry.
- `PatientViewPage.tsx` has exactly one action button (Edit) — no "Record New Visit"/"New Encounter" button anywhere.
- `PatientDetails.tsx` shows a section literally titled **"Current Registration"** (singular) — the most recent visit only. There is no visit-history list, table, or accordion anywhere in the UI.

The good news: **no new DTOs are needed.** The exact shapes already exist and just need to be reused:

- `PatientRegistrationDetailsRequest` (`frontend/shared/dtos/patients/patient.ts:71-79`) — already the request body shape used inside `CreatePatientRequest.registration`; reuse it as-is for the new POST body.
- `PatientRegistration` (`frontend/shared/dtos/patients/patient.ts:13-24`) — already the shape of `Patient.currentRegistration`; reuse it as the response type for both the POST result and the GET list.

### 2a. `frontend/shared` changes

**`frontend/shared/constants/routes.ts`** — add one line to the existing `patients` block:

```ts
patients: {
  base: '/api/v1/patients',
  byId: (id: string) => `/api/v1/patients/${id}`,
  photo: (id: string) => `/api/v1/patients/${id}/photo`,
  idProof: (id: string) => `/api/v1/patients/${id}/id-proof`,
  registrations: (id: string) => `/api/v1/patients/${id}/registrations`, // new
},
```

**`frontend/shared/api-client/services/patientsApi.ts`** — add two methods, mirroring the existing style exactly:

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

(Add `PatientRegistration` and `PatientRegistrationDetailsRequest` to the existing type-only import from `'../../dtos'` at the top of the file.)

### 2b. `frontend/web` changes

**New hooks**, mirroring the conventions already in `hooks/usePatientMutations.ts` / `hooks/usePatientQuery.ts` (including whatever mock-store fallback pattern those use on `NetworkError` — decide whether `mockPatientsStore.ts` needs a matching `registrations` array added, or whether it's acceptable for this feature to be live-API-only for now since it's brand new):

- `useAddPatientRegistrationMutation` — POST, invalidate the patient query + a new registrations-list query key on success.
- `usePatientRegistrationsQuery(patientId)` — GET, list.

**UI — two things need to exist somewhere, exact placement is your call:**

1. **A way to record a new visit.** Simplest option: a "Record New Visit" button next to the existing Edit button in `PatientViewPage.tsx` (banner area, `PatientViewPage.tsx:44-53`), opening a drawer/dialog with just the *registration details* fields — Encounter Type, Mode of Arrival, Department, Consultant, Admission Type (conditional on IP/Emergency), Referral Source, Category. These fields already exist as a self-contained tab in `PatientRegistrationForm.tsx` (the `registration-details` tab) and as `registrationDetailsUiSchema` in the validation file — the new form doesn't need to reinvent them, and shouldn't re-collect demographics that don't change per visit.
2. **A way to see visit history.** `PatientDetails.tsx`'s "Current Registration" section needs a sibling — either replace it with a small table/list of all registrations (newest first, from the new `getRegistrations` call) with the current one visually marked, or keep "Current Registration" as-is and add a collapsible "View all visits" section below it. Either way, this is the only place a user will ever be able to see a patient's full visit history — right now it's simply not visible anywhere in the product.

No new route is strictly required if you go the drawer/modal route. If you'd rather have a dedicated page, `/patients/registration/:id/new-visit` would follow the existing route-naming convention in `frontend/web/src/routes/routes.tsx`.

### 2c. Mobile

`frontend/mobile/src/features/patients/` is an empty scaffold (just a `.gitkeep`) — Patients isn't built on mobile at all yet. No action needed now, but **whoever eventually builds mobile Patients should include the registrations endpoints in the v1 scope**, not treat them as a later bolt-on — otherwise mobile will ship with the same "can't record a second visit" gap the web app just had.

---

## 3. RECOMMENDED — Tighten client-side file upload validation (not a breaking change)

**What changed on the backend:** photo/ID-proof uploads now check the file's actual bytes (magic-byte/signature check) against JPEG/PNG (photo) or JPEG/PNG/PDF (ID proof), on top of the existing extension and size checks. A renamed non-image file that used to slip through is now rejected server-side.

**This does not break anything** — legitimate files upload exactly as before. But the current frontend does **zero** client-side validation of the file's actual type or size before sending it:

`PatientDocumentUpload.tsx:23-29` (and the equivalent in `DocumentUploadStaging.tsx`, used during the registration wizard):

```ts
function handlePhotoChange(event: React.ChangeEvent<HTMLInputElement>) {
  const file = event.target.files?.[0];
  if (file) {
    photoMutation.mutate({ id: patientId, file });
  }
  event.target.value = '';
}
```

The only client-side gate today is the HTML `accept="image/jpeg,image/png"` attribute — which is a picker *hint*, not a real validator (bypassable via drag-and-drop or "All Files"). There's no client-side size check at all; "max 5MB" is currently just label text.

**Recommendation (optional polish, not required for correctness):**
- Add a quick `file.size > 5 * 1024 * 1024` check before calling the mutation, so oversized files fail instantly instead of after a network round trip.
- Confirm the error surfaced from a rejected upload reads well now that the server message is more specific (`"The uploaded file is not a valid JPG or PNG image."` / `"...JPG, PNG, or PDF."` via `PatientErrorCodes.InvalidFile`) — no code change needed here if the existing error-toast plumbing already surfaces `ApiError.message`, just worth eyeballing once.

---

## 4. Cleanup — stale build artifact (unrelated to the fixes above, but worth doing while in this area)

`frontend/shared/dist/validation/patients/patientValidation.js` (and its `.d.ts`) is leftover compiled output from a source file that no longer exists — there is no `frontend/shared/validation/patients/patientValidation.ts` in the repo, and `frontend/shared/validation/index.ts` only exports the current `patientRegistrationUiValidation.ts`. It isn't imported anywhere, so it's harmless, but it'll confuse anyone who greps `dist/` looking for the "real" validation source. A clean `npm run build` in `frontend/shared` from a wiped `dist/` will drop it; no source change needed.

---

## Summary checklist

| # | Priority | File(s) | Change |
|---|----------|---------|--------|
| 1 | **Required** | `frontend/shared/validation/patients/patientRegistrationUiValidation.ts` | Fix `phonePattern` regex to require ≥1 digit |
| 2a | **Required** | `frontend/shared/constants/routes.ts`, `frontend/shared/api-client/services/patientsApi.ts` | Add `registrations` route + `addRegistration`/`getRegistrations` API methods |
| 2b | **Required** | `frontend/web/src/features/patients/hooks/*`, `PatientViewPage.tsx`, `PatientDetails.tsx` | New mutation/query hooks + "Record New Visit" UI + visit-history UI |
| 2c | FYI only | `frontend/mobile/src/features/patients/` | Include registrations in scope whenever mobile Patients gets built |
| 3 | Recommended | `PatientDocumentUpload.tsx`, `DocumentUploadStaging.tsx` | Client-side file size pre-check (UX only) |
| 4 | Cleanup | `frontend/shared/dist/` | Delete/regenerate stale `patientValidation.js` |

No new DTOs are needed anywhere — `PatientRegistrationDetailsRequest` and `PatientRegistration` already exist in `frontend/shared/dtos/patients/patient.ts` and cover both new endpoints exactly.
