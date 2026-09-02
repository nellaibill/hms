import type { Allergy, AllergyInput, CreatePatientRequest, Patient, PatientRegistrationCoreUiFormValues, PatientRegistrationUiFormValues } from '@hms/shared';
import { ArrowLeft, UserPlus } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useToast } from '@/components/ui/toast-context';
import { UnsavedChangesDialog } from '@/components/UnsavedChangesDialog';
import { useUnsavedChangesGuard } from '@/hooks/useUnsavedChangesGuard';
import { RequirePermission } from '../../features/auth/RequirePermission';
import {
  PatientRegistrationForm,
  useAddPatientAllergyMutation,
  useCreatePatientMutation,
  useCreatePatientVisitMutation,
  useRemovePatientAllergyMutation,
  useUpdatePatientMutation,
  useUploadPatientIdProofMutation,
  useUploadPatientPhotoMutation,
  type StagedDocuments,
} from '../../features/patients';
import { toDisplayError } from '../../features/patients/apiErrorDisplay';
import {
  toAllergyRequest,
  toBackendGender,
  toCreatePatientVisitRequest,
  toEmergencyContactRequest,
  toModeOfArrivalRequest,
} from '../../features/patients/bridging';

/** The wizard's own Allergy Details fields (primary + additional rows), as AddAllergyRequest-shaped rows — shared by toRequest (inline on create) and syncAllergies (remove/re-add on update, see its own doc comment for why). */
function buildAllergyRequests(values: PatientRegistrationCoreUiFormValues): AllergyInput[] {
  return values.hasKnownAllergy
    ? [
        toAllergyRequest({ allergyCategory: values.allergyCategory, allergySpecify: values.allergySpecify, allergySeverity: values.allergySeverity }),
        ...values.additionalAllergies
          .filter((row) => row.allergyCategory && row.allergySeverity)
          .map((row) => toAllergyRequest(row)),
      ]
    : [];
}

/**
 * Builds the real CreatePatientRequest from Patient Information + Contact Information +
 * Medical Information (the three tabs wired to the backend) plus the separately-staged
 * `documents` (idProofType/idProofNumber live there, not on the RHF form — see
 * DocumentUploadStaging's own doc comment). Registration Details isn't part of this payload —
 * it's sent as its own CreatePatientVisitRequest once the patient id is known, see
 * handleSubmit's toCreatePatientVisitRequest call.
 */
function toRequest(values: PatientRegistrationCoreUiFormValues, documents: StagedDocuments): CreatePatientRequest {
  const allergies = buildAllergyRequests(values);

  const emergencyContacts = [
    toEmergencyContactRequest({
      relationship: values.emergencyContactRelationship,
      name: values.emergencyContactName,
      phone: values.emergencyContactPhone,
    }),
    ...values.additionalEmergencyContacts.map((row) => toEmergencyContactRequest(row)),
  ];

  return {
    title: values.title,
    firstName: values.firstName,
    lastName: values.lastName,
    dateOfBirth: values.dateOfBirth,
    gender: toBackendGender(values.gender),
    bloodGroup: values.bloodGroup,
    maritalStatus: values.maritalStatus,

    primaryPhone: values.primaryPhone.number,
    secondaryPhone: values.secondaryPhone || undefined,
    email: values.email || undefined,
    profession: values.profession || undefined,

    idProofType: documents.idProofType || undefined,
    // Trimmed here, not just at validation time — idProofNumberValidationError (which gates
    // the Save button) trims before checking the format, but the raw untrimmed value was what
    // actually got sent, so a stray leading/trailing space could pass client-side validation
    // and still fail the backend's own (untrimmed) pattern check. Same class of bug as the
    // form-wide getValues() fix above, just for this field, which lives outside the RHF form.
    idProofNumber: documents.idProofNumber.trim() || undefined,

    ...toModeOfArrivalRequest(values.arrivalSource),

    address: {
      addressLine1: values.addressLine1,
      addressLine2: values.addressLine2 || undefined,
      addressLine3: values.addressLine3 || undefined,
      stateId: values.state,
      districtId: values.district,
      pincode: values.pincode,
    },
    allergies,
    emergencyContacts,
  };
}

