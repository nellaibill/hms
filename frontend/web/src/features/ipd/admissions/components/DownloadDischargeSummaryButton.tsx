import { ApiError, type Admission } from '@hms/shared';
import { Download, FileX, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { exportDischargeSummaryPdf, useStaffDirectoryQuery } from '@/features/dischargeSummary';
import { dischargeSummaryApi, patientsApi } from '@/services/apiClient';

interface DownloadDischargeSummaryButtonProps {
  admission: Admission;
}

/**
 * Fetches the discharge summary + patient on click rather than eagerly per row — a discharged
 * admission isn't guaranteed to have a discharge summary yet (creating one is a separate,
 * explicit step from discharge itself), so a 404 here is an expected outcome, not an error.
 */
export function DownloadDischargeSummaryButton({ admission }: DownloadDischargeSummaryButtonProps) {
  const [status, setStatus] = useState<'idle' | 'loading' | 'unavailable'>('idle');
  const directoryQuery = useStaffDirectoryQuery();

  async function handleDownload() {
    setStatus('loading');
    try {
      const [summary, patient] = await Promise.all([
        dischargeSummaryApi.getByAdmissionId(admission.id),
        patientsApi.getPatientById(admission.patientId),
      ]);
      exportDischargeSummaryPdf(summary, patient, admission, directoryQuery.data ?? []);
      setStatus('idle');
    } catch (error) {
      setStatus(error instanceof ApiError && error.status === 404 ? 'unavailable' : 'idle');
    }
  }

  if (status === 'unavailable') {
    return (
      <span className="inline-flex items-center gap-1 text-xs text-muted-foreground">
        <FileX className="h-3.5 w-3.5" />
        No summary
      </span>
    );
  }

  return (
    <Button type="button" variant="ghost" size="sm" className="h-7 gap-1.5 px-2 text-xs" onClick={handleDownload} disabled={status === 'loading'}>
      {status === 'loading' ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Download className="h-3.5 w-3.5" />}
      PDF
    </Button>
  );
}
