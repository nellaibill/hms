import type { DiagnosticProvider } from '@hms/shared';
import { Building2, Loader2, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useAuth } from '@/features/auth/AuthContext';
import {
  DiagnosticProviderFormDialog,
  DiagnosticProviderTable,
  useDeleteDiagnosticProviderMutation,
  useDiagnosticProvidersQuery,
} from '@/features/diagnostics';

export default function ExternalLabsListPage() {
  const [page, setPage] = useState(1);
  const [formDialog, setFormDialog] = useState<{ mode: 'create' | 'edit'; provider?: DiagnosticProvider } | null>(null);
  const [providerPendingDelete, setProviderPendingDelete] = useState<DiagnosticProvider | null>(null);

  const { hasPermission } = useAuth();
  const { data, isPending, isError, error } = useDiagnosticProvidersQuery({ page, pageSize: 20, sort: 'name' });
  const deleteMutation = useDeleteDiagnosticProviderMutation();

  function handleConfirmDelete() {
    if (!providerPendingDelete) return;
    deleteMutation.mutate(providerPendingDelete.id, { onSuccess: () => setProviderPendingDelete(null) });
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/diagnostics/lab" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to Central Laboratory
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Building2 className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">External Labs</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Providers tests are outsourced to.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex justify-end">
          {hasPermission('diagnostics.create') && (
            <Button className="gap-1.5" onClick={() => setFormDialog({ mode: 'create' })}>
              <Plus className="h-4 w-4" />
              Add External Lab
            </Button>
          )}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading external labs…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load external labs.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No external labs found</p>
              <p className="text-sm text-muted-foreground">Add the first external lab to get started.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <DiagnosticProviderTable
              providers={data.items}
              onEditRequested={(provider) => setFormDialog({ mode: 'edit', provider })}
              onDeleteRequested={setProviderPendingDelete}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {formDialog && (
          <DiagnosticProviderFormDialog mode={formDialog.mode} provider={formDialog.provider} onClose={() => setFormDialog(null)} />
        )}

        {providerPendingDelete && (
          <Dialog open onOpenChange={(open) => !open && setProviderPendingDelete(null)}>
            <DialogContent role="alertdialog" aria-labelledby="delete-diagnostic-provider-title">
              <DialogHeader>
                <DialogTitle id="delete-diagnostic-provider-title">Delete external lab?</DialogTitle>
                <DialogDescription>
                  This will remove <strong className="text-foreground">{providerPendingDelete.name}</strong> ({providerPendingDelete.code})
                  from active lists. The record is retained (soft delete).
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button variant="outline" onClick={() => setProviderPendingDelete(null)} disabled={deleteMutation.isPending}>
                  Cancel
                </Button>
                <Button variant="destructive" onClick={handleConfirmDelete} disabled={deleteMutation.isPending}>
                  {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>
    </div>
  );
}
