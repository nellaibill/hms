import { ApiError, IPD_ADMISSION_TYPES, type AdmissionFormValues, type Patient, type PatientVisit } from '@hms/shared';
import { AlertTriangle, ClipboardList, Loader2, UserRound } from 'lucide-react';
import { type ReactNode, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { PageBanner } from '@/components/PageBanner';
import { PatientPicker } from '@/features/billing';
import { usePatientQuery, usePatientVisitsQuery } from '@/features/patients';
import { AdmissionForm, useAdmissionsQuery, useCreateAdmissionMutation, type AdmissionFormInitialValues } from '../../features/ipd/admissions';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { PatientNameLink } from '@/components/PatientNameLink';

const NO_VISIT = 'none';

function visitLabel(visit: PatientVisit): string {
  const type = visit.visitType === 'DayCare' ? 'Day Care' : visit.visitType;
  const recorded = new Date(visit.createdAt).toLocaleString('en-IN', { day: 'numeric', month: 'numeric', year: 'numeric', hour: 'numeric', minute: '2-digit', hour12: true });
  return `${type} · ${recorded} · ${visit.visitNumber ?? 'IP No pending'}`;
}

/**
 * A patient can have only one active (Admitted or Requested) admission — the API refuses a
 * second one on Save. Checked here as soon as the patient is picked, so staff aren't left to fill
 * in the whole form only to be told at the very end: with an active admission, the form is
 * replaced by a warning that links to it. If the check itself fails, the form shows as before and
 * Save still enforces the rule.
 */
function ActiveAdmissionGate({ patientId, children }: { patientId: string; children: ReactNode }) {
  // Same query (and cache entry) as AdmissionVisitSelect's. isPlaceholderData: right after
  // "Change patient" the previous patient's admissions are still on screen — wait for this one's.
  const { data: admissions, isPending, isPlaceholderData } = useAdmissionsQuery({ patientId, pageSize: 100 });

  if (isPending || isPlaceholderData) {
    return (
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Checking the patient's admissions…
      </div>
    );
  }

  const active = admissions?.items.find((admission) => admission.status === 'Admitted' || admission.status === 'Requested');
  if (!active) return <>{children}</>;

  return (
    <div role="alert" className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2.5 text-sm text-foreground">
      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-warning" />
      <div className="flex flex-col gap-1">
        <p>
          <strong>This patient already has an active admission</strong> — {active.admissionNumber},{' '}
          {active.status === 'Requested'
            ? 'requested and waiting for a bed. Assign it a bed instead of creating a new admission.'
            : `admitted on ${new Date(active.admissionDateTime).toLocaleDateString('en-IN')}. Discharge it before admitting the patient again.`}
        </p>
        <Link to={`/clinical/ipd/admissions/${active.id}`} className="w-fit font-medium text-primary hover:underline">
          Open {active.admissionNumber}
        </Link>
      </div>
    </div>
  );
}

/**
 * Which of the patient's visits this admission is for (ADR-104) — that visit's IP No is issued
 * once an advance is paid on the admission. Lists the patient's non-OP visits that no other
 * admission has claimed, newest first, and once loaded picks the one Add Visit passed
 * (?visitId=), else the newest; "No visit" is a direct IPD admission, which gets no IP No.
 */
function AdmissionVisitSelect({
  patientId,
  preferredVisitId,
  value,
  onChange,
}: {
  patientId: string;
  preferredVisitId: string | null;
  value: string | null;
  onChange: (visitId: string | null) => void;
}) {
  const { data: visits, isPending: visitsPending } = usePatientVisitsQuery(patientId);
  const { data: admissions, isPending: admissionsPending } = useAdmissionsQuery({ patientId, pageSize: 100 });
  const linkedVisitIds = new Set(admissions?.items.map((admission) => admission.visitId).filter(Boolean));
  const eligible = (visits ?? []).filter((visit) => visit.visitType !== 'OP' && !linkedVisitIds.has(visit.visitId));
  const isReady = !visitsPending && !admissionsPending;

  const defaultAppliedRef = useRef(false);
  useEffect(() => {
    if (!isReady || defaultAppliedRef.current) return;
    defaultAppliedRef.current = true;
    onChange((eligible.find((visit) => visit.visitId === preferredVisitId) ?? eligible[0])?.visitId ?? null);
  }, [isReady, eligible, preferredVisitId, onChange]);

  if (!isReady) {
    return (
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading the patient's visits…
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor="admissionVisit">Visit</Label>
      <Select value={value ?? NO_VISIT} onValueChange={(selected) => onChange(selected === NO_VISIT ? null : selected)}>
        <SelectTrigger id="admissionVisit" className="w-full sm:w-[28rem]">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={NO_VISIT}>No visit (direct admission)</SelectItem>
          {eligible.map((visit) => (
            <SelectItem key={visit.visitId} value={visit.visitId}>
              {visitLabel(visit)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      <p className="text-xs text-muted-foreground">The visit's IP No is issued once an advance is paid on this admission.</p>
    </div>
  );
}

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
  const [preferredVisitId, setPreferredVisitId] = useState<string | null>(null);
  const [visitId, setVisitId] = useState<string | null>(null);
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
    setPreferredVisitId(searchParams.get('visitId'));
  }, [preselectedPatient, searchParams]);

  function handleChangePatient() {
    setPatient(null);
    setPrefill(undefined);
    setPreferredVisitId(null);
    setVisitId(null);
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
        visitId,
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
              <ActiveAdmissionGate key={patient.id} patientId={patient.id}>
                {/* key (on the gate): a fresh select (and fresh default) per patient. */}
                <AdmissionVisitSelect patientId={patient.id} preferredVisitId={preferredVisitId} value={visitId} onChange={setVisitId} />
                <AdmissionForm
                  patientId={patient.id}
                  initialValues={prefill}
                  isSubmitting={mutation.isPending}
                  apiError={mutation.error instanceof ApiError ? mutation.error : null}
                  onSubmit={handleSubmit}
                />
              </ActiveAdmissionGate>
            </div>
          </>
        )}
      </div>
    </div>
    </RequirePermission>
  );
}
