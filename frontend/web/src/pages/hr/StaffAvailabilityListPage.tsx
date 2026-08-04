import type { StaffAvailability } from '@hms/shared';
import { CalendarClock, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
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
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/admin/hr" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to HR
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <CalendarClock className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Staff Availability</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Track when staff are available or unavailable, and why.</p>
      </div>

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
