import type { StockBalanceResponse } from '@hms/shared';
import { Badge } from '@/components/ui/badge';

interface StockBalanceTableProps {
  balances: StockBalanceResponse[];
}

/** A batch within this many days of its expiry date is flagged "Expiring soon" — chosen to
 * give a real pharmacy user enough lead time to prioritize dispensing it or writing it off
 * before it's unusable. Mirrors the backend's PharmacyClock.ExpiringSoonWindowDays, which the
 * product-level stock summary uses for the same judgement. */
const EXPIRING_SOON_WINDOW_DAYS = 30;

function daysUntil(dateString: string): number {
  const msPerDay = 1000 * 60 * 60 * 24;
  return Math.ceil((new Date(dateString).getTime() - Date.now()) / msPerDay);
}

/**
 * Per-batch on-hand quantities. Deliberately carries no low-stock flag: whether a product is
 * low is a question about its total across batches, answered by StockSummaryPanel — a batch
 * here is only ever in stock, empty, expired, or on hold.
 */
export function StockBalanceTable({ balances }: StockBalanceTableProps) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border bg-background">
      <table className="w-full text-sm">
        <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
          <tr>
            <th className="px-4 py-2.5">Product</th>
            <th className="px-4 py-2.5">Batch</th>
            <th className="px-4 py-2.5">Expiry</th>
            <th className="px-4 py-2.5">Quantity on Hand</th>
            <th className="px-4 py-2.5">Status</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {balances.map((balance) => {
            const isZero = balance.quantityOnHand === 0;
            const daysToExpiry = daysUntil(balance.expiryDate);
            const isExpired = daysToExpiry < 0;
            const isExpiringSoon = !isExpired && daysToExpiry <= EXPIRING_SOON_WINDOW_DAYS;
            const isOnHold = !isExpired && !balance.batchIsActive;
            const rowClassName =
              isZero || isExpired ? 'bg-destructive/5 hover:bg-destructive/10' : isExpiringSoon ? 'bg-warning/5 hover:bg-warning/10' : 'hover:bg-muted/30';
            return (
              <tr key={balance.id} className={rowClassName}>
                <td className="px-4 py-3 font-medium text-foreground">{balance.productName}</td>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{balance.batchNo}</td>
                <td className="px-4 py-3 text-sm text-foreground">
                  <div className="flex items-center gap-2">
                    <span>{new Date(balance.expiryDate).toLocaleDateString('en-IN')}</span>
                    {isExpired && <Badge variant="destructive">Expired</Badge>}
                    {isExpiringSoon && <Badge variant="warning">Expiring soon</Badge>}
                  </div>
                </td>
                <td className="px-4 py-3 tabular-nums font-medium text-foreground">{balance.quantityOnHand}</td>
                <td className="px-4 py-3">
                  {isZero && <Badge variant="destructive">Empty</Badge>}
                  {!isZero && isOnHold && <Badge variant="secondary">On hold</Badge>}
                  {!isZero && !isOnHold && !isExpired && <Badge variant="success">In stock</Badge>}
                  {!isZero && isExpired && <Badge variant="destructive">Not dispensable</Badge>}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
