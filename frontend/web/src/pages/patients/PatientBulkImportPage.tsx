import { AlertTriangle, CheckCircle2, CloudUpload, Download, FileSpreadsheet, History, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { FileChooserButton } from '@/components/ui/file-chooser-button';
import { useToast } from '@/components/ui/toast-context';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { Pagination } from '../../features/patients';
import {
  downloadBlob,
  useCommitPatientImportBatchMutation,
  usePatientImportBatchQuery,
  usePatientImportRowsQuery,
  useUploadPatientImportMutation,
} from '../../features/patientImport';
import { patientImportApi } from '../../services/apiClient';

const IMPORT_PERMISSION = 'patient-management.import';

/** Enterprise bulk patient import: download template → upload → validate (async, nothing
 * written yet) → review skipped rows → explicit confirm → commit. The URL's :batchId is the
 * single source of truth for "which batch am I looking at" — refreshing or sharing the link
 * lands back on the same review screen, and PatientImportHistoryPage links here too. */
export default function PatientBulkImportPage() {
  const { batchId: routeBatchId } = useParams<{ batchId?: string }>();
  const batchId = routeBatchId ?? null;
  const navigate = useNavigate();
  const { toast } = useToast();

  const [rowPage, setRowPage] = useState(1);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [isDownloadingTemplate, setIsDownloadingTemplate] = useState(false);

  const { data: batch } = usePatientImportBatchQuery(batchId);
  const uploadMutation = useUploadPatientImportMutation();
  const commitMutation = useCommitPatientImportBatchMutation();

  const showReview = !!batch && ['ReadyForReview', 'Committing', 'Completed'].includes(batch.status);
  const hasSkippedRows = !!batch && batch.invalidRows + batch.commitFailedRows > 0;
  const { data: skippedRows } = usePatientImportRowsQuery(
    batchId,
    { status: 'Invalid', page: rowPage, pageSize: 25 },
    { enabled: showReview && hasSkippedRows },
  );

  async function handleDownloadTemplate() {
    setIsDownloadingTemplate(true);
    try {
      const blob = await patientImportApi.getTemplate();
      downloadBlob(blob, 'patient_import_template.xlsx');
    } catch (err) {
      toast({
        title: 'Download failed',
        description: err instanceof Error ? err.message : 'Could not download the template.',
        variant: 'error',
      });
    } finally {
      setIsDownloadingTemplate(false);
    }
  }

  function handleFileSelected(file: File) {
    uploadMutation.mutate(file, {
      onSuccess: (result) => {
        setRowPage(1);
        navigate(`/patients/import/${result.id}`, { replace: true });
      },
      onError: (err) =>
        toast({
          title: 'Upload failed',
          description: err instanceof Error ? err.message : 'Could not upload the file.',
          variant: 'error',
        }),
    });
  }

  function handleConfirmImport() {
    if (!batchId) return;
    commitMutation.mutate(batchId, {
      onSuccess: () => setConfirmOpen(false),
      onError: (err) => {
        setConfirmOpen(false);
        toast({
          title: 'Import failed',
          description: err instanceof Error ? err.message : 'Could not start the import.',
          variant: 'error',
        });
      },
    });
  }

  async function handleDownloadReport() {
    if (!batchId) return;
    try {
      const blob = await patientImportApi.getReport(batchId);
      downloadBlob(blob, `patient_import_${batchId}_errors.xlsx`);
    } catch (err) {
      toast({
        title: 'Download failed',
        description: err instanceof Error ? err.message : 'Could not download the error report.',
        variant: 'error',
      });
    }
  }

  return (
    <RequirePermission permission={IMPORT_PERMISSION}>
      <>
        <div className="flex flex-1 flex-col">
          <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
            <div className="flex items-center gap-3">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
                <CloudUpload className="h-5 w-5" />
              </span>
              <h1 className="text-xl font-semibold tracking-tight">Bulk Patient Import</h1>
            </div>
            <p className="max-w-2xl text-sm text-page-banner-foreground/85">
              Upload a filled-in Excel template to register many patients at once. Every row is checked first — nothing
              is saved until you review and confirm.
            </p>
          </div>

          <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <Button variant="outline" onClick={handleDownloadTemplate} disabled={isDownloadingTemplate}>
                <Download className="h-4 w-4" />
                {isDownloadingTemplate ? 'Preparing…' : 'Download Template'}
              </Button>
              <Button variant="ghost" asChild>
                <Link to="/patients/import-history">
                  <History className="h-4 w-4" />
                  Import History
                </Link>
              </Button>
            </div>

            {!batch && (
              <Card>
                <CardContent className="flex flex-col items-center gap-3 py-12 text-center">
                  <FileSpreadsheet className="h-8 w-8 text-muted-foreground" />
                  <p className="text-sm font-medium text-foreground">Upload a filled-in template</p>
                  <p className="max-w-md text-sm text-muted-foreground">
                    Download the template above, fill in one row per patient, then upload it here. Fields marked with *
                    on the template are required.
                  </p>
                  <FileChooserButton
                    id="patient-import-file"
                    accept=".xlsx"
                    disabled={uploadMutation.isPending}
                    onFileSelected={handleFileSelected}
                    status={uploadMutation.isPending ? 'Uploading…' : undefined}
                  />
                </CardContent>
              </Card>
            )}

            {batch && batch.status === 'Validating' && (
              <Card>
                <CardContent className="flex flex-col items-center gap-2 py-12 text-center">
                  <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
                  <p className="text-sm font-medium text-foreground">Validating {batch.fileName}…</p>
                  <p className="text-sm text-muted-foreground">
                    Checking every row against your patient records. This can take a few minutes for large files.
                  </p>
                </CardContent>
              </Card>
            )}

            {batch && batch.status === 'Failed' && (
              <Card className="border-destructive/40">
                <CardContent className="flex flex-col items-center gap-2 py-12 text-center">
                  <AlertTriangle className="h-6 w-6 text-destructive" />
                  <p className="text-sm font-medium text-foreground">Couldn't read {batch.fileName}</p>
                  <p className="max-w-md text-sm text-muted-foreground">
                    The file isn't a valid copy of the template (wrong format, missing "Patients" sheet, or corrupted).
                    Download a fresh template and try again.
                  </p>
                  <Button variant="outline" onClick={() => navigate('/patients/import')}>
                    Try Again
                  </Button>
                </CardContent>
              </Card>
            )}

            {showReview && batch && (
              <>
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                  <SummaryStat label="Total Rows" value={batch.totalRows} />
                  <SummaryStat label="Valid" value={batch.validRows} tone="success" />
                  <SummaryStat
                    label="Skipped"
                    value={batch.invalidRows + batch.commitFailedRows}
                    tone={batch.invalidRows + batch.commitFailedRows > 0 ? 'destructive' : undefined}
                  />
                  <SummaryStat label="Created" value={batch.createdRows} tone={batch.status === 'Completed' ? 'success' : undefined} />
                </div>

                {hasSkippedRows && (
                  <Card>
                    <CardContent className="flex flex-col gap-3 py-4">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <p className="text-sm font-medium text-foreground">Skipped rows</p>
                        <Button variant="outline" size="sm" onClick={handleDownloadReport}>
                          <Download className="h-4 w-4" />
                          Download Error Report
                        </Button>
                      </div>
                      <div className="overflow-x-auto rounded-md border">
                        <table className="w-full text-left text-sm">
                          <thead className="bg-muted/50 text-xs uppercase text-muted-foreground">
                            <tr>
                              <th className="px-3 py-2">Row</th>
                              <th className="px-3 py-2">Name</th>
                              <th className="px-3 py-2">Reason</th>
                            </tr>
                          </thead>
                          <tbody>
                            {skippedRows?.items.map((row) => (
                              <tr key={row.id} className="border-t">
                                <td className="px-3 py-2 align-top">{row.rowNumber}</td>
                                <td className="px-3 py-2 align-top">
                                  {row.rawData['First Name'] ?? ''} {row.rawData['Last Name'] ?? ''}
                                </td>
                                <td className="px-3 py-2 align-top text-muted-foreground">
                                  {row.errors.map((e) => `${e.field}: ${e.message}`).join('; ')}
                                </td>
                              </tr>
                            ))}
                            {skippedRows?.items.length === 0 && (
                              <tr>
                                <td colSpan={3} className="px-3 py-6 text-center text-muted-foreground">
                                  No skipped rows on this page.
                                </td>
                              </tr>
                            )}
                          </tbody>
                        </table>
                      </div>
                      {skippedRows && <Pagination meta={skippedRows.meta} onPageChange={setRowPage} />}
                    </CardContent>
                  </Card>
                )}

                {batch.status === 'ReadyForReview' && (
                  <div className="flex justify-end">
                    <Button onClick={() => setConfirmOpen(true)} disabled={batch.validRows === 0}>
                      <CheckCircle2 className="h-4 w-4" />
                      Confirm Import ({batch.validRows} patient{batch.validRows === 1 ? '' : 's'})
                    </Button>
                  </div>
                )}

                {batch.status === 'Committing' && (
                  <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Creating patient records…
                  </div>
                )}

                {batch.status === 'Completed' && (
                  <div className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-success/30 bg-success/10 px-4 py-3">
                    <p className="text-sm text-foreground">
                      <strong>{batch.createdRows}</strong> patient{batch.createdRows === 1 ? '' : 's'} created.
                      {batch.commitFailedRows > 0 && (
                        <>
                          {' '}
                          {batch.commitFailedRows} row{batch.commitFailedRows === 1 ? '' : 's'} failed at commit — see the
                          error report above.
                        </>
                      )}
                    </p>
                    <Button variant="outline" onClick={() => navigate('/patients/import')}>
                      Start a New Import
                    </Button>
                  </div>
                )}
              </>
            )}
          </div>
        </div>

        <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
          <DialogContent role="alertdialog" aria-labelledby="confirm-import-title">
            <DialogHeader>
              <DialogTitle id="confirm-import-title">Confirm import?</DialogTitle>
              <DialogDescription>
                This will create <strong className="text-foreground">{batch?.validRows ?? 0}</strong> new patient
                record{batch?.validRows === 1 ? '' : 's'}. This cannot be undone.
                {batch && batch.invalidRows > 0 && (
                  <> The {batch.invalidRows} skipped row{batch.invalidRows === 1 ? '' : 's'} will not be imported.</>
                )}
              </DialogDescription>
            </DialogHeader>
            <DialogFooter>
              <Button variant="outline" onClick={() => setConfirmOpen(false)} disabled={commitMutation.isPending}>
                Cancel
              </Button>
              <Button onClick={handleConfirmImport} disabled={commitMutation.isPending}>
                {commitMutation.isPending ? 'Confirming…' : 'Confirm Import'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </>
    </RequirePermission>
  );
}

function SummaryStat({ label, value, tone }: { label: string; value: number; tone?: 'success' | 'destructive' }) {
  const valueClass =
    tone === 'success' ? 'text-success' : tone === 'destructive' ? 'text-destructive' : 'text-foreground';
  return (
    <Card>
      <CardContent className="flex flex-col gap-1 py-4">
        <p className="text-xs font-medium uppercase text-muted-foreground">{label}</p>
        <p className={`text-2xl font-semibold ${valueClass}`}>{value}</p>
      </CardContent>
    </Card>
  );
}
