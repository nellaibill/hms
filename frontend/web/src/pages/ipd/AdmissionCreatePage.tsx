import { ApiError, IPD_ADMISSION_TYPES, type AdmissionFormValues, type Patient } from '@hms/shared';
import { ClipboardList, Loader2, UserRound } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { PatientPicker } from '@/features/billing';
import { usePatientQuery } from '@/features/patients';
import { AdmissionForm, useCreateAdmissionMutation, type AdmissionFormInitialValues } from '../../features/ipd/admissions';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { PatientNameLink } from '@/components/PatientNameLink';

/** The visit-derived prefill carried on the URL by Add Visit's "Proceed with admission". */
function readPrefill(searchParams: URLSearchParams): AdmissionFormInitialValues {
  const admissionType = searchParams.get('admissionType');
  return {
    departmentId: searchParams.get('departmentId') ?? undefined,
    consultantId: searchParams.get('consultantId') ?? undefined,
    admissionType: IPD_ADMISSION_TYPES.find((type) => type === admissionType),
  };
}

export default function AdmissionCreatePage() {
  const navigate = useNavigate();
  const [patient, setPatient] = useState<Patient | null>(null);
  const [prefill, setPrefill] = useState<AdmissionFormInitialValues | undefined>(undefined);
  const mutation = useCreateAdmissionMutation();

  // ?patientId= (Add Visit → "Proceed with admission" for a non-OP visit) preselects that
  // patient once — same pattern as InvoiceCreatePage — and ?departmentId=/?consultantId=/
  // ?admissionType= pre-fill the form from that visit. "Change patient" drops the prefill too,
  // since it belonged to that patient's visit. If the patient can't be loaded, the picker shows.
  const [searchParams] = useSearchParams();
  const { data: preselectedPatient, isLoading: isPreselectLoading } = usePatientQuery(searchParams.get('patientId') ?? undefined);
  const preselectAppliedRef = useRef(false);
  useEffect(() => {
    if (!preselectedPatient || preselectAppliedRef.current) return;
    preselectAppliedRef.current = true;
    setPatient(preselectedPatient);
    setPrefill(readPrefill(searchParams));
  }, [preselectedPatient, searchParams]);

  function handleChangePatient() {
    setPatient(null);
    setPrefill(undefined);
  }

  function handleSubmit(values: AdmissionFormValues) {
    mutation.mutate(
      {
        patientId: values.patientId,
        departmentId: values.departmentId,
        consultantId: values.consultantId,
        wardId: values.wardId,
        bedId: values.bedId,
        // datetime-local's value ("YYYY-MM-DDTHH:mm") has no timezone offset — Date parses
        // it as local time, and toISOString() converts that to the UTC instant the backend
        // expects. Left blank, the backend defaults to "now" server-side.
        admissionDateTime: values.admissionDateTime ? new Date(values.admissionDateTime).toISOString() : undefined,
        admissionType: values.admissionType,
        reasonForAdmission: values.reasonForAdmission,
      },
      { onSuccess: (admission) => navigate(`/clinical/ipd/admissions/${admission.id}`) },
    );
  }

  return (
    <RequirePermission permission="clinical-care.create">
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={ClipboardList}
        title="New Admission"
        subtitle="Convert an existing patient into an inpatient admission."
        backTo="/clinical/ipd/admissions"
        backLabel="Back to admissions"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        {!patient && isPreselectLoading && (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading patient…
          </div>
        )}

        {!patient && !isPreselectLoading && (
          <div className="flex flex-col gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Patient Information</h2>
            <PatientPicker onSelect={setPatient} />
          </div>
        )}

        {patient && (
          <>
            <Card>
              <CardContent className="flex flex-wrap items-center justify-between gap-4 py-4">
                <div className="flex items-center gap-3">
                  <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                    <UserRound className="h-5 w-5" />
                  </span>
                  <div className="flex flex-col">
                    <PatientNameLink patientId={patient.id} newTab>
                      {patient.title} {patient.firstName} {patient.lastName}
                    </PatientNameLink>
                    <span className="text-xs text-muted-foreground">
                      {patient.uhid} · {patient.age} yrs · {patient.gender} · {patient.primaryPhone}
                    </span>
                  </div>
                </div>
                <Button variant="outline" size="sm" onClick={handleChangePatient}>
                  Change patient
                </Button>
              </CardContent>
            </Card>

            <div className="flex flex-col gap-3">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Admission Details &amp; Bed Allocation</h2>
              <AdmissionForm
                patientId={patient.id}
                initialValues={prefill}
                isSubmitting={mutation.isPending}
                apiError={mutation.error instanceof ApiError ? mutation.error : null}
                onSubmit={handleSubmit}
              />
            </div>
          </>
        )}
      </div>
    </div>
    </RequirePermission>
  );
}
