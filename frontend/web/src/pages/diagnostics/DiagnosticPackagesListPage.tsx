import type { DiagnosticPackage } from '@hms/shared';
import { Loader2, PackageSearch, Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { useAuth } from '@/features/auth/AuthContext';
import { DiagnosticPackageTable, useDeleteDiagnosticPackageMutation, useDiagnosticPackagesQuery } from '@/features/diagnostics';

export default function DiagnosticPackagesListPage() {
  const [page, setPage] = useState(1);
  const [packagePendingDelete, setPackagePendingDelete] = useState<DiagnosticPackage | null>(null);

  const { hasPermission } = useAuth();
  const { data, isPending, isError, error } = useDiagnosticPackagesQuery({ page, pageSize: 20, sort: 'name' });
  const deleteMutation = useDeleteDiagnosticPackageMutation();

  function handleConfirmDelete() {
    if (!packagePendingDelete) return;
    deleteMutation.mutate(packagePendingDelete.id, { onSuccess: () => setPackagePendingDelete(null) });
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
            <PackageSearch className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Packages</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Bundled test packages at a fixed price.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex justify-end">
          {hasPermission('diagnostics.create') && (
            <Button asChild className="gap-1.5">
              <Link to="/diagnostics/lab/packages/new">
                <Plus className="h-4 w-4" />
                Add Package
              </Link>
            </Button>
          )}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading packages…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load packages.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No packages found</p>
              <p className="text-sm text-muted-foreground">Add the first package to get started.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <DiagnosticPackageTable packages={data.items} onDeleteRequested={setPackagePendingDelete} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {packagePendingDelete && (
          <Dialog open onOpenChange={(open) => !open && setPackagePendingDelete(null)}>
            <DialogContent role="alertdialog" aria-labelledby="delete-diagnostic-package-title">
              <DialogHeader>
                <DialogTitle id="delete-diagnostic-package-title">Delete package?</DialogTitle>
                <DialogDescription>
                  This will remove <strong className="text-foreground">{packagePendingDelete.name}</strong> ({packagePendingDelete.code})
                  from active lists. The record is retained (soft delete).
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button variant="outline" onClick={() => setPackagePendingDelete(null)} disabled={deleteMutation.isPending}>
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
