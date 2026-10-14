import { ApiError, type ExpiringBatchResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, Loader2, Trash2, Undo2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { expiryLabel, money, quantity } from '@/features/pharmacy/common/format';
import { STOCK_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { exportReportToExcel } from '@/features/reports/exportUtils';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

const WINDOWS = [
  { days: 0, label: 'Expired' },
  { days: 30, label: '30 days' },
  { days: 60, label: '60 days' },
  { days: 90, label: '90 days' },
  { days: 180, label: '6 months' },
];

/** Opens purchase-return entry for the supplier the batch was bought from. */
function returnPath(row: ExpiringBatchResponse): string {
  const query = new URLSearchParams({ reason: row.daysLeft < 0 ? 'Expired' : 'NearExpiry', storeId: row.storeId });
  if (row.supplierId) {
    query.set('supplierId', row.supplierId);
    query.set('supplierName', row.supplierName ?? '');
  }
  return `/pharmacy/purchase-returns/new?${query.toString()}`;
}

/**
 * Expiry management. Everything on the shelf that has expired or soon will, soonest first,
 * with what it is worth and who it was bought from — because the two things to do with it
 * are to send it back to that supplier for credit, or write it off. Expired stock can be
 * ticked and written off here in one step.
 */
export default function ExpiryPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canWriteOff = hasPermission('pharmacy.edit');

  const [storeId, setStoreId] = useState('');
  const [days, setDays] = useState<number | undefined>(undefined);
  const [picked, setPicked] = useState<Record<string, boolean>>({});
  const [error, setError] = useState<string | null>(null);

  const { data: settings } = useQuery({ queryKey: ['pharmacy', 'settings'], queryFn: () => pharmacyApi.getSettings(), staleTime: 300_000 });
  const window = days ?? settings?.expiryAlertDays ?? 90;

  const { data, isPending, isError } = useQuery({
    queryKey: ['pharmacy', 'expiring', storeId, window],
    queryFn: () => pharmacyApi.getExpiringBatches(storeId || undefined, window),
  });

  const key = (row: ExpiringBatchResponse) => `${row.storeId}:${row.productBatchId}`;
  const expired = useMemo(() => data?.filter((row) => row.daysLeft < 0) ?? [], [data]);
  const chosen = expired.filter((row) => picked[key(row)]);
  const totalValue = data?.reduce((sum, row) => sum + (row.value ?? 0), 0) ?? 0;
  const expiredValue = expired.reduce((sum, row) => sum + (row.value ?? 0), 0);

  const writeOff = useMutation({
    mutationFn: async () => {
      // One adjustment per store: an adjustment belongs to a single store.
      const byStore = new Map<string, ExpiringBatchResponse[]>();
      chosen.forEach((row) => byStore.set(row.storeId, [...(byStore.get(row.storeId) ?? []), row]));
      const saved = [];
      for (const [store, rows] of byStore) {
        saved.push(
          await pharmacyApi.createStockAdjustment({
            storeId: store,
            reason: 'Expired',
            remarks: 'Expired stock written off',
            lines: rows.map((row) => ({ productId: row.productId, productBatchId: row.productBatchId, quantity: -row.quantityOnHand })),
          }),
        );
      }
      return saved;
    },
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
      toast({ title: `${chosen.length} expired ${chosen.length === 1 ? 'batch' : 'batches'} written off`, description: saved.map((adjustment) => adjustment.adjustmentNo).join(', '), variant: 'success' });
      setPicked({});
      setError(null);
    },
    onError: (failure) => setError(failure instanceof ApiError ? failure.message : 'Could not write the stock off.'),
  });

  function exportList() {
    if (!data) return;
    void exportReportToExcel('pharmacy-expiry.xlsx', [
      {
        heading: 'Expiry',
        headers: ['Product', 'Batch', 'Expiry', 'Days left', 'Quantity', 'Value at cost', 'Store', 'Rack', 'Supplier'],
        rows: data.map((row) => [row.productName, row.batchNo, row.expiryDate, row.daysLeft, row.quantityOnHand, row.value ?? '', row.storeName, row.rackLocation ?? '', row.supplierName ?? '']),
      },
    ]);
  }

  return (
    <PharmacyPage
      title="Expiry"
      subtitle={
        data
          ? `${data.length} ${data.length === 1 ? 'batch' : 'batches'} · ${expired.length} already expired${totalValue ? ` · ₹${money(totalValue)} at cost${expiredValue ? `, of which ₹${money(expiredValue)} expired` : ''}` : ''}`
          : 'Stock that has expired or soon will.'
      }
      subLinks={STOCK_LINKS}
      actions={
        <>
          <Button size="sm" variant="outline" onClick={exportList} disabled={!data || data.length === 0}>
            <Download className="h-4 w-4" />
            Excel
          </Button>
          {canWriteOff && (
            <Button size="sm" variant="destructive" onClick={() => writeOff.mutate()} disabled={chosen.length === 0 || writeOff.isPending}>
              {writeOff.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
              Write off {chosen.length || ''} expired
            </Button>
          )}
        </>
      }
    >
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex overflow-hidden rounded-md border border-border text-xs font-medium">
          {WINDOWS.map((option) => (
            <button key={option.days} type="button" onClick={() => setDays(option.days)} className={cn('px-3 py-1.5', window === option.days ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent')}>
              {option.label}
            </button>
          ))}
        </div>
        <StoreSelect id="expiry-store" value={storeId} onValueChange={setStoreId} allowAll />
      </div>

      {error && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}
      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Checking expiry dates…
        </div>
      )}
      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          The expiry list could not be loaded.
        </p>
      )}

      {data && data.length === 0 && (
        <p className="rounded-lg border border-dashed border-border bg-background p-10 text-center text-sm text-muted-foreground">
          {window === 0 ? 'No expired stock on the shelf.' : `Nothing expires within ${window} days.`}
        </p>
      )}

      {data && data.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-border bg-background">
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
              <tr>
                <th className="w-10 px-3 py-2" />
                <th className="px-3 py-2">Product</th>
                <th className="px-3 py-2">Batch</th>
                <th className="px-3 py-2">Expiry</th>
                <th className="px-3 py-2 text-right">Quantity</th>
                <th className="px-3 py-2 text-right">Value at cost</th>
                <th className="px-3 py-2">Store · rack</th>
                <th className="px-3 py-2">Bought from</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data.map((row) => (
                <tr key={key(row)} className={cn(row.daysLeft < 0 ? 'bg-destructive/5' : row.daysLeft <= 30 ? 'bg-warning/5' : '')}>
                  <td className="px-3 py-2">
                    {row.daysLeft < 0 && canWriteOff && (
                      <input type="checkbox" checked={Boolean(picked[key(row)])} aria-label={`Write off ${row.productName} batch ${row.batchNo}`} onChange={(event) => setPicked((current) => ({ ...current, [key(row)]: event.target.checked }))} />
                    )}
                  </td>
                  <td className="px-3 py-2 font-medium text-foreground">{row.productName}</td>
                  <td className="px-3 py-2 font-mono text-xs">{row.batchNo}</td>
                  <td className="whitespace-nowrap px-3 py-2">
                    {expiryLabel(row.expiryDate)}{' '}
                    {row.daysLeft < 0 ? <Badge variant="destructive">Expired {-row.daysLeft}d ago</Badge> : <Badge variant={row.daysLeft <= 30 ? 'warning' : 'secondary'}>{row.daysLeft}d left</Badge>}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums">{quantity(row.quantityOnHand)}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{row.value != null ? money(row.value) : '—'}</td>
                  <td className="px-3 py-2 text-muted-foreground">{[row.storeName, row.rackLocation].filter(Boolean).join(' · ')}</td>
                  <td className="px-3 py-2">
                    {row.supplierName ? (
                      <Link to={returnPath(row)} className="inline-flex items-center gap-1 text-primary hover:underline" title="Return to this supplier">
                        <Undo2 className="h-3.5 w-3.5" />
                        {row.supplierName}
                      </Link>
                    ) : (
                      <span className="text-muted-foreground">—</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </PharmacyPage>
  );
}
