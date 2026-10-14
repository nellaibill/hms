import { useQuery } from '@tanstack/react-query';
import { ChevronDown, ChevronRight, Loader2, Plus, Search } from 'lucide-react';
import { Fragment, useState } from 'react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { dateLabel, expiryLabel, money, quantity } from '@/features/pharmacy/common/format';
import { RETURN_REASON_LABELS } from '@/features/pharmacy/common/labels';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { pharmacyApi } from '@/services/apiClient';

/** Stock sent back to suppliers, newest first; a row opens to the batches that went. */
export default function PurchaseReturnsListPage() {
  const { hasPermission } = useAuth();
  const canSeeCost = hasPermission('pharmacy.view-cost');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [openId, setOpenId] = useState<string | null>(null);
  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  const query = { page, pageSize: 20, search: debouncedSearch || undefined };
  const { data, isPending, isError } = useQuery({
    queryKey: ['pharmacy', 'purchase-returns', 'list', query],
    queryFn: () => pharmacyApi.getPurchaseReturns(query),
    placeholderData: (previous) => previous,
    enabled: canSeeCost,
  });
  const { data: detail } = useQuery({
    queryKey: ['pharmacy', 'purchase-returns', 'byId', openId],
    queryFn: () => pharmacyApi.getPurchaseReturnById(openId!),
    enabled: Boolean(openId),
  });

  return (
    <PharmacyPage
      title="Purchase returns"
      subtitle="Stock sent back to suppliers."
      subLinks={PURCHASE_LINKS}
      actions={
        hasPermission('pharmacy.create') && (
          <Button size="sm" asChild>
            <Link to="/pharmacy/purchase-returns/new">
              <Plus className="h-4 w-4" />
              New return
            </Link>
          </Button>
        )
      }
    >
      {!canSeeCost ? (
        <p className="rounded-lg border border-border bg-background p-6 text-sm text-muted-foreground">Purchase returns show cost, so the list is limited to users who can view cost. You can still enter a new return.</p>
      ) : (
        <>
          <div className="relative w-full sm:w-80">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              value={search}
              onChange={(event) => { setSearch(event.target.value); setPage(1); }}
              placeholder="Return no., supplier or credit note…"
              aria-label="Search returns"
              className="h-9 w-full rounded-md border border-input bg-background pl-8 pr-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
            />
          </div>

          {isPending && (
            <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading returns…
            </div>
          )}
          {isError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              Purchase returns could not be loaded.
            </p>
          )}

          {data && (
            <>
              <div className="overflow-x-auto rounded-lg border border-border bg-background">
                <table className="w-full text-sm">
                  <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                    <tr>
                      <th className="w-8 px-2 py-2" />
                      <th className="px-3 py-2">Return no.</th>
                      <th className="px-3 py-2">Date</th>
                      <th className="px-3 py-2">Supplier</th>
                      <th className="px-3 py-2">Reason</th>
                      <th className="px-3 py-2">Credit note</th>
                      <th className="px-3 py-2 text-right">Lines</th>
                      <th className="px-3 py-2 text-right">Value</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {data.items.length === 0 && (
                      <tr>
                        <td colSpan={8} className="px-3 py-12 text-center text-muted-foreground">
                          {debouncedSearch ? 'No returns match.' : 'Nothing has been returned to a supplier yet.'}
                        </td>
                      </tr>
                    )}
                    {data.items.map((item) => {
                      const open = openId === item.id;
                      return (
                        <Fragment key={item.id}>
                          <tr onClick={() => setOpenId(open ? null : item.id)} className="cursor-pointer hover:bg-muted/40">
                            <td className="px-2 py-2 text-muted-foreground">{open ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}</td>
                            <td className="whitespace-nowrap px-3 py-2 font-mono text-xs font-medium text-primary">{item.returnNo}</td>
                            <td className="px-3 py-2">{dateLabel(item.returnDate)}</td>
                            <td className="px-3 py-2 font-medium text-foreground">{item.supplierName}</td>
                            <td className="px-3 py-2">
                              <Badge variant="secondary">{RETURN_REASON_LABELS[item.reason]}</Badge>
                            </td>
                            <td className="px-3 py-2 text-muted-foreground">{item.creditNoteNo ?? '—'}</td>
                            <td className="px-3 py-2 text-right tabular-nums">{item.lineCount}</td>
                            <td className="px-3 py-2 text-right font-medium tabular-nums">{money(item.netAmount)}</td>
                          </tr>
                          {open && (
                            <tr>
                              <td colSpan={8} className="bg-muted/20 px-4 py-3">
                                {detail?.id === item.id ? (
                                  <ul className="divide-y divide-border text-sm">
                                    {detail.lines.map((line) => (
                                      <li key={line.id} className="flex flex-wrap justify-between gap-3 py-1">
                                        <span>
                                          {line.productName} <span className="font-mono text-xs text-muted-foreground">{line.batchNo} · {expiryLabel(line.expiryDate)}</span>
                                        </span>
                                        <span className="tabular-nums text-muted-foreground">
                                          {quantity(line.quantity)} × {money(line.unitRate)} + {quantity(line.gstRatePercent)}% = <span className="text-foreground">{money(line.lineTotal)}</span>
                                        </span>
                                      </li>
                                    ))}
                                    {detail.remarks && <li className="py-1 text-muted-foreground">{detail.remarks}</li>}
                                  </ul>
                                ) : (
                                  <span className="text-sm text-muted-foreground">Loading…</span>
                                )}
                              </td>
                            </tr>
                          )}
                        </Fragment>
                      );
                    })}
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
