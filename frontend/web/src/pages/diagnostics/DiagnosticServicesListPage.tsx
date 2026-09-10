import type { DiagnosticService } from '@hms/shared';
import { FlaskConical, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { PageBanner } from '@/components/PageBanner';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import {
  DiagnosticServiceListToolbar,
  DiagnosticServiceTable,
  useDeleteDiagnosticServiceMutation,
  useDiagnosticCategoriesQuery,
  useDiagnosticProvidersQuery,
  useDiagnosticServicesQuery,
  type DiagnosticServiceFilters,
} from '@/features/diagnostics';

const emptyFilters: DiagnosticServiceFilters = { categoryId: undefined, serviceType: undefined, isActive: undefined };

export default function DiagnosticServicesListPage() {
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState<DiagnosticServiceFilters>(emptyFilters);
  const [search, setSearch] = useState('');
  const [servicePendingDelete, setServicePendingDelete] = useState<DiagnosticService | null>(null);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useDiagnosticServicesQuery({
    page,
    pageSize: 20,
    sort: 'name',
    search: debouncedSearch || undefined,
    categoryId: filters.categoryId,
    serviceType: filters.serviceType,
    isActive: filters.isActive,
  });
  // Resolved client-side against these two ~100-row lookup queries so the table can show
  // Category/Provider names instead of raw ids — same "prime a 100-row lookup query" pattern
  // PharmacyHubPage uses for reorder levels.
  const categoriesQuery = useDiagnosticCategoriesQuery({ pageSize: 200, sort: 'name' });
  const providersQuery = useDiagnosticProvidersQuery({ pageSize: 200, sort: 'name' });
  const deleteMutation = useDeleteDiagnosticServiceMutation();

  const categoriesById = new Map((categoriesQuery.data?.items ?? []).map((category) => [category.id, category]));
  const providersById = new Map((providersQuery.data?.items ?? []).map((provider) => [provider.id, provider]));

  function handleFiltersChange(next: DiagnosticServiceFilters) {
    setFilters(next);
    setPage(1);
  }

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleConfirmDelete() {
    if (!servicePendingDelete) return;
    deleteMutation.mutate(servicePendingDelete.id, { onSuccess: () => setServicePendingDelete(null) });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={FlaskConical}
        title="Services"
        subtitle="The Laboratory/Radiology test catalog — pricing, category, and outsourcing."
        backTo="/admin/masters"
        backLabel="Back to Hospital Reference Data"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <DiagnosticServiceListToolbar
          filters={filters}
          onChange={handleFiltersChange}
          categories={categoriesQuery.data?.items ?? []}
          search={search}
          onSearchChange={handleSearchChange}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading services…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load services.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No services found</p>
              <p className="text-sm text-muted-foreground">Try a different filter, or add the first service to get started.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <DiagnosticServiceTable
              services={data.items}
              categoriesById={categoriesById}
              providersById={providersById}
              onDeleteRequested={setServicePendingDelete}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {servicePendingDelete && (
          <Dialog open onOpenChange={(open) => !open && setServicePendingDelete(null)}>
            <DialogContent role="alertdialog" aria-labelledby="delete-diagnostic-service-title">
              <DialogHeader>
                <DialogTitle id="delete-diagnostic-service-title">Delete service?</DialogTitle>
                <DialogDescription>
                  This will remove <strong className="text-foreground">{servicePendingDelete.name}</strong> ({servicePendingDelete.code})
                  from active lists. The record is retained (soft delete).
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button variant="outline" onClick={() => setServicePendingDelete(null)} disabled={deleteMutation.isPending}>
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
