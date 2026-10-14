import { useQuery } from '@tanstack/react-query';
import { Loader2, Plus, Search } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { dateLabel, money } from '@/features/pharmacy/common/format';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { pharmacyApi } from '@/services/apiClient';

const PAGE_SIZE = 20;

/** Purchase invoices (goods receipts), newest first — searchable by our number, the
 * supplier's invoice number, or the supplier's name. */
export default function PurchasesListPage() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search.trim(), 300);
  const canSeeCost = hasPermission('pharmacy.view-cost');

  const query = { page, pageSize: PAGE_SIZE, search: debouncedSearch || undefined, fromDate: fromDate || undefined, toDate: toDate || undefined };
  const { data, isPending, isError, error } = useQuery({
    queryKey: ['pharmacy', 'goods-receipts', 'list', query],
    queryFn: () => pharmacyApi.getGoodsReceipts(query),
    placeholderData: (previous) => previous,
    enabled: canSeeCost,
  });

  const fieldClass = 'h-9 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

  return (
    <PharmacyPage
      title="Purchase invoices"
      subtitle="Every supplier invoice booked into stock."
      subLinks={PURCHASE_LINKS}
      actions={
        hasPermission('pharmacy.create') && (
          <Button size="sm" asChild>
            <Link to="/pharmacy/purchases/new">
              <Plus className="h-4 w-4" />
              New purchase
            </Link>
          </Button>
        )
      }
    >
      {!canSeeCost ? (
        <p className="rounded-lg border border-border bg-background p-6 text-sm text-muted-foreground">
          Purchase invoices show what the pharmacy pays its suppliers, so they are limited to users who can view cost. You can still enter a new purchase.
        </p>
      ) : (
        <>
          <div className="flex flex-wrap items-center gap-2">
            <div className="relative w-full sm:w-80">
              <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <input
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(1);
                }}
                placeholder="GRN no., invoice no. or supplier…"
                aria-label="Search purchases"
                className={`${fieldClass} w-full pl-8`}
              />
            </div>
            <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
              From
              <input type="date" value={fromDate} onChange={(event) => { setFromDate(event.target.value); setPage(1); }} className={fieldClass} />
            </label>
            <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
              To
              <input type="date" value={toDate} onChange={(event) => { setToDate(event.target.value); setPage(1); }} className={fieldClass} />
            </label>
          </div>

          {isPending && (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading purchases…
            </div>
          )}

          {isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error instanceof Error ? error.message : 'Failed to load purchases.'}
            </p>
          )}

          {data && (
            <>
              <div className="overflow-x-auto rounded-lg border border-border bg-background">
                <table className="w-full text-sm">
                  <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                    <tr>
                      <th className="px-3 py-2">GRN no.</th>
                      <th className="px-3 py-2">Received</th>
                      <th className="px-3 py-2">Supplier</th>
                      <th className="px-3 py-2">Invoice no.</th>
                      <th className="px-3 py-2">Store</th>
                      <th className="px-3 py-2 text-right">Lines</th>
                      <th className="px-3 py-2 text-right">Taxable</th>
                      <th className="px-3 py-2 text-right">GST</th>
                      <th className="px-3 py-2 text-right">Total</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {data.items.length === 0 && (
                      <tr>
                        <td colSpan={9} className="px-3 py-12 text-center text-muted-foreground">
                          {debouncedSearch || fromDate || toDate ? 'No purchases match.' : 'No purchases entered yet.'}
                        </td>
                      </tr>
                    )}
                    {data.items.map((receipt) => (
                      <tr key={receipt.id} onClick={() => navigate(`/pharmacy/purchases/${receipt.id}`)} className="cursor-pointer hover:bg-muted/40">
                        <td className="px-3 py-2 font-mono text-xs font-medium text-primary">{receipt.receiptNo}</td>
                        <td className="px-3 py-2">{dateLabel(receipt.receivedDate)}</td>
                        <td className="px-3 py-2 font-medium text-foreground">{receipt.supplierName}</td>
                        <td className="px-3 py-2">{receipt.supplierInvoiceNo}</td>
                        <td className="px-3 py-2 text-muted-foreground">{receipt.storeName}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{receipt.lineCount}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{money(receipt.taxableAmount)}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{money(receipt.taxAmount)}</td>
                        <td className="px-3 py-2 text-right font-medium tabular-nums">{money(receipt.netAmount)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <Pagination meta={data.meta} onPageChange={setPage} />
            </>
          )}
        </>
      )}
    </PharmacyPage>
  );
}
