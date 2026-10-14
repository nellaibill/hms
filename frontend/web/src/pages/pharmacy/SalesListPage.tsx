import { useQuery } from '@tanstack/react-query';
import { Loader2, Search } from 'lucide-react';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { dateTimeLabel, money, todayIso } from '@/features/pharmacy/common/format';
import { SALES_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { pharmacyApi } from '@/services/apiClient';

const PAGE_SIZE = 20;
type View = 'bills' | 'due' | 'returns';

const field = 'h-9 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

/**
 * Every pharmacy bill, with the two follow-ups a bill can need: money still owed on it, and
 * goods returned against it. A return always starts from its bill — find the bill here, open
 * it, and return from there.
 */
export default function SalesListPage() {
  const navigate = useNavigate();
  const [view, setView] = useState<View>('bills');
  const [search, setSearch] = useState('');
  const [fromDate, setFromDate] = useState(todayIso());
  const [toDate, setToDate] = useState(todayIso());
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  // A search looks across all dates: the customer at the counter with last week's bill
  // should not need the date range widened first.
  const range = debouncedSearch || view === 'due' ? {} : { fromDate: fromDate || undefined, toDate: toDate || undefined };
  const query = { page, pageSize: PAGE_SIZE, search: debouncedSearch || undefined, ...range };

  const bills = useQuery({
    queryKey: ['pharmacy', 'sales', 'list', view, query],
    queryFn: () => pharmacyApi.getSales({ ...query, hasDue: view === 'due' ? true : undefined }),
    placeholderData: (previous) => previous,
    enabled: view !== 'returns',
  });
  const returns = useQuery({
    queryKey: ['pharmacy', 'sale-returns', 'list', query],
    queryFn: () => pharmacyApi.getSaleReturns(query),
    placeholderData: (previous) => previous,
    enabled: view === 'returns',
  });

  const active = view === 'returns' ? returns : bills;

  function change<T>(setter: (value: T) => void) {
    return (value: T) => {
      setter(value);
      setPage(1);
    };
  }

  return (
    <PharmacyPage
      title="Sales and returns"
      subtitle="Find a bill to reprint it, take a balance, or return goods against it."
      subLinks={SALES_LINKS}
    >
      <div className="flex flex-wrap items-center gap-2">
        <Tabs value={view} onValueChange={(next) => change(setView)(next as View)}>
          <TabsList>
            <TabsTrigger value="bills">Bills</TabsTrigger>
            <TabsTrigger value="due">Balance due</TabsTrigger>
            <TabsTrigger value="returns">Returns</TabsTrigger>
          </TabsList>
        </Tabs>
        <div className="relative w-full sm:w-80">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input
            value={search}
            onChange={(event) => change(setSearch)(event.target.value)}
            placeholder={view === 'returns' ? 'Return no., bill no. or customer…' : 'Bill no., customer, phone or UHID…'}
            aria-label="Search"
            autoFocus
            className={`${field} w-full pl-8`}
          />
        </div>
        {view !== 'due' && (
          <>
            <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
              From
              <input type="date" value={fromDate} disabled={Boolean(debouncedSearch)} onChange={(event) => change(setFromDate)(event.target.value)} className={field} />
            </label>
            <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
              To
              <input type="date" value={toDate} disabled={Boolean(debouncedSearch)} onChange={(event) => change(setToDate)(event.target.value)} className={field} />
            </label>
          </>
        )}
      </div>

      {active.isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading…
        </div>
      )}
      {active.isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {active.error instanceof Error ? active.error.message : 'Failed to load.'}
        </p>
      )}

      {view !== 'returns' && bills.data && (
        <>
          <div className="overflow-x-auto rounded-lg border border-border bg-background">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="px-3 py-2">Bill no.</th>
                  <th className="px-3 py-2">Date</th>
                  <th className="px-3 py-2">Customer</th>
                  <th className="px-3 py-2">Doctor</th>
                  <th className="px-3 py-2 text-right">Items</th>
                  <th className="px-3 py-2 text-right">Amount</th>
                  <th className="px-3 py-2 text-right">Paid</th>
                  <th className="px-3 py-2">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {bills.data.items.length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-3 py-12 text-center text-muted-foreground">
                      {view === 'due' ? 'No bill has a balance due.' : debouncedSearch ? 'No bill matches.' : 'No bills in this period.'}
                    </td>
                  </tr>
                )}
                {bills.data.items.map((sale) => (
                  <tr key={sale.id} onClick={() => navigate(`/pharmacy/sales/${sale.id}`)} className="cursor-pointer hover:bg-muted/40">
                    <td className="px-3 py-2 font-mono text-xs font-medium text-primary">{sale.billNo}</td>
                    <td className="whitespace-nowrap px-3 py-2">{dateTimeLabel(sale.saleDate)}</td>
                    <td className="px-3 py-2">
                      <div className="font-medium text-foreground">{sale.customerName}</div>
                      <div className="text-xs text-muted-foreground">{[sale.patientUhid, sale.customerPhone].filter(Boolean).join(' · ') || 'Walk-in'}</div>
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">{sale.doctorName ?? '—'}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{sale.lineCount}</td>
                    <td className="px-3 py-2 text-right font-medium tabular-nums">{money(sale.netAmount)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(sale.paidAmount - sale.refundedAmount)}</td>
                    <td className="px-3 py-2">
                      <div className="flex flex-wrap gap-1">
                        {sale.chargedToAdmission ? (
                          <Badge variant="secondary">IP credit</Badge>
                        ) : sale.balanceDue > 0 ? (
                          <Badge variant="warning">Due {money(sale.balanceDue)}</Badge>
                        ) : (
                          <Badge variant="success">Paid</Badge>
                        )}
                        {sale.returnedAmount > 0 && <Badge variant="secondary">Returned {money(sale.returnedAmount)}</Badge>}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination meta={bills.data.meta} onPageChange={setPage} />
        </>
      )}

      {view === 'returns' && returns.data && (
        <>
          <div className="overflow-x-auto rounded-lg border border-border bg-background">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="px-3 py-2">Return no.</th>
                  <th className="px-3 py-2">Date</th>
                  <th className="px-3 py-2">Against bill</th>
                  <th className="px-3 py-2">Customer</th>
                  <th className="px-3 py-2">Items</th>
                  <th className="px-3 py-2 text-right">Value</th>
                  <th className="px-3 py-2 text-right">Refunded</th>
                  <th className="px-3 py-2">Reason</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {returns.data.items.length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-3 py-12 text-center text-muted-foreground">
                      No returns in this period.
                    </td>
                  </tr>
                )}
                {returns.data.items.map((saleReturn) => (
                  <tr key={saleReturn.id} onClick={() => navigate(`/pharmacy/sales/${saleReturn.saleId}`)} className="cursor-pointer hover:bg-muted/40">
                    <td className="px-3 py-2 font-mono text-xs font-medium text-primary">{saleReturn.returnNo}</td>
                    <td className="whitespace-nowrap px-3 py-2">{dateTimeLabel(saleReturn.returnDate)}</td>
                    <td className="px-3 py-2 font-mono text-xs">{saleReturn.billNo}</td>
                    <td className="px-3 py-2">{saleReturn.customerName}</td>
                    <td className="px-3 py-2 text-xs text-muted-foreground">{saleReturn.lines.map((line) => `${line.productName} × ${line.quantity}`).join(', ')}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(saleReturn.returnAmount)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(saleReturn.refundAmount)}</td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {saleReturn.reason ?? '—'}
                      {!saleReturn.restocked && <Badge variant="warning" className="ml-1.5">Not restocked</Badge>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination meta={returns.data.meta} onPageChange={setPage} />
        </>
      )}
    </PharmacyPage>
  );
}
