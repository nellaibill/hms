import { ApiError, type AdmissionFormValues, type Patient } from '@hms/shared';
import { ClipboardList, UserRound } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { PatientPicker } from '@/features/billing';
import { AdmissionForm, useCreateAdmissionMutation } from '../../features/ipd/admissions';
import { RequirePermission } from '../../features/auth/RequirePermission';

export default function AdmissionCreatePage() {
  const navigate = useNavigate();
  const [patient, setPatient] = useState<Patient | null>(null);
  const mutation = useCreateAdmissionMutation();

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
        {!patient && (
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
                    <span className="font-medium text-foreground">
                      {patient.title} {patient.firstName} {patient.lastName}
                    </span>
                    <span className="text-xs text-muted-foreground">
                      {patient.uhid} · {patient.age} yrs · {patient.gender} · {patient.primaryPhone}
                    </span>
                  </div>
                </div>
                <Button variant="outline" size="sm" onClick={() => setPatient(null)}>
                  Change patient
                </Button>
              </CardContent>
            </Card>

            <div className="flex flex-col gap-3">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Admission Details &amp; Bed Allocation</h2>
              <AdmissionForm
                patientId={patient.id}
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
