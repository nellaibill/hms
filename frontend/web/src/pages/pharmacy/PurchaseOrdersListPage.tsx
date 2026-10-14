import { PURCHASE_ORDER_STATUSES, type PurchaseOrderStatus } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { Loader2, Plus, Search } from 'lucide-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/AuthContext';
import { dateLabel, money } from '@/features/pharmacy/common/format';
import { ORDER_STATUS } from '@/features/pharmacy/common/labels';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { pharmacyApi } from '@/services/apiClient';

const field = 'h-9 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

/** Purchase orders, newest first, with how far each has got. */
export default function PurchaseOrdersListPage() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<PurchaseOrderStatus | ''>('');
  const [page, setPage] = useState(1);
  const debouncedSearch = useDebouncedValue(search.trim(), 300);

  const query = { page, pageSize: 20, search: debouncedSearch || undefined, status: status || undefined };
  const { data, isPending, isError } = useQuery({
    queryKey: ['pharmacy', 'purchase-orders', 'list', query],
    queryFn: () => pharmacyApi.getPurchaseOrders(query),
    placeholderData: (previous) => previous,
  });

  return (
    <PharmacyPage
      title="Purchase orders"
      subtitle="Orders placed with suppliers, and what is still to come on each."
      subLinks={PURCHASE_LINKS}
      actions={
        hasPermission('pharmacy.create') && (
          <Button size="sm" asChild>
            <Link to="/pharmacy/purchase-orders/new">
              <Plus className="h-4 w-4" />
              New order
            </Link>
          </Button>
        )
      }
    >
      <div className="flex flex-wrap items-center gap-2">
        <div className="relative w-full sm:w-80">
          <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} placeholder="Order no. or supplier…" aria-label="Search orders" className={`${field} w-full pl-8`} />
        </div>
        <select value={status} onChange={(event) => { setStatus(event.target.value as PurchaseOrderStatus | ''); setPage(1); }} aria-label="Status" className={field}>
          <option value="">All statuses</option>
          {PURCHASE_ORDER_STATUSES.map((value) => (
            <option key={value} value={value}>
              {ORDER_STATUS[value].label}
            </option>
          ))}
        </select>
      </div>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading orders…
        </div>
      )}
      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Purchase orders could not be loaded.
        </p>
      )}

      {data && (
        <>
          <div className="overflow-x-auto rounded-lg border border-border bg-background">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="px-3 py-2">Order no.</th>
                  <th className="px-3 py-2">Date</th>
                  <th className="px-3 py-2">Supplier</th>
                  <th className="px-3 py-2">Expected</th>
                  <th className="px-3 py-2 text-right">Lines</th>
                  <th className="px-3 py-2 text-right">Est. amount</th>
                  <th className="px-3 py-2">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-3 py-12 text-center text-muted-foreground">
                      {debouncedSearch || status ? 'No orders match.' : 'No purchase orders yet. Start from the reorder list, or create one by hand.'}
                    </td>
                  </tr>
                )}
                {data.items.map((order) => (
                  <tr key={order.id} onClick={() => navigate(`/pharmacy/purchase-orders/${order.id}`)} className="cursor-pointer hover:bg-muted/40">
                    <td className="whitespace-nowrap px-3 py-2 font-mono text-xs font-medium text-primary">{order.orderNo}</td>
                    <td className="px-3 py-2">{dateLabel(order.orderDate)}</td>
                    <td className="px-3 py-2 font-medium text-foreground">{order.supplierName}</td>
                    <td className="px-3 py-2 text-muted-foreground">{dateLabel(order.expectedDate)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{order.lineCount}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(order.estimatedAmount)}</td>
                    <td className="px-3 py-2">
                      <Badge variant={ORDER_STATUS[order.status].variant}>{ORDER_STATUS[order.status].label}</Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination meta={data.meta} onPageChange={setPage} />
        </>
      )}
    </PharmacyPage>
  );
}
