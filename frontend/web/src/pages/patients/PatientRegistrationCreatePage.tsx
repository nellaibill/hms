import type { CreatePatientRequest, PatientRegistrationUiFormValues } from '@hms/shared';
import { ArrowLeft, UserPlus } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';
import { useToast } from '@/components/ui/toast-context';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { saveBillingForPatient, type BillingFormValues } from '../../features/billing';
import {
  PatientRegistrationForm,
  useCreatePatientMutation,
  useUploadPatientIdProofMutation,
  useUploadPatientPhotoMutation,
  type StagedDocuments,
} from '../../features/patients';
import { toDisplayError } from '../../features/patients/apiErrorDisplay';
import { toAllergyType, toBackendGender, toPhoneRelationLabel, toRelationshipLabel } from '../../features/patients/bridging';
import { humanize } from '../../features/patients/humanize';
import { clearRegistrationDraft } from '../../features/patients/registrationDraft';

/**
 * Bridges the source-doc-accurate form shape to the *current* backend Contracts, which
 * haven't changed yet (see docs/DecisionLog.md — UI ships first, backend catches up in
 * Phase 2). A few fields collected in the form don't have a backend slot yet:
 * - Mode of Arrival: the backend's old modeOfArrival field (walk-in/ambulance/referred)
 *   is superseded by the new referral/advertisement attribution captured here, which has
 *   no backend field at all yet — a fixed placeholder value is sent so the still-required
 *   backend field validates.
 * - The arrival-source details are captured in the UI but not sent — nowhere to persist
 *   them yet. (OP appointment type *is* sent now — see AppointmentTypeId, backed by
 *   Masters' AppointmentType, the same way Department/Consultant are.)
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
    bloodGroup: values.bloodGroup,

    addressLine1: values.addressLine1,
    addressLine2: values.addressLine2 || undefined,
    addressLine3: values.addressLine3 || undefined,
    district: values.district,
    state: values.state,
    pincode: values.pincode,

    primaryPhone: values.primaryPhone.number,
    primaryPhoneRelation: toPhoneRelationLabel(values.primaryPhone.relation),
    alternatePhone: values.additionalPhones[0]?.number || undefined,
    alternatePhoneRelation: values.additionalPhones[0] ? toPhoneRelationLabel(values.additionalPhones[0].relation) : undefined,
    alternatePhone2: values.additionalPhones[1]?.number || undefined,
    alternatePhone2Relation: values.additionalPhones[1] ? toPhoneRelationLabel(values.additionalPhones[1].relation) : undefined,
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
      departmentId: values.registration.departmentId,
      consultantId: values.registration.consultantId,
      appointmentTypeId: values.registration.appointmentTypeId || undefined,
      admissionType: values.registration.admissionType || undefined,
      referralSource,
      category: values.registration.category || undefined,
    },
  };
}

export default function PatientRegistrationCreatePage() {
  const navigate = useNavigate();
  const mutation = useCreatePatientMutation();
  const photoMutation = useUploadPatientPhotoMutation();
  const idProofMutation = useUploadPatientIdProofMutation();
  const { toast } = useToast();

  function handleSubmit(values: PatientRegistrationUiFormValues, documents: StagedDocuments, billing: BillingFormValues) {
    mutation.mutate(toRequest(values), {
      onSuccess: (patient) => {
        clearRegistrationDraft();
        // Fired-and-forgotten (not awaited before navigating, to keep registration fast),
        // but a failure must never be silent — the patient record already exists at this
        // point, so a failed upload here is recoverable via the Edit page's document
        // upload, as long as the receptionist actually finds out it failed.
        if (documents.photo) {
          photoMutation.mutate(
            { id: patient.id, file: documents.photo },
            {
              onError: () =>
                toast({
                  title: 'Photo not saved',
                  description: `${patient.firstName} ${patient.lastName} (UHID ${patient.uhid}) was registered, but the photo upload failed. Add it from the patient's Edit page.`,
                  variant: 'error',
                }),
            },
          );
        }
        if (documents.idProofFile) {
          idProofMutation.mutate(
            { id: patient.id, idProofType: documents.idProofType, file: documents.idProofFile },
            {
              onError: () =>
                toast({
                  title: 'ID proof not saved',
                  description: `${patient.firstName} ${patient.lastName} (UHID ${patient.uhid}) was registered, but the ID proof upload failed. Add it from the patient's Edit page.`,
                  variant: 'error',
                }),
            },
          );
        }
        // No Billing API yet — parked in the offline mock store (see mockBillingStore.ts)
        // the same way patient records were ahead of the Patients API, until Billing has a
        // backend endpoint of its own.
        saveBillingForPatient(patient.id, patient.currentRegistration?.id ?? patient.id, billing, {
          name: `${patient.firstName} ${patient.lastName}`,
          uhid: patient.uhid,
        });
        navigate(`/patients/registration/${patient.id}`);
      },
    });
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
          isSubmitting={mutation.isPending}
          apiError={toDisplayError(mutation.error)}
          onSubmit={handleSubmit}
        />
      </RequirePermission>
      </div>
    </div>
  );
}
