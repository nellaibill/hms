import type { RecordVisitUiFormValues } from '@hms/shared';
import { ArrowLeft, ClipboardList, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useToast } from '@/components/ui/toast-context';
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
      <div className="px-6 pt-4 lg:px-8">
        <Link to={`/patients/registration/${id}`} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to patient
        </Link>
      </div>

      {/* Centered, brand-colored banner — matches the Page banner style used across module
          pages (Theme & Branding → Section headers). The UHID is shown here, not buried in
          the form, since confirming which patient this visit is for is the whole point of
          reaching this page from the search list rather than New Patient Registration. */}
      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <ClipboardList className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">
            Add Visit — {patient.title} {patient.firstName} {patient.lastName}
          </h1>
        </div>
        <p className="text-sm text-page-banner-foreground/85">UHID: {patient.uhid}</p>
      </div>

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
