import type { Bed, BedStatus } from '@hms/shared';
import { BedSingle, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardContent } from '@/components/ui/card';
import { Pagination } from '@/components/Pagination';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { useWardsQuery } from '../../features/ipd/wards';
import { BedListToolbar, BedTable, DeleteBedDialog, useBedsQuery, useDeleteBedMutation } from '../../features/ipd/beds';

export default function BedsListPage() {
  const [search, setSearch] = useState('');
  const [wardId, setWardId] = useState<string | undefined>(undefined);
  const [status, setStatus] = useState<BedStatus | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('bedNumber');
  const [bedPendingDelete, setBedPendingDelete] = useState<Bed | null>(null);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useBedsQuery({
    page,
    pageSize: 20,
    sort,
    search: debouncedSearch || undefined,
    wardId,
    status,
  });

  // Loaded once for the whole list (not per-row) to resolve WardId -> display label in the
  // table without an N+1 round-trip per bed.
  const wardsForLabels = useWardsQuery({ pageSize: 100 });
  const wardLabels = Object.fromEntries((wardsForLabels.data?.items ?? []).map((ward) => [ward.id, `${ward.name} (${ward.code})`]));

  const deleteMutation = useDeleteBedMutation();

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleWardIdChange(value: string | undefined) {
    setWardId(value);
    setPage(1);
  }

  function handleStatusChange(value: BedStatus | undefined) {
    setStatus(value);
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  function handleConfirmDelete() {
    if (!bedPendingDelete) {
      return;
    }
    deleteMutation.mutate(bedPendingDelete.id, {
      onSuccess: () => setBedPendingDelete(null),
    });
  }

  return (
    <div className="flex flex-1 flex-col">
      <div className="px-6 pt-4 lg:px-8">
        <Link to="/clinical/ipd" className="text-sm text-muted-foreground hover:text-foreground">
          &larr; Back to IPD
        </Link>
      </div>

      <div className="mt-3 flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <BedSingle className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Beds</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Track bed availability inside wards.</p>
      </div>

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <BedListToolbar
          search={search}
          onSearchChange={handleSearchChange}
          wardId={wardId}
          onWardIdChange={handleWardIdChange}
          status={status}
          onStatusChange={handleStatusChange}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading beds…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load beds.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No beds found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch ? `No results for "${debouncedSearch}".` : 'Create the first bed to get started.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <BedTable
              beds={data.items}
              wardLabels={wardLabels}
              sort={sort}
              onSortChange={handleSortChange}
              onDeleteRequested={setBedPendingDelete}
            />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}

        {bedPendingDelete && (
          <DeleteBedDialog
            bed={bedPendingDelete}
            isDeleting={deleteMutation.isPending}
            onConfirm={handleConfirmDelete}
            onCancel={() => setBedPendingDelete(null)}
          />
        )}
      </div>
    </div>
  );
}
