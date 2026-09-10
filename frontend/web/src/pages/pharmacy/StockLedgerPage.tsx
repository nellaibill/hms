import { Loader2, Receipt } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { Pagination } from '@/components/Pagination';
import { StockLedgerTable, StockLedgerToolbar, useStockLedgerQuery, type StockLedgerFilters } from '@/features/pharmacy/stock-ledger';

const emptyFilters: StockLedgerFilters = { productId: '', transactionType: undefined, fromDate: '', toDate: '' };

export default function StockLedgerPage() {
  const [filters, setFilters] = useState<StockLedgerFilters>(emptyFilters);
  const [page, setPage] = useState(1);

  const { data, isPending, isError, error } = useStockLedgerQuery({
    page,
    pageSize: 20,
    productId: filters.productId || undefined,
    transactionType: filters.transactionType,
    // Date inputs yield "YYYY-MM-DD" — the backend's FromDate/ToDate are DateTime, so
    // widen to full-day bounds the same way a UTC-instant conversion would for a
    // datetime-local field elsewhere in this app (see AdmissionCreatePage).
    fromDate: filters.fromDate ? new Date(`${filters.fromDate}T00:00:00`).toISOString() : undefined,
    toDate: filters.toDate ? new Date(`${filters.toDate}T23:59:59.999`).toISOString() : undefined,
  });

  function handleFiltersChange(next: StockLedgerFilters) {
    setFilters(next);
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      <PageBanner
        icon={Receipt}
        title="Stock Ledger"
        subtitle="Combined, filterable receipt + dispense history."
        backTo="/pharmacy"
        backLabel="Back to Pharmacy"
      />

      <div className="flex flex-1 flex-col gap-6 p-6 lg:p-8">
        <StockLedgerToolbar filters={filters} onChange={handleFiltersChange} />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading stock ledger…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load stock ledger.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No ledger entries found</p>
              <p className="text-sm text-muted-foreground">Try a different filter, or nothing has happened yet.</p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <StockLedgerTable transactions={data.items} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
