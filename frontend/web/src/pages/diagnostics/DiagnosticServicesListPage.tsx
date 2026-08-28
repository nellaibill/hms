import type { DiagnosticService } from '@hms/shared';
import { FlaskConical, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
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
  const [servicePendingDelete, setServicePendingDelete] = useState<DiagnosticService | null>(null);

  const { data, isPending, isError, error } = useDiagnosticServicesQuery({
    page,
    pageSize: 20,
    sort: 'name',
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

  function handleConfirmDelete() {
    if (!servicePendingDelete) return;
    deleteMutation.mutate(servicePendingDelete.id, { onSuccess: () => setServicePendingDelete(null) });
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
            <FlaskConical className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Services</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">The Laboratory/Radiology test catalog — pricing, category, and outsourcing.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <DiagnosticServiceListToolbar filters={filters} onChange={handleFiltersChange} categories={categoriesQuery.data?.items ?? []} />

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
