import type { StaffAvailability } from '@hms/shared';
import { CalendarClock, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
import { PageBanner } from '@/components/PageBanner';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import {
  DeleteStaffAvailabilityDialog,
  StaffAvailabilityListToolbar,
  StaffAvailabilityTable,
  useDeleteStaffAvailabilityMutation,
  useStaffAvailabilityQuery,
} from '../../features/staffAvailability';

export default function StaffAvailabilityListPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('-startDate');
  const [recordPendingDelete, setRecordPendingDelete] = useState<StaffAvailability | null>(null);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useStaffAvailabilityQuery({
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
  });

  const deleteMutation = useDeleteStaffAvailabilityMutation();

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  function handleConfirmDelete() {
    if (!recordPendingDelete) {
      return;
    }
    deleteMutation.mutate(recordPendingDelete.id, {
      onSuccess: () => setRecordPendingDelete(null),
    });
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={CalendarClock}
        title="Staff Availability"
        subtitle="Track when staff are available or unavailable, and why."
        backTo="/admin/hr"
        backLabel="Back to HR"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <StaffAvailabilityListToolbar search={search} onSearchChange={handleSearchChange} />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading availability records…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load staff availability.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No availability records found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Create the first availability record to get started.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <StaffAvailabilityTable records={data.items} sort={sort} onSortChange={handleSortChange} onDeleteRequested={setRecordPendingDelete} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {recordPendingDelete && (
          <DeleteStaffAvailabilityDialog
            record={recordPendingDelete}
            isDeleting={deleteMutation.isPending}
            onConfirm={handleConfirmDelete}
            onCancel={() => setRecordPendingDelete(null)}
          />
        )}
      </div>
    </div>
  );
}
