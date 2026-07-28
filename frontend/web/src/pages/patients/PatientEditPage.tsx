import { ApiError, type Patient, type PatientEditUiFormValues, type UpdatePatientRequest } from '@hms/shared';
import { ArrowLeft, Loader2, UserCog } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { PatientEditForm, usePatientQuery, useUpdatePatientMutation } from '../../features/patients';
import {
  fromAllergyType,
  fromBackendGender,
  fromPhoneRelationLabel,
  fromRelationshipLabel,
  toAllergyType,
  toBackendGender,
  toPhoneRelationLabel,
  toRelationshipLabel,
} from '../../features/patients/bridging';

/** See PatientRegistrationCreatePage.tsx's toRequest() comment for why this bridge exists. */
function toRequest(values: PatientEditUiFormValues): UpdatePatientRequest {
  return {
    title: values.title,
    firstName: values.firstName,
    lastName: values.lastName,
    dateOfBirth: values.dateOfBirth,
    gender: toBackendGender(values.gender),
    bloodGroup: values.bloodGroup || undefined,

    addressLine1: values.addressLine1,
    addressLine2: values.addressLine2 || undefined,
    addressLine3: values.addressLine3 || undefined,
    district: values.district,
    state: values.state,
    pincode: values.pincode,

    primaryPhone: values.primaryPhone.number,
    primaryPhoneRelation: toPhoneRelationLabel(values.primaryPhone.relation),
    alternatePhone: values.additionalPhones[0]?.number || undefined,
    email: values.email || undefined,
    profession: values.profession || undefined,

    emergencyContactRelationship: toRelationshipLabel(values.emergencyContactRelationship),
    emergencyContactName: values.emergencyContactName,
    emergencyContactPhone: values.emergencyContactPhone,

    hasKnownAllergy: values.hasKnownAllergy,
    allergyType: values.hasKnownAllergy ? toAllergyType(values.allergyCategory ?? '', values.allergySpecify ?? '') : undefined,
    allergySeverity: values.hasKnownAllergy ? values.allergySeverity || undefined : undefined,
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
    bloodGroup: patient.bloodGroup ?? '',

    addressLine1: patient.addressLine1,
    addressLine2: patient.addressLine2 ?? '',
    addressLine3: patient.addressLine3 ?? '',
    district: patient.district,
    state: patient.state,
    pincode: patient.pincode,

    primaryPhone: { number: patient.primaryPhone, relation: fromPhoneRelationLabel(patient.primaryPhoneRelation) },
    additionalPhones: patient.alternatePhone ? [{ number: patient.alternatePhone, relation: 'Self' }] : [],
    email: patient.email ?? '',
    profession: patient.profession ?? '',

    emergencyContactRelationship: fromRelationshipLabel(patient.emergencyContactRelationship),
    emergencyContactName: patient.emergencyContactName,
    emergencyContactPhone: patient.emergencyContactPhone,

    hasKnownAllergy: patient.hasKnownAllergy,
    allergyCategory: allergy.category,
    allergySpecify: allergy.specify,
    allergySeverity: patient.allergySeverity ?? '',
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

  function handleSubmit(values: PatientEditUiFormValues) {
    mutation.mutate(
      { id: id as string, request: toRequest(values) },
      { onSuccess: () => navigate(`/patients/registration/${id}`) },
    );
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <div>
        <Link
          to={`/patients/registration/${id}`}
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to patient
        </Link>
        <div className="mt-2 flex items-start gap-3 border-b border-border pb-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
            <UserCog className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-foreground">
              Edit {patient.firstName} {patient.lastName}
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">Update this patient's demographic details.</p>
          </div>
        </div>
      </div>

      <PatientEditForm
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        defaultValues={toDefaultValues(patient)}
        onSubmit={handleSubmit}
        onCancel={() => navigate(`/patients/registration/${id}`)}
      />
    </div>
  );
}
