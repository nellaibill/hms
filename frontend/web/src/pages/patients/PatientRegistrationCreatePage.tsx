import type { CreatePatientRequest, Patient, PatientRegistrationCoreUiFormValues, PatientRegistrationUiFormValues } from '@hms/shared';
import { ArrowLeft, UserPlus } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useToast } from '@/components/ui/toast-context';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { useCreateInvoiceMutation, type BillingFormValues } from '../../features/billing';
import {
  PatientRegistrationForm,
  useCreatePatientMutation,
  useUpdatePatientMutation,
  useUploadPatientIdProofMutation,
  useUploadPatientPhotoMutation,
  type StagedDocuments,
} from '../../features/patients';
import { toDisplayError } from '../../features/patients/apiErrorDisplay';
import { toAllergyRequest, toBackendGender, toEmergencyContactRequest, toModeOfArrivalRequest } from '../../features/patients/bridging';
import { clearRegistrationDraft } from '../../features/patients/registrationDraft';

/**
 * Builds the real CreatePatientRequest from Patient Information + Contact Information +
 * Medical Information (the three tabs wired to the backend) plus the separately-staged
 * `documents` (idProofType/idProofNumber live there, not on the RHF form — see
 * DocumentUploadStaging's own doc comment). Registration Details/Billing stay UI-only: the
 * Patients backend has no encounter/visit concept yet (deliberately deferred), so nothing
 * from those two tabs is sent here.
 */
function toRequest(values: PatientRegistrationCoreUiFormValues, documents: StagedDocuments): CreatePatientRequest {
  const allergies = values.hasKnownAllergy
    ? [
        toAllergyRequest({ allergyCategory: values.allergyCategory, allergySpecify: values.allergySpecify, allergySeverity: values.allergySeverity }),
        ...values.additionalAllergies
          .filter((row) => row.allergyCategory && row.allergySeverity)
          .map((row) => toAllergyRequest(row)),
      ]
    : [];

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
  const billingMutation = useCreateInvoiceMutation();
  const { toast } = useToast();

  // Set once "Save and proceed to Registration" (Medical Information tab) successfully saves
  // the patient — lets a second click of that button (after going back and editing tabs 1-3)
  // update instead of re-create, and lets the final Billing-tab submit skip creating again.
  const [savedPatient, setSavedPatient] = useState<Patient | null>(null);

  function uploadStagedDocuments(patientId: string, documents: StagedDocuments) {
    if (documents.photo) {
      photoMutation.mutate(
        { id: patientId, file: documents.photo },
        {
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
        { id: patientId, idProofType: documents.idProofType, file: documents.idProofFile },
        {
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

  // Passed to PatientRegistrationForm as onSaveAndProceed — called after the form validates
  // Patient/Contact/Medical Information itself. Returns whether it succeeded so the form knows
  // whether to advance to Registration Details; a failure is surfaced via the apiError prop
  // below (derived from whichever mutation just failed), same display path final submit uses.
  async function handleSaveAndProceed(values: PatientRegistrationCoreUiFormValues, documents: StagedDocuments): Promise<boolean> {
    try {
      if (savedPatient) {
        const patient = await updateMutation.mutateAsync({
          id: savedPatient.id,
          request: { ...toRequest(values, documents), rowVersion: savedPatient.rowVersion },
        });
        setSavedPatient(patient);
        return true;
      }
      const patient = await createMutation.mutateAsync(toRequest(values, documents));
      setSavedPatient(patient);
      uploadStagedDocuments(patient.id, documents);
      return true;
    } catch {
      return false;
    }
  }

  // Already saved via "Save and proceed to Registration" — don't create the patient again
  // (that would 409 as a duplicate). Instead, re-send tabs 1-3 as an update first: the user
  // may have gone back to Patient/Contact/Medical Information and edited something (e.g.
  // fixing a mistyped ID proof number) after that first save without revisiting Medical
  // Information's own Save button — without this, that edit would silently never reach the
  // server, since this final submit previously only handled Billing. This PUT is a no-op in
  // effect when nothing changed (rowVersion still protects against real conflicts), so it's
  // safe to always send. Billing stays a fire-and-forget follow-up call exactly as before;
  // Billing wiring itself is out of scope for this pass.
  async function handleSubmit(values: PatientRegistrationUiFormValues, documents: StagedDocuments, billing: BillingFormValues) {
    if (savedPatient) {
      let patient: Patient;
      try {
        patient = await updateMutation.mutateAsync({
          id: savedPatient.id,
          request: { ...toRequest(values, documents), rowVersion: savedPatient.rowVersion },
        });
        setSavedPatient(patient);
      } catch {
        // Surfaced via the existing apiError prop (toDisplayError(... ?? updateMutation.error)) — stay on the form.
        return;
      }
      clearRegistrationDraft();
      billingMutation.mutate(
        {
          patientId: patient.id,
          visitId: patient.id,
          values: billing,
          patient: { name: `${patient.firstName} ${patient.lastName}`, uhid: patient.uhid },
        },
        {
          onError: () =>
            toast({
              title: 'Billing not saved',
              description: `${patient.firstName} ${patient.lastName} (UHID ${patient.uhid}) was registered, but billing failed to save. Add it from Accounts and Finance → OPD Billing Entry.`,
              variant: 'error',
            }),
        },
      );
      navigate(`/patients/registration/${patient.id}`);
      return;
    }

    // Fallback for reaching Billing without ever clicking "Save and proceed" (e.g. jumping
    // straight to a later tab via its header) — create now, same flow as before this change.
    createMutation.mutate(toRequest(values, documents), {
      onSuccess: (patient) => {
        clearRegistrationDraft();
        uploadStagedDocuments(patient.id, documents);
        billingMutation.mutate(
          {
            patientId: patient.id,
            visitId: patient.id,
            values: billing,
            patient: { name: `${patient.firstName} ${patient.lastName}`, uhid: patient.uhid },
          },
          {
            onError: () =>
              toast({
                title: 'Billing not saved',
                description: `${patient.firstName} ${patient.lastName} (UHID ${patient.uhid}) was registered, but billing failed to save. Add it from Accounts and Finance → OPD Billing Entry.`,
                variant: 'error',
              }),
          },
        );
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
          isSubmitting={createMutation.isPending || updateMutation.isPending}
          isSavingAndProceeding={createMutation.isPending || updateMutation.isPending}
          apiError={toDisplayError(createMutation.error ?? updateMutation.error)}
          savedPatient={savedPatient}
          onSaveAndProceed={handleSaveAndProceed}
          onSubmit={handleSubmit}
        />
      </RequirePermission>
      </div>
    </div>
  );
}
