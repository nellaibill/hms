import { ApiError } from '@hms/shared';
import { ArrowLeft, Download, Loader2, Pencil } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAdmissionQuery } from '@/features/ipd/admissions';
import { usePatientQuery } from '@/features/patients';
import {
  DischargeSummaryDetails,
  exportDischargeSummaryPdf,
  useDischargeSummaryByAdmissionQuery,
  useStaffDirectoryQuery,
} from '@/features/dischargeSummary';

export default function DischargeSummaryViewPage() {
  const { admissionId } = useParams<{ admissionId: string }>();

  const { data: admission, isPending: isAdmissionPending } = useAdmissionQuery(admissionId);
  const { data: patient, isPending: isPatientPending } = usePatientQuery(admission?.patientId);
  const summaryQuery = useDischargeSummaryByAdmissionQuery(admissionId);
  const directoryQuery = useStaffDirectoryQuery();

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
  const staff = directoryQuery.data ?? [];

  function handleDownload() {
    if (!admission || !patient) return;
    exportDischargeSummaryPdf(summary, patient, admission, staff);
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to={`/clinical/ipd/admissions/${admission.id}`} className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <ArrowLeft className="h-4 w-4" />
          Back to admission
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <h1 className="text-xl font-semibold tracking-tight">
            Discharge Summary — {patient.firstName} {patient.lastName}
          </h1>
          <Badge variant={summary.status === 'Finalized' ? 'success' : 'secondary'}>{summary.status}</Badge>
        </div>
        <p className="font-mono text-sm text-page-banner-foreground/85">{admission.admissionNumber}</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap gap-3">
          <Button type="button" variant="outline" className="gap-1.5" onClick={handleDownload}>
            <Download className="h-4 w-4" />
            Download PDF
          </Button>
          {summary.status === 'Draft' && (
            <Button asChild variant="outline" className="gap-1.5">
              <Link to={`/clinical/ipd/admissions/${admission.id}/discharge-summary/edit`}>
                <Pencil className="h-4 w-4" />
                Continue Editing
              </Link>
            </Button>
          )}
        </div>

        <DischargeSummaryDetails summary={summary} staff={staff} />
      </div>
    </div>
  );
}
