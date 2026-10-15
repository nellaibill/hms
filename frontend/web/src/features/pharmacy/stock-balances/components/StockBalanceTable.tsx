import type { StockBalanceResponse } from '@hms/shared';
import { useQuery } from '@tanstack/react-query';
import { Badge } from '@/components/ui/badge';
import { pharmacyApi } from '@/services/apiClient';

interface StockBalanceTableProps {
  balances: StockBalanceResponse[];
}

/** Until the settings load: the same default the settings screen shows before they are
 * first saved (PharmacySettingsDefaults.ExpiryAlertDays on the server). */
const DEFAULT_EXPIRY_ALERT_DAYS = 90;

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
  // "Expiring soon" uses the pharmacy's own alert window, as the product summary above does.
  const { data: settings } = useQuery({ queryKey: ['pharmacy', 'settings'], queryFn: () => pharmacyApi.getSettings(), staleTime: 300_000 });
  const expiringSoonDays = settings?.expiryAlertDays ?? DEFAULT_EXPIRY_ALERT_DAYS;

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
            const isExpiringSoon = !isExpired && daysToExpiry <= expiringSoonDays;
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
