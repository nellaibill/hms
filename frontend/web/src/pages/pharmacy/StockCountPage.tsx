import { ApiError, type StockCountResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Ban, CheckCheck, Loader2, Plus, Printer, Save } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { PrintPageStyle } from '@/features/branding/components/PrintPageStyle';
import { dateTimeLabel, expiryLabel, quantity, toNumber } from '@/features/pharmacy/common/format';
import { STOCK_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

const STATUS: Record<StockCountResponse['status'], { label: string; variant: 'warning' | 'success' | 'secondary' }> = {
  Open: { label: 'Open', variant: 'warning' },
  Posted: { label: 'Posted', variant: 'success' },
  Cancelled: { label: 'Cancelled', variant: 'secondary' },
};

/**
 * Stock count. Without an id this lists the counts and starts a new one; with one it is the
 * count sheet: every batch the books say the store holds, in shelf order, with a box for
 * what was actually found. It can be saved as often as needed and printed to count from.
 * Posting turns the differences into a single stock adjustment.
 */
export default function StockCountPage() {
  const { id } = useParams();
  return id ? <CountSheet id={id} /> : <CountList />;
}

function CountList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const [storeId, setStoreId] = useState('');
  const [error, setError] = useState<string | null>(null);

  const { data, isPending } = useQuery({ queryKey: ['pharmacy', 'stock-counts', 'list'], queryFn: () => pharmacyApi.getStockCounts({ pageSize: 50 }) });
  const startMutation = useMutation({
    mutationFn: () => pharmacyApi.startStockCount(storeId || undefined),
    onSuccess: (count) => navigate(`/pharmacy/stock-counts/${count.id}`),
    onError: (failure) => setError(failure instanceof ApiError ? failure.message : 'Could not start the count.'),
  });

  return (
    <PharmacyPage
      title="Stock count"
      subtitle="Count the shelves and put the books right."
      subLinks={STOCK_LINKS}
      actions={
        hasPermission('pharmacy.edit') && (
          <>
            <StoreSelect id="count-store" value={storeId} onValueChange={setStoreId} label="Store to count" />
            <Button size="sm" onClick={() => startMutation.mutate()} disabled={startMutation.isPending}>
              {startMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
              Start a count
            </Button>
          </>
        )
      }
    >
      {error && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}
      <p className="text-sm text-muted-foreground">
        Count when the counter is quiet. Differences are worked out against the books as they stand when you post, so stock sold before a shelf is counted is fine, but a sale made after a shelf was counted will show as excess.
      </p>

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading…
        </div>
      )}

      {data && (
        <div className="overflow-x-auto rounded-lg border border-border bg-background">
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
              <tr>
                <th className="px-3 py-2">Count no.</th>
                <th className="px-3 py-2">Started</th>
                <th className="px-3 py-2">Store</th>
                <th className="px-3 py-2 text-right">Counted</th>
                <th className="px-3 py-2">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {data.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-3 py-12 text-center text-muted-foreground">
                    No stock count has been done yet.
                  </td>
                </tr>
              )}
              {data.items.map((count) => (
                <tr key={count.id} onClick={() => navigate(`/pharmacy/stock-counts/${count.id}`)} className="cursor-pointer hover:bg-muted/40">
                  <td className="whitespace-nowrap px-3 py-2 font-mono text-xs font-medium text-primary">{count.countNo}</td>
                  <td className="px-3 py-2">{dateTimeLabel(count.startedAt)}</td>
                  <td className="px-3 py-2">{count.storeName}</td>
                  <td className="px-3 py-2 text-right tabular-nums">
                    {count.countedLines} of {count.lineCount}
                  </td>
                  <td className="px-3 py-2">
                    <Badge variant={STATUS[count.status].variant}>{STATUS[count.status].label}</Badge>
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

function CountSheet({ id }: { id: string }) {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const [entries, setEntries] = useState<Record<string, string>>({});
  const [onlyDifferences, setOnlyDifferences] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data: count, isPending } = useQuery({ queryKey: ['pharmacy', 'stock-counts', 'byId', id], queryFn: () => pharmacyApi.getStockCountById(id) });

  useEffect(() => {
    if (count) setEntries(Object.fromEntries(count.lines.map((line) => [line.id, line.countedQuantity != null ? String(line.countedQuantity) : ''])));
  }, [count]);

  const open = count?.status === 'Open';
  const canEdit = open && hasPermission('pharmacy.edit');

  // What has been typed but not yet saved.
  const pending = useMemo(
    () =>
      (count?.lines ?? [])
        .filter((line) => (entries[line.id] ?? '') !== (line.countedQuantity != null ? String(line.countedQuantity) : ''))
        .map((line) => ({ lineId: line.id, countedQuantity: (entries[line.id] ?? '').trim() === '' ? null : toNumber(entries[line.id]) })),
    [count, entries],
  );

  function done(next: StockCountResponse, message: string) {
    queryClient.setQueryData(['pharmacy', 'stock-counts', 'byId', id], next);
    void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
    toast({ title: message, variant: 'success' });
    setError(null);
  }
  const onError = (failure: unknown) => setError(failure instanceof ApiError ? failure.message : 'Could not save.');

  const saveMutation = useMutation({ mutationFn: () => pharmacyApi.saveStockCount(id, pending), onSuccess: (next) => done(next, 'Count saved'), onError });
  const postMutation = useMutation({
    mutationFn: async () => {
      if (pending.length > 0) await pharmacyApi.saveStockCount(id, pending);
      return pharmacyApi.postStockCount(id);
    },
    onSuccess: (next) => done(next, next.adjustmentId ? 'Count posted — differences adjusted' : 'Count posted — the shelves matched the books'),
    onError,
  });
  const cancelMutation = useMutation({ mutationFn: () => pharmacyApi.cancelStockCount(id), onSuccess: (next) => done(next, 'Count cancelled'), onError });
  const busy = saveMutation.isPending || postMutation.isPending || cancelMutation.isPending;

  const rows = (count?.lines ?? []).filter((line) => {
    if (!onlyDifferences) return true;
    const typed = (entries[line.id] ?? '').trim();
    return typed !== '' && toNumber(typed) !== line.currentQuantity;
  });
  const counted = Object.values(entries).filter((value) => value.trim() !== '').length;

  return (
    <PharmacyPage
      title={count ? `Stock count ${count.countNo}` : 'Stock count'}
      subtitle={count ? `${count.storeName} · started ${dateTimeLabel(count.startedAt)} · ${counted} of ${count.lineCount} counted` : undefined}
      subLinks={STOCK_LINKS}
      actions={
        <>
          {count && <Badge variant={STATUS[count.status].variant}>{STATUS[count.status].label}</Badge>}
          {count?.adjustmentId && (
            <Link to="/pharmacy/adjustments" className="text-xs font-medium text-primary hover:underline">
              See the adjustment
            </Link>
          )}
          <Button size="sm" variant="outline" onClick={() => window.print()} disabled={!count}>
            <Printer className="h-4 w-4" />
            Print count sheet
          </Button>
          {canEdit && (
            <>
              <Button size="sm" variant="outline" onClick={() => cancelMutation.mutate()} disabled={busy}>
                <Ban className="h-4 w-4" />
                Cancel count
              </Button>
              <Button size="sm" variant="outline" onClick={() => saveMutation.mutate()} disabled={busy || pending.length === 0}>
                {saveMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
                Save{pending.length > 0 ? ` (${pending.length})` : ''}
              </Button>
              <Button size="sm" onClick={() => postMutation.mutate()} disabled={busy || counted === 0}>
                {postMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <CheckCheck className="h-4 w-4" />}
                Post count
              </Button>
            </>
          )}
        </>
      }
    >
      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading the count sheet…
        </div>
      )}
      {error && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}

      {count && (
        <>
          <label className="flex items-center gap-2 text-sm text-muted-foreground print:hidden">
            <input type="checkbox" checked={onlyDifferences} onChange={(event) => setOnlyDifferences(event.target.checked)} />
            Show only lines that differ from the books
          </label>

          <div className="print-target overflow-x-auto rounded-lg border border-border bg-background">
            <PrintPageStyle document="report" />
            <p className="hidden px-3 py-2 text-sm font-semibold print:block">
              Stock count {count.countNo} · {count.storeName} · {dateTimeLabel(count.startedAt)}
            </p>
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="px-3 py-2">Rack</th>
                  <th className="px-3 py-2">Product</th>
                  <th className="px-3 py-2">Batch · expiry</th>
                  <th className="px-3 py-2 text-right">Book</th>
                  <th className="w-32 px-3 py-2 text-right">Counted</th>
                  <th className="px-3 py-2 text-right print:hidden">Difference</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {rows.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-3 py-10 text-center text-muted-foreground">
                      {onlyDifferences ? 'Nothing counted so far differs from the books.' : 'Nothing on this count.'}
                    </td>
                  </tr>
                )}
                {rows.map((line) => {
                  const typed = (entries[line.id] ?? '').trim();
                  const book = open ? line.currentQuantity : line.bookQuantity;
                  const difference = typed === '' ? null : toNumber(typed) - (open ? line.currentQuantity : (line.countedQuantity ?? 0) - (line.variance ?? 0));
                  const moved = open && line.currentQuantity !== line.bookQuantity;
                  return (
                    <tr key={line.id}>
                      <td className="px-3 py-1.5 text-muted-foreground">{line.rackLocation ?? '—'}</td>
                      <td className="px-3 py-1.5 font-medium text-foreground">{line.productName}</td>
                      <td className="px-3 py-1.5 font-mono text-xs">
                        {line.batchNo} · {expiryLabel(line.expiryDate)}
                      </td>
                      <td className="px-3 py-1.5 text-right tabular-nums">
                        {quantity(book)}
                        {moved && <div className="text-[11px] text-warning print:hidden">was {quantity(line.bookQuantity)} at start</div>}
                      </td>
                      <td className="px-3 py-1">
                        {canEdit ? (
                          <input
                            value={entries[line.id] ?? ''}
                            inputMode="decimal"
                            aria-label={`Counted quantity of ${line.productName} batch ${line.batchNo}`}
                            onChange={(event) => setEntries((current) => ({ ...current, [line.id]: event.target.value }))}
                            className="h-8 w-full rounded border border-input bg-background px-2 text-right text-sm tabular-nums outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 print:border-0 print:border-b"
                          />
                        ) : (
                          <span className="block text-right tabular-nums">{line.countedQuantity != null ? quantity(line.countedQuantity) : '—'}</span>
                        )}
                      </td>
                      <td className={cn('px-3 py-1.5 text-right tabular-nums print:hidden', difference == null || difference === 0 ? 'text-muted-foreground' : difference < 0 ? 'font-medium text-destructive' : 'font-medium text-success')}>
                        {difference == null ? '' : difference === 0 ? 'matches' : `${difference > 0 ? '+' : ''}${quantity(difference)}`}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </>
      )}
    </PharmacyPage>
  );
}
