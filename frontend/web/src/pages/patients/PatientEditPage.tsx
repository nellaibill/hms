import type { Patient, PatientEditUiFormValues, UpdatePatientRequest } from '@hms/shared';
import { ArrowLeft, Loader2, UserCog } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { PatientEditForm, usePatientQuery, useUpdatePatientMutation } from '../../features/patients';
import { toDisplayError } from '../../features/patients/apiErrorDisplay';
import { fromAllergyType, fromBackendGender, fromRelationshipLabel, toAllergyType, toBackendGender, toRelationshipLabel } from '../../features/patients/bridging';

/**
 * See PatientRegistrationCreatePage.tsx's toRequest() comment for why this bridge exists.
 * rowVersion is threaded in separately (not part of the editable form state) — it's the
 * optimistic-concurrency token from the Patient this edit was loaded from, echoed back so
 * the server can detect and reject a save made against data someone else has since changed.
 */
function toRequest(values: PatientEditUiFormValues, rowVersion: string): UpdatePatientRequest {
  return {
    title: values.title,
    firstName: values.firstName,
    lastName: values.lastName,
    dateOfBirth: values.dateOfBirth,
    gender: toBackendGender(values.gender),
    bloodGroup: values.bloodGroup,

    addressLine1: values.addressLine1,
    addressLine2: values.addressLine2 || undefined,
    addressLine3: values.addressLine3 || undefined,
    district: values.district,
    state: values.state,
    pincode: values.pincode,

    primaryPhone: values.primaryPhone.number,
    alternatePhone: values.secondaryPhone || undefined,
    email: values.email || undefined,
    profession: values.profession || undefined,

    emergencyContactRelationship: toRelationshipLabel(values.emergencyContactRelationship),
    emergencyContactName: values.emergencyContactName,
    emergencyContactPhone: values.emergencyContactPhone,

    hasKnownAllergy: values.hasKnownAllergy,
    allergyType: values.hasKnownAllergy ? toAllergyType(values.allergyCategory ?? '', values.allergySpecify ?? '') : undefined,
    allergySeverity: values.hasKnownAllergy ? values.allergySeverity || undefined : undefined,

    rowVersion,
  };
}

/** Reconstructs the richer edit-form shape from the patient record the backend actually returns. */
function toDefaultValues(patient: Patient): PatientEditUiFormValues {
  const allergy = fromAllergyType(patient.allergyType);

  return {
    title: patient.title,
    firstName: patient.firstName,
    lastName: patient.lastName,
    dateOfBirth: patient.dateOfBirth,
    gender: fromBackendGender(patient.gender),
    // Pre-required-validation records may still have a null BloodGroup in the database —
    // 'Unknown' is the honest, backward-compatible default rather than leaving the Select
    // unset (which the required schema below would then reject on save).
    bloodGroup: patient.bloodGroup ?? 'Unknown',
    // No backend field yet (see PatientRegistrationCreatePage.tsx's toRequest() comment) —
    // 'NA' is the honest default for an existing record with nothing recorded, same reasoning
    // as bloodGroup's 'Unknown' fallback above.
    maritalStatus: 'NA',

    addressLine1: patient.addressLine1,
    addressLine2: patient.addressLine2 ?? '',
    addressLine3: patient.addressLine3 ?? '',
    district: patient.district,
    state: patient.state,
    pincode: patient.pincode,

    primaryPhone: { number: patient.primaryPhone },
    secondaryPhone: patient.alternatePhone ?? '',
    email: patient.email ?? '',
    profession: patient.profession ?? '',

    emergencyContactRelationship: fromRelationshipLabel(patient.emergencyContactRelationship),
    emergencyContactName: patient.emergencyContactName,
    emergencyContactPhone: patient.emergencyContactPhone,
    // No backend field yet (see PatientRegistrationCreatePage.tsx's toRequest() comment) —
    // an existing record never has any to restore.
    additionalEmergencyContacts: [],

    hasKnownAllergy: patient.hasKnownAllergy,
    allergyCategory: allergy.category,
    allergySpecify: allergy.specify,
    allergySeverity: patient.allergySeverity ?? '',
    // No backend field yet (see PatientRegistrationCreatePage.tsx's toRequest() comment) —
    // an existing record never has any to restore.
    additionalAllergies: [],
  };
}

export default function PatientEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: patient, isPending, isError } = usePatientQuery(id);
  const mutation = useUpdatePatientMutation();

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
  const rowVersion = patient.rowVersion ?? '';

  function handleSubmit(values: PatientEditUiFormValues) {
    mutation.mutate(
      { id: id as string, request: toRequest(values, rowVersion) },
      { onSuccess: () => navigate(`/patients/registration/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link
          to={`/patients/registration/${id}`}
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to patient
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <UserCog className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">
            Edit {patient.firstName} {patient.lastName}
          </h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">Update this patient's demographic details.</p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <RequirePermission permission="patient-management.edit">
        <PatientEditForm
          patientId={id as string}
          isSubmitting={mutation.isPending}
          apiError={toDisplayError(mutation.error)}
          defaultValues={toDefaultValues(patient)}
          onSubmit={handleSubmit}
          onCancel={() => navigate(`/patients/registration/${id}`)}
        />
      </RequirePermission>
      </div>
    </div>
  );
}
