import type { RecordVisitUiFormValues } from '@hms/shared';
import { ClipboardList, Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useToast } from '@/components/ui/toast-context';
import { PageBanner } from '@/components/PageBanner';
import { UnsavedChangesDialog } from '@/components/UnsavedChangesDialog';
import { useUnsavedChangesGuard } from '@/hooks/useUnsavedChangesGuard';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { RecordVisitForm, useCreatePatientVisitMutation, usePatientQuery } from '../../features/patients';
import { toDisplayError } from '../../features/patients/apiErrorDisplay';
import { toCreatePatientVisitRequest } from '../../features/patients/bridging';

/** "Add Visit" — records a new registration/encounter for an existing, already-registered
 * patient (reached from the Old Patient Registration search list). Distinct from the New
 * Patient Registration wizard's own Registration Details tab: there is no patient to create
 * here, so this page is just that one tab's fields on their own, submitting straight to
 * POST /api/v1/patients/{id}/visits for the patient already loaded by :id. */
export default function PatientRecordVisitPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { data: patient, isPending, isError } = usePatientQuery(id);
  const mutation = useCreatePatientVisitMutation();
  const [isDirty, setIsDirty] = useState(false);
  const { showUnsavedDialog, confirmDiscard, cancelDiscard, markSaved } = useUnsavedChangesGuard(isDirty);

  // Reached by typing/bookmarking this URL directly for a patient still flagged
  // Patient.requiresDataVerification — PatientSummaryCard's Add Visit button already blocks
  // this case with VerifyPatientDialog, but this page must refuse the flow too rather than
  // trust that every entry point stayed gated. Bounces back to the patient page immediately.
  useEffect(() => {
    if (patient?.requiresDataVerification) {
      toast({
        title: 'Verify patient details first',
        description: `${patient.firstName} ${patient.lastName} (UHID ${patient.uhid}) still has placeholder data — edit and verify before adding a visit.`,
        variant: 'warning',
      });
      navigate(`/patients/registration/${id}`, { replace: true });
    }
  }, [patient, id, navigate, toast]);

  if (isPending || patient?.requiresDataVerification) {
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
  // function's closure — still applies. Same pattern as PatientEditPage.tsx.
  const uhid = patient.uhid;

  function handleSubmit(values: RecordVisitUiFormValues) {
    mutation.mutate(
      { id: id as string, request: toCreatePatientVisitRequest(values) },
      {
        onSuccess: () => {
          toast({ title: 'Visit recorded', description: `A new visit was added to UHID ${uhid}.`, variant: 'success' });
          markSaved();
          navigate(`/patients/registration/${id}`);
        },
      },
    );
  }

  return (
    <div className="flex flex-1 flex-col">
      {/* The UHID is shown here, not buried in the form, since confirming which patient this
          visit is for is the whole point of reaching this page from the search list rather
          than New Patient Registration. */}
      <PageBanner
        icon={ClipboardList}
        title={`Add Visit — ${patient.title} ${patient.firstName} ${patient.lastName}`}
        subtitle={`UHID: ${patient.uhid}`}
        backTo={`/patients/registration/${id}`}
        backLabel="Back to patient"
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <RequirePermission permission="patient-management.edit">
          <RecordVisitForm
            isSubmitting={mutation.isPending}
            apiError={toDisplayError(mutation.error)}
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
