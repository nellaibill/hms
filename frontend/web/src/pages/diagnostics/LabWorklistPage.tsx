import { ClipboardList, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { Pagination } from '@/features/billing';
import { LabWorklistFilters, LabWorklistTable, useLabOrdersQuery, type LabOrderPriority, type LabOrderStatus } from '@/features/laboratory';

const RESULTS_PAGE_SIZE = 20;

/** The full lab worklist ('/diagnostics/lab/worklist') — searchable, filterable by status/
 * priority/date-range, paginated. Mirrors InvoiceLedgerPage's own layout shape. */
export default function LabWorklistPage() {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<LabOrderStatus | undefined>(undefined);
  const [priority, setPriority] = useState<LabOrderPriority | undefined>(undefined);
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [page, setPage] = useState(1);

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useLabOrdersQuery({
    page,
    pageSize: RESULTS_PAGE_SIZE,
    sort: '-createdAt',
    search: debouncedSearch || undefined,
    status,
    priority,
    dateFrom: dateFrom || undefined,
    dateTo: dateTo || undefined,
  });

  return (
    <div className="flex flex-1 flex-col">
      <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <ClipboardList className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Lab Worklist</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">Every lab order — sample collection through report release.</p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <LabWorklistFilters
          filters={{ search, status, priority, dateFrom, dateTo }}
          onChange={(next) => {
            setSearch(next.search);
            setStatus(next.status);
            setPriority(next.priority);
            setDateFrom(next.dateFrom);
            setDateTo(next.dateTo);
            setPage(1);
          }}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading lab orders…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load lab orders.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No lab orders found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch || status || priority || dateFrom || dateTo ? 'Try a different search or filter.' : 'Lab orders created from Billing will appear here.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <LabWorklistTable orders={data.items} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
