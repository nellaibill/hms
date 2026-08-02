import { Loader2, Wallet } from 'lucide-react';
import { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { InvoiceListToolbar, InvoiceLedgerTable, Pagination, useBillingsQuery } from '../../features/billing';
import type { PaymentStatus } from '../../features/billing';

const RESULTS_PAGE_SIZE = 20;

/** Unified Invoice Ledger — Finance & Billing's landing screen (docs/ScreenInventory.md "Finance & Billing Domain"). Mock data for now: see mockBillingStore.ts. */
export default function InvoiceLedgerPage() {
  const [search, setSearch] = useState('');
  const [paymentStatus, setPaymentStatus] = useState<PaymentStatus | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState('-createdAt');

  const debouncedSearch = useDebouncedValue(search);

  const { data, isPending, isError, error } = useBillingsQuery({
    page,
    pageSize: RESULTS_PAGE_SIZE,
    sort,
    search: debouncedSearch || undefined,
    paymentStatus,
  });

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handlePaymentStatusChange(value: PaymentStatus | undefined) {
    setPaymentStatus(value);
    setPage(1);
  }

  function handleSortChange(value: string) {
    setSort(value);
    setPage(1);
  }

  return (
    <div className="flex flex-1 flex-col">
      {/* Centered, brand-colored banner — matches the Page banner style used
          across module pages (Theme & Branding → Section headers). */}
      <div className="relative flex flex-col items-center gap-1 bg-page-banner px-6 py-5 text-center text-page-banner-foreground">
        <span className="absolute right-6 top-5 hidden rounded-full bg-page-banner-foreground/15 px-2.5 py-1 text-xs font-medium sm:inline-block">
          Demo data — API not connected
        </span>
        <div className="flex items-center gap-3">
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-page-banner-foreground/15 text-page-banner-foreground">
            <Wallet className="h-5 w-5" />
          </span>
          <h1 className="text-xl font-semibold tracking-tight">Accounts and Finance</h1>
        </div>
        <p className="max-w-2xl text-sm text-page-banner-foreground/85">
          Unified invoice ledger — every OP, Radiology, Laboratory, and Procedure bill in one place.
        </p>
      </div>

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <InvoiceListToolbar
          search={search}
          onSearchChange={handleSearchChange}
          paymentStatus={paymentStatus}
          onPaymentStatusChange={handlePaymentStatusChange}
        />

        {isPending && (
          <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading invoices…
          </div>
        )}

        {isError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error instanceof Error ? error.message : 'Failed to load invoices.'}
          </p>
        )}

        {!isPending && !isError && data && data.items.length === 0 && (
          <Card className="border-dashed">
            <CardContent className="flex flex-col items-center gap-2 py-16 text-center">
              <p className="text-sm font-medium text-foreground">No invoices found</p>
              <p className="text-sm text-muted-foreground">
                {debouncedSearch || paymentStatus ? 'Try a different search or filter.' : 'Invoices created at patient registration will appear here.'}
              </p>
            </CardContent>
          </Card>
        )}

        {!isPending && !isError && data && data.items.length > 0 && (
          <div className="flex flex-col gap-3">
            <InvoiceLedgerTable billings={data.items} sort={sort} onSortChange={handleSortChange} />
            <Pagination meta={data.meta} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}
