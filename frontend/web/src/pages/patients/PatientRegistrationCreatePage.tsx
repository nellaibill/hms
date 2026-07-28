import { ApiError, type CreatePatientRequest, type PatientRegistrationUiFormValues } from '@hms/shared';
import { ArrowLeft, UserPlus } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { PatientRegistrationForm, useCreatePatientMutation } from '../../features/patients';
import { toAllergyType, toBackendGender, toPhoneRelationLabel, toRelationshipLabel } from '../../features/patients/bridging';
import { humanize } from '../../features/patients/humanize';

/**
 * Bridges the source-doc-accurate form shape to the *current* backend Contracts, which
 * haven't changed yet (see docs/DecisionLog.md — UI ships first, backend catches up in
 * Phase 2). A few fields collected in the form don't have a backend slot yet:
 * - Gender: Transgender/NA have no backend value yet — mapped to "Other" as a temporary
 *   bridge until the backend Gender enum is extended.
 * - Mode of Arrival: the backend's old modeOfArrival field (walk-in/ambulance/referred)
 *   is superseded by the new referral/advertisement attribution captured here, which has
 *   no backend field at all yet — a fixed placeholder value is sent so the still-required
 *   backend field validates.
 * - The 2nd additional phone number, OP appointment type, and the arrival-source details
 *   are captured in the UI but not sent — nowhere to persist them yet.
 * - Allergy category+"specify" and the IP/Emergency/Day-care referral column are composed
 *   into the single free-text backend fields (AllergyType / ReferralSource) that already exist.
 */
function toRequest(values: PatientRegistrationUiFormValues): CreatePatientRequest {
  const referral = values.registration.referral;
  const referralSource = referral?.category
    ? [humanize(referral.category), referral.details, referral.contactNumber && `Contact: ${referral.contactNumber}`]
        .filter(Boolean)
        .join(' — ')
    : undefined;

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

    registration: {
      encounterType: values.registration.encounterType,
      modeOfArrival: 'WalkIn',
      department: values.registration.department,
      consultant: values.registration.consultant,
      admissionType: values.registration.admissionType || undefined,
      referralSource,
      category: values.registration.category || undefined,
    },
  };
}

export default function PatientRegistrationCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreatePatientMutation();

  function handleSubmit(values: PatientRegistrationUiFormValues) {
    mutation.mutate(toRequest(values), {
      onSuccess: (patient) => navigate(`/patients/registration/${patient.id}`),
    });
  }

  return (
    <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
      <div>
        <Link
          to="/patients/registration"
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to registration
        </Link>
        <div className="mt-2 flex items-start gap-3 border-b border-border pb-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
            <UserPlus className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-foreground">New Patient Registration</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Capture demographics, contacts, and encounter details. A UHID and registration number are assigned automatically.
            </p>
          </div>
        </div>
      </div>

      <PatientRegistrationForm
        isSubmitting={mutation.isPending}
        apiError={mutation.error instanceof ApiError ? mutation.error : null}
        onSubmit={handleSubmit}
      />
    </div>
  );
}
