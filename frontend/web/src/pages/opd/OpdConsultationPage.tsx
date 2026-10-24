import { ApiError, type SaveOpdConsultationRequest } from '@hms/shared';
import { BadgeCheck, Flag, History, Loader2, Stethoscope } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { PageBanner } from '@/components/PageBanner';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { branding } from '@/config/branding';
import { useAuth } from '@/features/auth/AuthContext';
import { useBrandingQuery } from '@/features/branding/hooks/useBrandingQuery';
import {
  exportOpdConsultationPdf,
  MarkPatientDialog,
  OpdConsultationForm,
  OpdConsultationHeader,
  OpdConsultationPrintTemplate,
  ReopenConsultationDialog,
  useCompleteOpdConsultationMutation,
  useMarkPatientMutation,
  useOpdConsultationQuery,
  useReopenOpdConsultationMutation,
  useSaveOpdConsultationDraftMutation,
  useUnmarkPatientMutation,
} from '@/features/opdConsultation';

/**
 * Reached from the OPD Patient List's "Consult" action (OpdPatientListTable.tsx) — replaces the
 * previous placeholder navigation to the patient registration page. Keyed by consultationId,
 * the same PatientVisitConsultation id the "Consult" button already transitions to
 * InConsultation before navigating here.
 */
export default function OpdConsultationPage() {
  const { consultationId } = useParams<{ consultationId: string }>();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const [showReopenDialog, setShowReopenDialog] = useState(false);
  const [showMarkDialog, setShowMarkDialog] = useState(false);

  const { data, isPending, isError } = useOpdConsultationQuery(consultationId);
  const saveDraftMutation = useSaveOpdConsultationDraftMutation(consultationId);
  const completeMutation = useCompleteOpdConsultationMutation(consultationId);
  const reopenMutation = useReopenOpdConsultationMutation(consultationId);
  const markMutation = useMarkPatientMutation(consultationId);
  const unmarkMutation = useUnmarkPatientMutation(consultationId);
  const { data: brandingConfig } = useBrandingQuery();

  if (isPending) {
    return (
      <div className="flex flex-1 items-center justify-center gap-2 p-6 text-sm text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        Loading consultation…
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="p-6">
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          This consultation could not be found.
        </p>
        <Link to="/clinical/opd" className="mt-3 inline-block text-sm text-primary hover:underline">
          Back to Patient List
        </Link>
      </div>
    );
  }

  const { header, note } = data;
  const canEdit = hasPermission('clinical-care.edit');
  const isMarked = Boolean(note.markedAt);

  function handleSaveDraft(values: SaveOpdConsultationRequest) {
    saveDraftMutation.mutate(values, { onSuccess: () => toast({ title: 'Draft saved', description: 'The consultation draft has been saved.' }) });
  }

  function handleComplete(values: SaveOpdConsultationRequest) {
    completeMutation.mutate(values, {
      onSuccess: () => toast({ title: 'Consultation completed', description: 'The consultation has been marked as completed.' }),
    });
  }

  function handleReopen() {
    reopenMutation.mutate(undefined, {
      onSuccess: () => {
        setShowReopenDialog(false);
        toast({ title: 'Consultation reopened', description: 'The consultation is editable again.' });
      },
    });
  }

  function openMarkDialog() {
    markMutation.reset();
    unmarkMutation.reset();
    setShowMarkDialog(true);
  }

  function handleMark(reason: string) {
    markMutation.mutate(
      { reason: reason.trim() || null },
      {
        onSuccess: () => {
          setShowMarkDialog(false);
          toast({ title: isMarked ? 'Mark updated' : 'Patient marked', description: isMarked ? 'The reason has been updated.' : `${header.patientName} has been marked.` });
        },
      },
    );
  }

  function handleUnmark() {
    unmarkMutation.mutate(undefined, {
      onSuccess: () => {
        setShowMarkDialog(false);
        toast({ title: 'Mark removed', description: `${header.patientName} is no longer marked on this consultation.` });
      },
    });
  }

  function handlePrint() {
    window.print();
  }

  function handleDownloadPdf() {
    exportOpdConsultationPdf(header, note, {
      name: brandingConfig?.hospitalName ?? branding.hospitalName,
      address: brandingConfig?.address,
      phoneNumber: brandingConfig?.phoneNumber,
    });
  }

  const activeError = saveDraftMutation.error ?? completeMutation.error;
  const markError = markMutation.error ?? unmarkMutation.error;

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Stethoscope}
        title="OPD Consultation"
        subtitle="Record consultation details, diagnosis, investigations and plan of management"
        backTo="/clinical/opd"
        backLabel="Back to Patient List"
        rightActions={
          <>
            {canEdit && (
              <Button
                type="button"
                variant="success"
                className="gap-1.5"
                onClick={openMarkDialog}
                title={isMarked ? (note.markReason ? `Marked: ${note.markReason}` : 'Marked') : 'Mark this patient'}
              >
                {isMarked ? <BadgeCheck className="h-4 w-4" /> : <Flag className="h-4 w-4" />}
                {isMarked ? 'Marked' : 'Mark Patient'}
              </Button>
            )}
            <Button
              asChild
              variant="outline"
              className="gap-1.5 border-page-banner-foreground/30 bg-page-banner-foreground/10 text-page-banner-foreground hover:bg-page-banner-foreground/20"
            >
              <Link to={`/patients/registration/${header.patientId}`}>
                <History className="h-4 w-4" />
                View Patient History
              </Link>
            </Button>
          </>
        }
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <OpdConsultationHeader header={header} />

        <OpdConsultationForm
          note={note}
          isSavingDraft={saveDraftMutation.isPending}
          isCompleting={completeMutation.isPending}
          isReopening={reopenMutation.isPending}
          canReopen={canEdit}
          apiError={activeError instanceof ApiError ? activeError : null}
          onSaveDraft={handleSaveDraft}
          onComplete={handleComplete}
          onReopen={() => setShowReopenDialog(true)}
          onPrint={handlePrint}
          onDownloadPdf={handleDownloadPdf}
        />
      </div>

      {showMarkDialog && (
        <MarkPatientDialog
          patientName={header.patientName}
          currentReason={note.markReason}
          isMarked={isMarked}
          isSaving={markMutation.isPending}
          isRemoving={unmarkMutation.isPending}
          error={markError instanceof Error ? markError.message : null}
          onConfirm={handleMark}
          onRemove={handleUnmark}
          onCancel={() => setShowMarkDialog(false)}
        />
      )}

      {showReopenDialog && (
        <ReopenConsultationDialog isSaving={reopenMutation.isPending} onConfirm={handleReopen} onCancel={() => setShowReopenDialog(false)} />
      )}

      <OpdConsultationPrintTemplate header={header} note={note} />
    </div>
  );
}
