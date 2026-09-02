import type { ImportBatch } from '@hms/shared';
import { History, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { RequirePermission } from '../../features/auth/RequirePermission';
import { Pagination } from '../../features/patients';
import { usePatientImportBatchesQuery } from '../../features/patientImport';

const IMPORT_PERMISSION = 'patient-management.import';
const PAGE_SIZE = 20;

const STATUS_BADGE: Record<ImportBatch['status'], { label: string; variant: 'secondary' | 'warning' | 'success' | 'destructive' }> = {
  Validating: { label: 'Validating', variant: 'secondary' },
  ReadyForReview: { label: 'Ready For Review', variant: 'warning' },
  Committing: { label: 'Committing', variant: 'secondary' },
  Completed: { label: 'Completed', variant: 'success' },
  Failed: { label: 'Failed', variant: 'destructive' },
};

/** Permanent audit trail of every bulk import run — who ran it, when, and what happened.
 * Every row links back into PatientBulkImportPage's own review screen for that batch. */
export default function PatientImportHistoryPage() {
  const [page, setPage] = useState(1);
  const { data, isPending, isError, error } = usePatientImportBatchesQuery({ page, pageSize: PAGE_SIZE });

  return (
    <RequirePermission permission={IMPORT_PERMISSION}>
      <div className="flex flex-1 flex-col">
        <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
          <div className="flex items-center gap-3">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
              <History className="h-5 w-5" />
            </span>
            <h1 className="text-xl font-semibold tracking-tight">Import History</h1>
          </div>
          <p className="max-w-2xl text-sm text-page-banner-foreground/85">
            Every bulk patient import run on this account, newest first.
          </p>
        </div>

        <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
          <div className="flex justify-end">
            <Link to="/patients/import" className="text-sm text-link hover:underline">
              Start a new import →
            </Link>
          </div>

          {isPending && (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading…
            </div>
          )}

          {isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error instanceof Error ? error.message : 'Failed to load import history.'}
            </p>
          )}

          {!isPending && !isError && data && data.items.length === 0 && (
            <Card className="border-dashed">
              <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
                <p className="text-sm font-medium text-foreground">No imports yet.</p>
                <p className="text-sm text-muted-foreground">
                  <Link to="/patients/import" className="text-link hover:underline">
                    Start your first bulk import
                  </Link>
                  .
                </p>
              </CardContent>
            </Card>
          )}

          {!isPending && !isError && data && data.items.length > 0 && (
            <div className="flex flex-col gap-3">
              <div className="overflow-x-auto rounded-md border">
                <table className="w-full text-left text-sm">
                  <thead className="bg-muted/50 text-xs uppercase text-muted-foreground">
                    <tr>
                      <th className="px-3 py-2">File</th>
                      <th className="px-3 py-2">Uploaded</th>
                      <th className="px-3 py-2">Status</th>
                      <th className="px-3 py-2">Total</th>
                      <th className="px-3 py-2">Created</th>
                      <th className="px-3 py-2">Skipped</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.items.map((batch) => {
                      const status = STATUS_BADGE[batch.status];
                      return (
                        <tr key={batch.id} className="border-t hover:bg-accent/40">
                          <td className="px-3 py-2">
                            <Link to={`/patients/import/${batch.id}`} className="font-medium text-link hover:underline">
                              {batch.fileName}
                            </Link>
                          </td>
                          <td className="px-3 py-2 text-muted-foreground">{new Date(batch.uploadedAt).toLocaleString()}</td>
                          <td className="px-3 py-2">
                            <Badge variant={status.variant}>{status.label}</Badge>
                          </td>
                          <td className="px-3 py-2">{batch.totalRows}</td>
                          <td className="px-3 py-2">{batch.createdRows}</td>
                          <td className="px-3 py-2">{batch.invalidRows + batch.commitFailedRows}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
              <Pagination meta={data.meta} onPageChange={setPage} />
            </div>
          )}
        </div>
      </div>
    </RequirePermission>
  );
}