export default function PatientRegistrationCreatePage() {
  const navigate = useNavigate();
  const createMutation = useCreatePatientMutation();
  const updateMutation = useUpdatePatientMutation();
  const photoMutation = useUploadPatientPhotoMutation();
  const idProofMutation = useUploadPatientIdProofMutation();
  const addAllergyMutation = useAddPatientAllergyMutation();
  const removeAllergyMutation = useRemovePatientAllergyMutation();
  const createVisitMutation = useCreatePatientVisitMutation();
  const { toast } = useToast();

  // Set once "Save and proceed to Registration" (Medical Information tab) successfully saves
  // the patient — lets a second click of that button (after going back and editing tabs 1-3)
  // update instead of re-create, and lets the final Registration Details submit skip creating
  // again.
  const [savedPatient, setSavedPatient] = useState<Patient | null>(null);
  // JSON of the toRequest() payload exactly as it was last sent (create or update) — compared
  // against the current payload before any resave to decide whether tabs 1-3 actually changed.
  // Without this, every resave (including a "Register Patient" click where the user made zero
  // edits after the mid-wizard save) would unconditionally PUT the patient, bumping
  // updated_at/updated_by in the database for a write that changed nothing — a real record of
  // "who touched this and when" needs that to only happen on an actual edit.
  const [lastSavedPayloadJson, setLastSavedPayloadJson] = useState<string | null>(null);

  const [isDirty, setIsDirty] = useState(false);
  const { showUnsavedDialog, confirmDiscard, cancelDiscard, markSaved } = useUnsavedChangesGuard(isDirty);

  function uploadStagedDocuments(patientId: string, documents: StagedDocuments) {
    if (documents.photo) {
      photoMutation.mutate(
        { id: patientId, file: documents.photo },
        {
          onSuccess: () => toast({ title: 'Photo saved', description: `${documents.photo!.name} was uploaded.`, variant: 'success' }),
          onError: () =>
            toast({
              title: 'Photo not saved',
              description: "The patient was registered, but the photo upload failed. Add it from the patient's Edit page.",
              variant: 'error',
            }),
        },
      );
    }
    if (documents.idProofFile) {
      idProofMutation.mutate(
        { id: patientId, file: documents.idProofFile },
        {
          onSuccess: () => toast({ title: 'ID proof saved', description: `${documents.idProofFile!.name} was uploaded.`, variant: 'success' }),
          onError: () =>
            toast({
              title: 'ID proof not saved',
              description: "The patient was registered, but the ID proof upload failed. Add it from the patient's Edit page.",
              variant: 'error',
            }),
        },
      );
    }
  }

  // UpdatePatientRequest has no `allergies` field at all — the backend only supports adding
  // or removing one allergy row at a time (the same endpoints PatientEditForm's own allergy
  // list uses). Without this, any edit to the wizard's Allergy Details fields made *after* the
  // patient's first save would silently never reach the server: it'd ride along inside the
  // update PUT body, and the backend would just ignore the unknown property. Reconciles by
  // removing every allergy the patient currently has and re-adding whatever the form now says
  // — simpler and safer than diffing individual rows (there's no stable identity to match a
  // form row against a saved one by), and the wizard only ever holds a handful of rows so the
  // extra round-trips are cheap. Not atomic across the remove/add calls (same class of
  // partial-failure risk already accepted for the Documents upload calls elsewhere in this
  // flow) — returns the latest patient snapshot if any call was made, so the caller can fold
  // the resulting allergies into its own patient state; null when there was nothing to sync.
  async function syncAllergies(patientId: string, desired: AllergyInput[], existing: Allergy[]): Promise<Patient | null> {
    let latest: Patient | null = null;
    for (const allergy of existing) {
      latest = await removeAllergyMutation.mutateAsync({ id: patientId, allergyId: allergy.id });
    }
    for (const request of desired) {
      latest = await addAllergyMutation.mutateAsync({ id: patientId, request });
    }
    return latest;
  }

  // Passed to PatientRegistrationForm as onSaveAndProceed — called after the form validates
  // Patient/Contact/Medical Information itself. Returns whether it succeeded so the form knows
  // whether to advance to Registration Details; a failure is surfaced via the apiError prop
  // below (derived from whichever mutation just failed), same display path final submit uses.
  async function handleSaveAndProceed(values: PatientRegistrationCoreUiFormValues, documents: StagedDocuments): Promise<boolean> {
    try {
      const payload = toRequest(values, documents);
      if (savedPatient) {
        const payloadJson = JSON.stringify(payload);
        if (payloadJson === lastSavedPayloadJson) {
          // Nothing on tabs 1-3 actually changed since the last save — skip the no-op PUT.
          return true;
        }
        const patient = await updateMutation.mutateAsync({
          id: savedPatient.id,
          request: { ...payload, rowVersion: savedPatient.rowVersion },
        });
        const withAllergies = await syncAllergies(patient.id, payload.allergies, savedPatient.allergies);
        setSavedPatient(withAllergies ?? patient);
        setLastSavedPayloadJson(payloadJson);
        return true;
      }
      const patient = await createMutation.mutateAsync(payload);
      setSavedPatient(patient);
      setLastSavedPayloadJson(JSON.stringify(payload));
      uploadStagedDocuments(patient.id, documents);
      return true;
    } catch {
      return false;
    }
  }

  // Already saved via "Save and proceed to Registration" — don't create the patient again
  // (that would 409 as a duplicate). Instead, re-send tabs 1-3 as an update first *only if
  // they actually changed* since the last save (see lastSavedPayloadJson) — the user may have
  // gone back to Patient/Contact/Medical Information and edited something (e.g. fixing a
  // mistyped ID proof number) after that first save without revisiting Medical Information's
  // own Save button, and that edit needs to still reach the server. But when nothing changed
  // (the common case — Register Patient clicked right after Registration Details, with no
  // detour back to earlier tabs), skip the PUT entirely rather than sending a no-op update
  // that would still bump updated_at/updated_by for a write that changed nothing.
  // Unlike uploadStagedDocuments's fire-and-forget pattern, recording the visit is awaited and
  // its failure keeps the user on the form (apiError prop) instead of navigating past a lost
  // visit record — Registration Details is the whole point of this tab, so failing to save it
  // shouldn't look like success.
  async function handleSubmit(values: PatientRegistrationUiFormValues, documents: StagedDocuments) {
    if (savedPatient) {
      const payload = toRequest(values, documents);
      const payloadJson = JSON.stringify(payload);
      let patientId = savedPatient.id;
      try {
        if (payloadJson !== lastSavedPayloadJson) {
          const patient = await updateMutation.mutateAsync({
            id: savedPatient.id,
            request: { ...payload, rowVersion: savedPatient.rowVersion },
          });
          await syncAllergies(patient.id, payload.allergies, savedPatient.allergies);
          setLastSavedPayloadJson(payloadJson);
          patientId = patient.id;
        }
        await createVisitMutation.mutateAsync({ id: patientId, request: toCreatePatientVisitRequest(values.registration) });
      } catch {
        // Surfaced via the existing apiError prop — stay on the form.
        return;
      }
      markSaved();
      navigate(`/patients/registration/${patientId}`);
      return;
    }

    // Fallback for reaching Registration Details without ever clicking "Save and proceed"
    // (e.g. jumping straight to a later tab via its header) — create now, same flow as before
    // this change.
    try {
      const patient = await createMutation.mutateAsync(toRequest(values, documents));
      // Set even though this page is about to navigate away: if the visit call below fails,
      // the user stays on the form, and a retry must go through the savedPatient branch above
      // (skip the no-op re-create, just retry the visit) instead of re-POSTing the patient and
      // hitting a 409 duplicate.
      setSavedPatient(patient);
      setLastSavedPayloadJson(JSON.stringify(toRequest(values, documents)));
      uploadStagedDocuments(patient.id, documents);
      await createVisitMutation.mutateAsync({ id: patient.id, request: toCreatePatientVisitRequest(values.registration) });
      markSaved();
      navigate(`/patients/registration/${patient.id}`);
    } catch {
      // Surfaced via the existing apiError prop — stay on the form.
    }
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link
          to="/patients/registration"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to registration
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <UserPlus className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">New Patient Registration</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Capture demographics, contacts, and encounter details. A UHID and registration number are assigned automatically.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <RequirePermission permission="patient-management.create">
        <PatientRegistrationForm
          isSubmitting={createMutation.isPending || updateMutation.isPending || createVisitMutation.isPending}
          isSavingAndProceeding={createMutation.isPending || updateMutation.isPending}
          apiError={toDisplayError(createMutation.error ?? updateMutation.error ?? createVisitMutation.error)}
          savedPatient={savedPatient}
          onSaveAndProceed={handleSaveAndProceed}
          onSubmit={handleSubmit}
          onDirtyChange={setIsDirty}
        />
      </RequirePermission>
      </div>

      <UnsavedChangesDialog open={showUnsavedDialog} onConfirmDiscard={confirmDiscard} onCancel={cancelDiscard} />
    </div>
  );
}
