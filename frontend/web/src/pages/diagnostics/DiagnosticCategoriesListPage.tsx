import type { DiagnosticCategory } from '@hms/shared';
import { ListTree, Loader2, Plus, Search } from 'lucide-react';
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
  DiagnosticCategoryFormDialog,
  DiagnosticCategoryTable,
  useDeleteDiagnosticCategoryMutation,
  useDiagnosticCategoriesQuery,
} from '@/features/diagnostics';

export default function DiagnosticCategoriesListPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [formDialog, setFormDialog] = useState<{ mode: 'create' | 'edit'; category?: DiagnosticCategory } | null>(null);
  const [categoryPendingDelete, setCategoryPendingDelete] = useState<DiagnosticCategory | null>(null);

  const { hasPermission } = useAuth();
  const debouncedSearch = useDebouncedValue(search);
  const { data, isPending, isError, error } = useDiagnosticCategoriesQuery({
    page,
    pageSize: 20,
    sort: 'name',
    search: debouncedSearch || undefined,
  });
  const deleteMutation = useDeleteDiagnosticCategoryMutation();

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleConfirmDelete() {
    if (!categoryPendingDelete) return;
    deleteMutation.mutate(categoryPendingDelete.id, { onSuccess: () => setCategoryPendingDelete(null) });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={ListTree}
        title="Categories"
        subtitle="Test categories used to organize the service catalog."
        backTo="/admin/masters"
        backLabel="Back to Hospital Reference Data"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <div className="flex flex-wrap items-center gap-3">
          <div className="relative w-64">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              type="search"
              placeholder="Search categories…"
              value={search}
              onChange={(event) => handleSearchChange(event.target.value)}
              aria-label="Search categories"
              className="pl-9"
            />
          </div>

          {hasPermission('diagnostics.create') && (
            <Button className="ml-auto gap-1.5" onClick={() => setFormDialog({ mode: 'create' })}>
              <Plus className="h-4 w-4" />
              Add Category
            </Button>
          )}
        </div>

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading categories…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load categories.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No categories found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Add the first category to get started.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <DiagnosticCategoryTable
              categories={data.items}
              onEditRequested={(category) => setFormDialog({ mode: 'edit', category })}
              onDeleteRequested={setCategoryPendingDelete}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {formDialog && (
          <DiagnosticCategoryFormDialog mode={formDialog.mode} category={formDialog.category} onClose={() => setFormDialog(null)} />
        )}

        {categoryPendingDelete && (
          <Dialog open onOpenChange={(open) => !open && setCategoryPendingDelete(null)}>
            <DialogContent role="alertdialog" aria-labelledby="delete-diagnostic-category-title">
              <DialogHeader>
                <DialogTitle id="delete-diagnostic-category-title">Delete category?</DialogTitle>
                <DialogDescription>
                  This will remove <strong className="text-foreground">{categoryPendingDelete.name}</strong> ({categoryPendingDelete.code})
                  from active lists. The record is retained (soft delete).
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button variant="outline" onClick={() => setCategoryPendingDelete(null)} disabled={deleteMutation.isPending}>
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
