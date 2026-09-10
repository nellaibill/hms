import type { DiagnosticPackage } from '@hms/shared';
import { Loader2, PackageSearch, Plus, Search } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { PageBanner } from '@/components/PageBanner';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { useAuth } from '@/features/auth/AuthContext';
import { DiagnosticPackageTable, useDeleteDiagnosticPackageMutation, useDiagnosticPackagesQuery } from '@/features/diagnostics';

export default function DiagnosticPackagesListPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [packagePendingDelete, setPackagePendingDelete] = useState<DiagnosticPackage | null>(null);

  const { hasPermission } = useAuth();
  const debouncedSearch = useDebouncedValue(search);
  const { data, isPending, isError, error } = useDiagnosticPackagesQuery({
    page,
    pageSize: 20,
    sort: 'name',
    search: debouncedSearch || undefined,
  });
  const deleteMutation = useDeleteDiagnosticPackageMutation();

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleConfirmDelete() {
    if (!packagePendingDelete) return;
    deleteMutation.mutate(packagePendingDelete.id, { onSuccess: () => setPackagePendingDelete(null) });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={PackageSearch}
        title="Packages"
        subtitle="Bundled test packages at a fixed price."
        backTo="/admin/masters"
        backLabel="Back to Hospital Reference Data"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap items-center gap-3">
          <div className="relative w-64">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              type="search"
              placeholder="Search packages…"
              value={search}
              onChange={(event) => handleSearchChange(event.target.value)}
              aria-label="Search packages"
              className="pl-9"
            />
          </div>

          {hasPermission('diagnostics.create') && (
            <Button asChild className="ml-auto gap-1.5">
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
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Add the first package to get started.'}
              </p>
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
