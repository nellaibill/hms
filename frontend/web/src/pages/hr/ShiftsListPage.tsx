import type { Shift } from '@hms/shared';
import { Clock, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
import { PageBanner } from '@/components/PageBanner';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { DeleteShiftDialog, ShiftListToolbar, ShiftTable, useDeleteShiftMutation, useShiftsQuery } from '../../features/shifts';

export default function ShiftsListPage() {
  const [search, setSearch] = useState('');
  const [isActive, setIsActive] = useState<boolean | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('code');
  const [shiftPendingDelete, setShiftPendingDelete] = useState<Shift | null>(null);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useShiftsQuery({
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
    isActive,
  });

  const deleteMutation = useDeleteShiftMutation();

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleIsActiveChange(value: boolean | undefined) {
    setIsActive(value);
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  function handleConfirmDelete() {
    if (!shiftPendingDelete) {
      return;
    }
    deleteMutation.mutate(shiftPendingDelete.id, {
      onSuccess: () => setShiftPendingDelete(null),
    });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Clock}
        title="Shift Management"
        subtitle="Define shift codes, timings, breaks, and grace periods."
        backTo="/admin/hr"
        backLabel="Back to HR"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <ShiftListToolbar
          search={search}
          onSearchChange={handleSearchChange}
          isActive={isActive}
          onIsActiveChange={handleIsActiveChange}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading shifts…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load shifts.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No shifts found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Create the first shift to get started.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <ShiftTable shifts={data.items} sort={sort} onSortChange={handleSortChange} onDeleteRequested={setShiftPendingDelete} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {shiftPendingDelete && (
          <DeleteShiftDialog
            shift={shiftPendingDelete}
            isDeleting={deleteMutation.isPending}
            onConfirm={handleConfirmDelete}
            onCancel={() => setShiftPendingDelete(null)}
          />
        )}
      </div>
    </div>
  );
}
