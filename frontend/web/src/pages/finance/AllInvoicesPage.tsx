import { Loader2, Plus, Wallet } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { PageBanner } from '@/components/PageBanner';
import { useAuth } from '@/features/auth/AuthContext';
import { AccountsNavTabs } from '@/features/reports';
import { useDebouncedValue } from '../../hooks/useDebouncedValue';
import { InvoiceListToolbar, InvoiceLedgerTable, Pagination, useBillingsQuery } from '../../features/billing';
import type { PaymentStatus } from '../../features/billing';

const RESULTS_PAGE_SIZE = 20;

/** The "All Invoices" tab of the Accounts & Finance section — the full searchable/paginated
 * ledger, split out of what used to be InvoiceLedgerPage's second half (see that file's own
 * comment on why the two tabs became separate pages/routes rather than client-only tab state). */
export default function AllInvoicesPage() {
  const { hasPermission } = useAuth();
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
      <PageBanner
        icon={Wallet}
        title="Accounts and Finance"
        subtitle="Unified invoice ledger — every OP, Radiology, Laboratory, and Procedure bill in one place."
      />

      <div className="flex flex-1 flex-col gap-4 p-6 lg:p-8">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <AccountsNavTabs />
          {hasPermission('finance-billing.create') && (
            <Button asChild className="gap-1.5">
              <Link to="/finance/accounts/new">
                <Plus className="h-4 w-4" />
                New Invoice
              </Link>
            </Button>
          )}
        </div>

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
