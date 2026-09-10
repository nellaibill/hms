import type { Patient, PatientEditUiFormValues, UpdatePatientRequest } from '@hms/shared';
import { Loader2, UserCog } from 'lucide-react';
import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { UnsavedChangesDialog } from '@/components/UnsavedChangesDialog';
import { useUnsavedChangesGuard } from '@/hooks/useUnsavedChangesGuard';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { DataVerificationBanner, PatientEditForm, usePatientQuery, useUpdatePatientMutation } from '../../features/patients';
import { toDisplayError } from '../../features/patients/apiErrorDisplay';
import { toBackendGender, fromBackendGender, fromModeOfArrivalFields, toModeOfArrivalRequest } from '../../features/patients/bridging';

/**
 * See PatientRegistrationCreatePage.tsx's toRequest() comment for the same structural
 * mapping, applied here to an edit instead of a create. rowVersion is threaded in separately
 * (not part of the editable form state) — it's the optimistic-concurrency token from the
 * Patient this edit was loaded from, echoed back so the server can detect and reject a save
 * made against data someone else has since changed. modeOfArrival* and idProofType/
 * idProofNumber now both come from the edited `values` (real fields on
 * PatientEditUiFormValues — see patientEditUiSchema and ADR's reversal allowing Mode of
 * Arrival to be edited here, matching Registration). Allergies/EmergencyContacts aren't part
 * of UpdatePatientRequest (they have their own add/remove endpoints) — Allergies are wired
 * separately via useAddPatientAllergyMutation/useRemovePatientAllergyMutation in
 * PatientEditForm, fired immediately rather than batched into this request.
 */
function toRequest(values: PatientEditUiFormValues, rowVersion: string): UpdatePatientRequest {
  const modeOfArrival = toModeOfArrivalRequest(values.arrivalSource);
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

    idProofType: values.idProofType,
    idProofNumber: values.idProofNumber.trim(),

    modeOfArrivalSource: modeOfArrival.modeOfArrivalSource,
    modeOfArrivalChannel: modeOfArrival.modeOfArrivalChannel,
    modeOfArrivalSpecify: modeOfArrival.modeOfArrivalSpecify,

    address: {
      addressLine1: values.addressLine1,
      addressLine2: values.addressLine2 || undefined,
      addressLine3: values.addressLine3 || undefined,
      stateId: values.state,
      districtId: values.district,
      pincode: values.pincode,
    },

    rowVersion,
  };
}

/** Reconstructs the edit-form shape from the patient record the backend actually returns. Note
 * allergies are deliberately not read here — they're not part of PatientEditUiFormValues at
 * all (see patientEditUiSchema's own comment); PatientEditForm reads patient.allergies
 * directly as its own prop instead. */
function toDefaultValues(patient: Patient): PatientEditUiFormValues {
  const primaryContact = patient.emergencyContacts[0];

  return {
    title: patient.title,
    firstName: patient.firstName,
    lastName: patient.lastName,
    dateOfBirth: patient.dateOfBirth,
    gender: fromBackendGender(patient.gender),
    bloodGroup: patient.bloodGroup,
    maritalStatus: patient.maritalStatus,

    addressLine1: patient.address.addressLine1,
    addressLine2: patient.address.addressLine2 ?? '',
    addressLine3: patient.address.addressLine3 ?? '',
    district: patient.address.districtId,
    state: patient.address.stateId,
    pincode: patient.address.pincode,

    primaryPhone: { number: patient.primaryPhone },
    secondaryPhone: patient.secondaryPhone ?? '',
    email: patient.email ?? '',
    profession: patient.profession ?? '',

    idProofType: patient.idProofType ?? 'Aadhaar',
    idProofNumber: patient.idProofNumber ?? '',

    arrivalSource: fromModeOfArrivalFields(patient),

    emergencyContactRelationship: primaryContact?.relationship ?? 'Father',
    emergencyContactName: primaryContact?.name ?? '',
    emergencyContactPhone: primaryContact?.phone ?? '',
    // Every emergency contact beyond the first now round-trips (the backend genuinely stores
    // all of them) — the primary slot above always shows patient.emergencyContacts[0].
    additionalEmergencyContacts: patient.emergencyContacts.slice(1).map((contact) => ({
      relationship: contact.relationship,
      name: contact.name,
      phone: contact.phone,
    })),
  };
}

export default function PatientEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: patient, isPending, isError } = usePatientQuery(id);
  const mutation = useUpdatePatientMutation();
  const [isDirty, setIsDirty] = useState(false);
  const { showUnsavedDialog, confirmDiscard, cancelDiscard, markSaved } = useUnsavedChangesGuard(isDirty);

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading patient…
      </div>
    );
  }

  if (isError || !patient) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Patient not found.
        </p>
      </div>
    );
  }

  // Captured here (not read inside handleSubmit directly) so TypeScript's narrowing of
  // `patient` from the isError/!patient guard above — which doesn't extend into a nested
  // function's closure — still applies.
  const rowVersion = patient.rowVersion;

  function handleSubmit(values: PatientEditUiFormValues) {
    mutation.mutate(
      { id: id as string, request: toRequest(values, rowVersion) },
      {
        onSuccess: () => {
          markSaved();
          navigate(`/patients/registration/${id}`);
        },
      },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={UserCog}
        title={`Edit ${patient.firstName} ${patient.lastName}`}
        subtitle="Update this patient's demographic details."
        backTo={`/patients/registration/${id}`}
        backLabel="Back to patient"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      {patient.requiresDataVerification && <DataVerificationBanner />}
      <RequirePermission permission="patient-management.edit">
        <PatientEditForm
          patientId={id as string}
          allergies={patient.allergies}
          isSubmitting={mutation.isPending}
          apiError={toDisplayError(mutation.error)}
          defaultValues={toDefaultValues(patient)}
          onSubmit={handleSubmit}
          onCancel={() => navigate(`/patients/registration/${id}`)}
          onDirtyChange={setIsDirty}
        />
      </RequirePermission>
      </div>

      <UnsavedChangesDialog open={showUnsavedDialog} onConfirmDiscard={confirmDiscard} onCancel={cancelDiscard} />
    </div>
  );
}
