import { ADJUSTMENT_REASONS, type AdjustmentReason, ApiError, type ReturnableBatchResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ChevronDown, ChevronRight, Loader2, Plus, Save, Trash2, X } from 'lucide-react';
import { Fragment, useState } from 'react';
import { Link } from 'react-router-dom';
import { Pagination } from '@/components/Pagination';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { dateLabel, expiryLabel, money, quantity, toNumber } from '@/features/pharmacy/common/format';
import { ADJUSTMENT_REASON_LABELS } from '@/features/pharmacy/common/labels';
import { StoreBatchPicker } from '@/features/pharmacy/common/StoreBatchPicker';
import { STOCK_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

interface AdjustLine {
  batch: ReturnableBatchResponse;
  /** 'out' takes stock away, 'in' adds it. */
  direction: 'out' | 'in';
  quantity: string;
}

/** Reasons a person may pick; stock-count differences are posted from the count itself. */
const MANUAL_REASONS = ADJUSTMENT_REASONS.filter((reason) => reason !== 'StockCount');
const input = 'h-9 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

/**
 * Stock adjustments: every correction ever made to the books, and the form to make one. An
 * adjustment is the only way a quantity changes without a purchase, sale or transfer behind
 * it, so it asks for a reason and is kept as its own numbered document.
 */
export default function StockAdjustmentsPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canAdjust = hasPermission('pharmacy.edit');

  const [page, setPage] = useState(1);
  const [openId, setOpenId] = useState<string | null>(null);
  const [entering, setEntering] = useState(false);
  const [storeId, setStoreId] = useState('');
  const [reason, setReason] = useState<AdjustmentReason>('Damaged');
  const [remarks, setRemarks] = useState('');
  const [lines, setLines] = useState<AdjustLine[]>([]);
  const [error, setError] = useState<string | null>(null);

  const { data, isPending } = useQuery({
    queryKey: ['pharmacy', 'adjustments', 'list', page],
    queryFn: () => pharmacyApi.getStockAdjustments({ page, pageSize: 20 }),
    placeholderData: (previous) => previous,
  });
  const { data: detail } = useQuery({ queryKey: ['pharmacy', 'adjustments', 'byId', openId], queryFn: () => pharmacyApi.getStockAdjustmentById(openId!), enabled: Boolean(openId) });

  const mutation = useMutation({
    mutationFn: () =>
      pharmacyApi.createStockAdjustment({
        storeId: storeId || undefined,
        reason,
        remarks: remarks.trim() || undefined,
        lines: lines.map((line) => ({ productId: line.batch.productId, productBatchId: line.batch.productBatchId, quantity: toNumber(line.quantity) * (line.direction === 'out' ? -1 : 1) })),
      }),
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
      toast({ title: `Adjustment saved · ${saved.adjustmentNo}`, description: `${ADJUSTMENT_REASON_LABELS[saved.reason]} · ${saved.lineCount} lines`, variant: 'success' });
      setEntering(false);
      setLines([]);
      setRemarks('');
      setError(null);
    },
    onError: (failure) => setError(failure instanceof ApiError ? (failure.validationErrors?.[0]?.message ?? failure.message) : 'Could not save the adjustment.'),
  });

  function save() {
    const over = lines.find((line) => line.direction === 'out' && toNumber(line.quantity) > line.batch.quantityOnHand);
    if (over) {
      setError(`Only ${quantity(over.batch.quantityOnHand)} of ${over.batch.productName} batch ${over.batch.batchNo} is in stock.`);
      return;
    }
    if (lines.length === 0 || lines.some((line) => toNumber(line.quantity) <= 0)) {
      setError('Add at least one batch, with a quantity on every line.');
      return;
    }
    setError(null);
    mutation.mutate();
  }

  const update = (batchId: string, patch: Partial<AdjustLine>) => setLines((current) => current.map((line) => (line.batch.productBatchId === batchId ? { ...line, ...patch } : line)));

  return (
    <PharmacyPage
      title="Stock adjustments"
      subtitle="Corrections to the books: breakage, write-offs, shortages and stock found."
      subLinks={STOCK_LINKS}
      actions={
        canAdjust && (
          <Button size="sm" variant={entering ? 'outline' : 'default'} onClick={() => { setEntering(!entering); setError(null); }}>
            {entering ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {entering ? 'Close' : 'New adjustment'}
          </Button>
        )
      }
    >
      {entering && (
        <section className="flex flex-col gap-3 rounded-lg border border-border bg-background p-3">
          <div className="flex flex-wrap items-end gap-3">
            <label className="flex flex-col gap-1 text-xs text-muted-foreground">
              Reason
              <select value={reason} onChange={(event) => setReason(event.target.value as AdjustmentReason)} className={input}>
                {MANUAL_REASONS.map((value) => (
                  <option key={value} value={value}>
                    {ADJUSTMENT_REASON_LABELS[value]}
                  </option>
                ))}
              </select>
            </label>
            <label className="flex min-w-[240px] flex-1 flex-col gap-1 text-xs text-muted-foreground">
              Remarks
              <input value={remarks} maxLength={500} placeholder="What happened" onChange={(event) => setRemarks(event.target.value)} className={cn(input, 'w-full')} />
            </label>
            <StoreSelect id="adjust-store" value={storeId} onValueChange={(next) => { setStoreId(next); setLines([]); }} />
            <Button onClick={save} disabled={mutation.isPending}>
              {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
              Save adjustment
            </Button>
          </div>

          <StoreBatchPicker
            storeId={storeId}
            pickedBatchIds={lines.map((line) => line.batch.productBatchId)}
            onPick={(batch) => setLines((current) => [...current, { batch, direction: reason === 'Excess' ? 'in' : 'out', quantity: reason === 'Expired' ? String(batch.quantityOnHand) : '' }])}
            placeholder="Find the product to adjust…"
          />

          {lines.length > 0 && (
            <table className="w-full text-sm">
              <thead className="text-left text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="py-1.5 pr-3">Product</th>
                  <th className="py-1.5 pr-3">Batch · expiry</th>
                  <th className="py-1.5 pr-3 text-right">In stock</th>
                  <th className="w-40 py-1.5 pr-3">Change</th>
                  <th className="w-28 py-1.5 pr-3 text-right">Quantity</th>
                  <th className="py-1.5 pr-3 text-right">Will be</th>
                  <th className="w-8" />
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {lines.map((line) => {
                  const change = toNumber(line.quantity) * (line.direction === 'out' ? -1 : 1);
                  return (
                    <tr key={line.batch.productBatchId}>
                      <td className="py-1.5 pr-3 font-medium text-foreground">{line.batch.productName}</td>
                      <td className="py-1.5 pr-3 font-mono text-xs">
                        {line.batch.batchNo} · {expiryLabel(line.batch.expiryDate)}
                      </td>
                      <td className="py-1.5 pr-3 text-right tabular-nums text-muted-foreground">{quantity(line.batch.quantityOnHand)}</td>
                      <td className="py-1.5 pr-3">
                        <select value={line.direction} onChange={(event) => update(line.batch.productBatchId, { direction: event.target.value as 'out' | 'in' })} aria-label="Add or take away" className={cn(input, 'h-8 w-full')}>
                          <option value="out">Take away</option>
                          <option value="in">Add</option>
                        </select>
                      </td>
                      <td className="py-1.5 pr-3">
                        <input value={line.quantity} inputMode="decimal" aria-label={`Units of ${line.batch.productName}`} onChange={(event) => update(line.batch.productBatchId, { quantity: event.target.value })} className={cn(input, 'h-8 w-full text-right tabular-nums')} />
                      </td>
                      <td className={cn('py-1.5 pr-3 text-right tabular-nums', line.batch.quantityOnHand + change < 0 && 'font-medium text-destructive')}>{quantity(line.batch.quantityOnHand + change)}</td>
                      <td>
                        <button type="button" onClick={() => setLines((current) => current.filter((candidate) => candidate.batch.productBatchId !== line.batch.productBatchId))} aria-label={`Remove ${line.batch.productName}`} className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive">
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}

          {error && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error}
            </p>
          )}
        </section>
      )}

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading adjustments…
        </div>
      )}

      {data && (
        <>
          <div className="overflow-x-auto rounded-lg border border-border bg-background">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="w-8 px-2 py-2" />
                  <th className="px-3 py-2">Adjustment no.</th>
                  <th className="px-3 py-2">Date</th>
                  <th className="px-3 py-2">Store</th>
                  <th className="px-3 py-2">Reason</th>
                  <th className="px-3 py-2">Remarks</th>
                  <th className="px-3 py-2 text-right">Lines</th>
                  <th className="px-3 py-2 text-right">Value at cost</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-3 py-12 text-center text-muted-foreground">
                      No adjustments have been made.
                    </td>
                  </tr>
                )}
                {data.items.map((item) => {
                  const open = openId === item.id;
                  return (
                    <Fragment key={item.id}>
                      <tr onClick={() => setOpenId(open ? null : item.id)} className="cursor-pointer hover:bg-muted/40">
                        <td className="px-2 py-2 text-muted-foreground">{open ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}</td>
                        <td className="whitespace-nowrap px-3 py-2 font-mono text-xs font-medium text-primary">{item.adjustmentNo}</td>
                        <td className="px-3 py-2">{dateLabel(item.adjustmentDate)}</td>
                        <td className="px-3 py-2 text-muted-foreground">{item.storeName}</td>
                        <td className="px-3 py-2">
                          <Badge variant="secondary">{ADJUSTMENT_REASON_LABELS[item.reason]}</Badge>
                          {item.stockCountId && (
                            <Link to={`/pharmacy/stock-counts/${item.stockCountId}`} onClick={(event) => event.stopPropagation()} className="ml-2 text-xs text-primary hover:underline">
                              view count
                            </Link>
                          )}
                        </td>
                        <td className="px-3 py-2 text-muted-foreground">{item.remarks ?? '—'}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{item.lineCount}</td>
                        <td className={cn('px-3 py-2 text-right tabular-nums', (item.totalValue ?? 0) < 0 && 'text-destructive')}>{item.totalValue != null ? money(item.totalValue) : '—'}</td>
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
                                    <span className={cn('tabular-nums', line.quantity < 0 ? 'text-destructive' : 'text-success')}>
                                      {line.quantity > 0 ? '+' : ''}
                                      {quantity(line.quantity)}
                                      {line.value != null && <span className="ml-3 text-muted-foreground">{money(line.value)}</span>}
                                    </span>
                                  </li>
                                ))}
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
    </PharmacyPage>
  );
}
