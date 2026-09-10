import { ApiError, type DischargeSummaryFormValues } from '@hms/shared';
import { useState } from 'react';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAdmissionQuery } from '@/features/ipd/admissions';
import { usePatientQuery } from '@/features/patients';
import {
  AdmissionHeaderCard,
  DischargeSummaryForm,
  FinalizationCard,
  FinalizeConfirmDialog,
  toUpdateRequest,
  useDischargeSummaryByAdmissionQuery,
  useFinalizeDischargeSummaryMutation,
  useUpdateDischargeSummaryMutation,
} from '@/features/dischargeSummary';

export default function DischargeSummaryEditPage() {
  const { admissionId } = useParams<{ admissionId: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();

  const { data: admission, isPending: isAdmissionPending } = useAdmissionQuery(admissionId);
  const { data: patient, isPending: isPatientPending } = usePatientQuery(admission?.patientId);
  const summaryQuery = useDischargeSummaryByAdmissionQuery(admissionId);

  const updateMutation = useUpdateDischargeSummaryMutation();
  const finalizeMutation = useFinalizeDischargeSummaryMutation();

  const [preparedByUserId, setPreparedByUserId] = useState('');
  const [checkedByUserId, setCheckedByUserId] = useState('');
  const [consultantApprovedByUserId, setConsultantApprovedByUserId] = useState('');
  const [isFinalizeDialogOpen, setIsFinalizeDialogOpen] = useState(false);

  if (isAdmissionPending || isPatientPending || summaryQuery.isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading discharge summary…
      </div>
    );
  }

  if (!admission || !patient) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Admission not found.
        </p>
      </div>
    );
  }

  if (summaryQuery.isError || !summaryQuery.data) {
    const is404 = summaryQuery.error instanceof ApiError && summaryQuery.error.status === 404;
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {is404 ? 'No discharge summary has been created for this admission yet.' : 'Failed to load the discharge summary.'}
        </p>
        <Link to={`/clinical/ipd/admissions/${admission.id}`} className="mt-3 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to admission
        </Link>
      </div>
    );
  }

  const summary = summaryQuery.data;

  if (summary.status === 'Finalized') {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-warning/10 px-3 py-2 text-sm text-warning">
          This discharge summary has already been finalized and can no longer be edited.
        </p>
        <Link
          to={`/clinical/ipd/admissions/${admission.id}/discharge-summary`}
          className="mt-3 inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          View discharge summary
        </Link>
      </div>
    );
  }

  // Captured as a plain string so the mutation's onSuccess closures below (nested deep enough
  // that TS's control-flow narrowing of `admission` above doesn't reach them) stay typed as
  // `string`, not `string | undefined`.
  const admissionRouteId = admission.id;

  function handleSave(values: DischargeSummaryFormValues) {
    updateMutation.mutate(
      { id: summary.id, request: toUpdateRequest(values) },
      {
        onSuccess: () => {
          toast({ title: 'Saved', description: 'The discharge summary has been saved.' });
        },
      },
    );
  }

  function handleFinalize() {
    finalizeMutation.mutate(
      {
        id: summary.id,
        request: {
          preparedByUserId: preparedByUserId || undefined,
          checkedByUserId: checkedByUserId || undefined,
          consultantApprovedByUserId: consultantApprovedByUserId || undefined,
        },
      },
      {
        onSuccess: () => {
          setIsFinalizeDialogOpen(false);
          toast({ title: 'Finalized', description: 'The discharge summary has been finalized.' });
          navigate(`/clinical/ipd/admissions/${admissionRouteId}/discharge-summary`);
        },
      },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        title={`Discharge Summary — ${patient.firstName} ${patient.lastName}`}
        subtitle={<span className="font-mono">{admission.admissionNumber} · Draft</span>}
        backTo={`/clinical/ipd/admissions/${admission.id}`}
        backLabel="Back to admission"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <AdmissionHeaderCard patient={patient} admission={admission} />

        <DischargeSummaryForm
          summary={summary}
          isSubmitting={updateMutation.isPending}
          apiError={updateMutation.error instanceof ApiError ? updateMutation.error : null}
          onSave={handleSave}
        />

        <FinalizationCard
          preparedByUserId={preparedByUserId}
          checkedByUserId={checkedByUserId}
          consultantApprovedByUserId={consultantApprovedByUserId}
          onPreparedByChange={setPreparedByUserId}
          onCheckedByChange={setCheckedByUserId}
          onConsultantApprovedByChange={setConsultantApprovedByUserId}
        />

        <div>
          <Button type="button" variant="destructive" onClick={() => setIsFinalizeDialogOpen(true)}>
            Finalize
          </Button>
        </div>
      </div>

      <FinalizeConfirmDialog
        open={isFinalizeDialogOpen}
        isSubmitting={finalizeMutation.isPending}
        apiError={finalizeMutation.error instanceof ApiError ? finalizeMutation.error : null}
        onOpenChange={setIsFinalizeDialogOpen}
        onConfirm={handleFinalize}
      />
    </div>
  );
}
