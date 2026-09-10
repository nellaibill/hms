import type { DiagnosticProvider } from '@hms/shared';
import { Building2, Loader2, Plus, Search } from 'lucide-react';
import { useState } from 'react';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { PageBanner } from '@/components/PageBanner';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { useAuth } from '@/features/auth/AuthContext';
import {
  DiagnosticProviderFormDialog,
  DiagnosticProviderTable,
  useDeleteDiagnosticProviderMutation,
  useDiagnosticProvidersQuery,
} from '@/features/diagnostics';

export default function ExternalLabsListPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [formDialog, setFormDialog] = useState<{ mode: 'create' | 'edit'; provider?: DiagnosticProvider } | null>(null);
  const [providerPendingDelete, setProviderPendingDelete] = useState<DiagnosticProvider | null>(null);

  const { hasPermission } = useAuth();
  const debouncedSearch = useDebouncedValue(search);
  const { data, isPending, isError, error } = useDiagnosticProvidersQuery({
    page,
    pageSize: 20,
    sort: 'name',
    search: debouncedSearch || undefined,
  });
  const deleteMutation = useDeleteDiagnosticProviderMutation();

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleConfirmDelete() {
    if (!providerPendingDelete) return;
    deleteMutation.mutate(providerPendingDelete.id, { onSuccess: () => setProviderPendingDelete(null) });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Building2}
        title="External Labs"
        subtitle="Providers tests are outsourced to."
        backTo="/admin/masters"
        backLabel="Back to Hospital Reference Data"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap items-center gap-3">
          <div className="relative w-64">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              type="search"
              placeholder="Search external labs…"
              value={search}
              onChange={(event) => handleSearchChange(event.target.value)}
              aria-label="Search external labs"
              className="pl-9"
            />
          </div>

          {hasPermission('diagnostics.create') && (
            <Button className="ml-auto gap-1.5" onClick={() => setFormDialog({ mode: 'create' })}>
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
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Add the first external lab to get started.'}
              </p>
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
