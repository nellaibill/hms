import { ApiError, PURCHASE_RETURN_REASONS, type Product, type PurchaseReturnReason, type ReturnableBatchResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, Save, Trash2 } from 'lucide-react';
import { useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { daysUntil, expiryLabel, money, quantity, todayIso, toNumber } from '@/features/pharmacy/common/format';
import { taxExclusive } from '@/features/pharmacy/common/gst';
import { RETURN_REASON_LABELS } from '@/features/pharmacy/common/labels';
import { LookupInput, type LookupInputHandle } from '@/features/pharmacy/common/LookupInput';
import { SupplierLookup, type SupplierOption } from '@/features/pharmacy/common/SupplierLookup';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { cn } from '@/lib/utils';
import { pharmacyApi, productsApi } from '@/services/apiClient';

interface ReturnLine {
  batch: ReturnableBatchResponse;
  quantity: string;
  rate: string;
}

const input = 'h-9 w-full rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';
const caption = 'text-[11px] font-medium uppercase tracking-wide text-muted-foreground';

async function searchProducts(term: string): Promise<Product[]> {
  return (await productsApi.getProducts({ search: term, pageSize: 20, sort: 'productName' })).items;
}

/**
 * Sends stock back to a supplier. Pick a product and the screen lists the batches of it on
 * the shelf — expired ones first, since those are what most returns are — to add to the
 * return. Saving takes the stock out and credits the supplier's account.
 */
export default function PurchaseReturnEntryPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const [params] = useSearchParams();

  const [supplier, setSupplier] = useState<SupplierOption | null>(null);
  const [storeId, setStoreId] = useState('');
  const [reason, setReason] = useState<PurchaseReturnReason>((params.get('reason') as PurchaseReturnReason) || 'Expired');
  const [creditNoteNo, setCreditNoteNo] = useState('');
  const [remarks, setRemarks] = useState('');
  const [product, setProduct] = useState<Product | null>(null);
  const [lines, setLines] = useState<ReturnLine[]>([]);
  const [error, setError] = useState<string | null>(null);
  const productRef = useRef<LookupInputHandle>(null);

  const { data: batches, isFetching } = useQuery({
    queryKey: ['pharmacy', 'returnable-batches', product?.id, storeId],
    queryFn: () => pharmacyApi.getReturnableBatches(product!.id, storeId || undefined),
    enabled: Boolean(product),
  });

  const saveMutation = useMutation({
    mutationFn: () =>
      pharmacyApi.createPurchaseReturn({
        storeId: storeId || undefined,
        supplierId: supplier!.id,
        reason,
        creditNoteNo: creditNoteNo.trim() || undefined,
        remarks: remarks.trim() || undefined,
        lines: lines.map((line) => ({
          productId: line.batch.productId,
          productBatchId: line.batch.productBatchId,
          quantity: toNumber(line.quantity),
          unitRate: line.rate.trim() === '' ? undefined : toNumber(line.rate),
        })),
      }),
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
      toast({ title: `Return saved · ${saved.returnNo}`, description: `${saved.supplierName} · ₹${money(saved.netAmount)}`, variant: 'success' });
      navigate('/pharmacy/purchase-returns');
    },
    onError: (failure) => setError(failure instanceof ApiError ? failure.message : 'Could not save the return.'),
  });

  function addBatch(batch: ReturnableBatchResponse) {
    if (lines.some((line) => line.batch.productBatchId === batch.productBatchId)) return;
    // The whole of an expired batch goes back; for anything else the quantity is left to enter.
    const expired = daysUntil(batch.expiryDate) < 0;
    setLines((current) => [...current, { batch, quantity: expired ? String(batch.quantityOnHand) : '', rate: batch.unitRate != null ? String(batch.unitRate) : '' }]);
    setError(null);
  }

  function save() {
    if (!supplier) {
      setError('Choose the supplier the stock is going back to.');
      return;
    }
    const over = lines.find((line) => toNumber(line.quantity) > line.batch.quantityOnHand);
    if (over) {
      setError(`Only ${quantity(over.batch.quantityOnHand)} of ${over.batch.productName} batch ${over.batch.batchNo} is in stock.`);
      return;
    }
    if (lines.length === 0 || lines.some((line) => toNumber(line.quantity) <= 0)) {
      setError('Enter a quantity on every line, or remove the line.');
      return;
    }
    setError(null);
    saveMutation.mutate();
  }

  const totals = lines.reduce(
    (sum, line) => {
      const amount = taxExclusive(toNumber(line.quantity) * toNumber(line.rate), 0, line.batch.gstRatePercent);
      return { taxable: sum.taxable + amount.taxable, tax: sum.tax + amount.tax };
    },
    { taxable: 0, tax: 0 },
  );

  return (
    <PharmacyPage
      title="Return to supplier"
      subtitle="Send expired, damaged or excess stock back and credit the supplier's account."
      subLinks={PURCHASE_LINKS}
      actions={
        <Button size="sm" onClick={save} disabled={saveMutation.isPending}>
          {saveMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
          Save return
        </Button>
      }
    >
      <section className="grid gap-3 rounded-lg border border-border bg-background p-3 md:grid-cols-2 xl:grid-cols-[minmax(0,2fr)_180px_180px_minmax(0,2fr)]">
        <label className="flex flex-col gap-1">
          <span className={caption}>Supplier</span>
          <SupplierLookup value={supplier} onChange={(next) => { setSupplier(next); productRef.current?.focus(); }} autoFocus />
        </label>
        <label className="flex flex-col gap-1">
          <span className={caption}>Reason</span>
          <select value={reason} onChange={(event) => setReason(event.target.value as PurchaseReturnReason)} className={cn(input, 'h-10')}>
            {PURCHASE_RETURN_REASONS.map((value) => (
              <option key={value} value={value}>
                {RETURN_REASON_LABELS[value]}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1">
          <span className={caption}>Credit note no.</span>
          <input value={creditNoteNo} maxLength={50} placeholder="If issued" onChange={(event) => setCreditNoteNo(event.target.value)} className={cn(input, 'h-10')} />
        </label>
        <label className="flex flex-col gap-1">
          <span className={caption}>Remarks</span>
          <input value={remarks} maxLength={500} onChange={(event) => setRemarks(event.target.value)} className={cn(input, 'h-10')} />
        </label>
        <div className="empty:hidden md:col-span-2">
          <StoreSelect id="return-store" value={storeId} onValueChange={(next) => { setStoreId(next); setLines([]); }} label="Returning from" />
        </div>
      </section>

      <section className="rounded-lg border border-primary/30 bg-primary/5 p-3">
        <LookupInput<Product>
          ref={productRef}
          queryKey={['pharmacy', 'purchase-return', 'products']}
          search={searchProducts}
          getKey={(candidate) => candidate.id}
          onSelect={setProduct}
          ariaLabel="Product"
          placeholder="Find the product to return…"
          displayValue={product?.productName}
          emptyText="No product found."
          renderItem={(candidate) => (
            <span>
              <span className="font-medium">{candidate.productName}</span>
              {candidate.genericName && <span className="ml-2 text-xs text-muted-foreground">{candidate.genericName}</span>}
            </span>
          )}
        />
        {product && (
          <div className="mt-2 flex flex-wrap gap-2">
            {isFetching && <span className="text-sm text-muted-foreground">Loading batches…</span>}
            {batches?.length === 0 && <span className="text-sm text-muted-foreground">No stock of {product.productName} in this store.</span>}
            {batches?.map((batch) => {
              const days = daysUntil(batch.expiryDate);
              const added = lines.some((line) => line.batch.productBatchId === batch.productBatchId);
              return (
                <button
                  key={batch.productBatchId}
                  type="button"
                  disabled={added}
                  onClick={() => addBatch(batch)}
                  className="flex items-center gap-2 rounded-md border border-border bg-background px-2.5 py-1.5 text-sm hover:border-primary/50 disabled:opacity-50"
                >
                  <span className="font-mono text-xs">{batch.batchNo}</span>
                  <span>exp {expiryLabel(batch.expiryDate)}</span>
                  <span className="tabular-nums text-muted-foreground">{quantity(batch.quantityOnHand)} in stock</span>
                  {days < 0 ? <Badge variant="destructive">Expired</Badge> : days <= 90 ? <Badge variant="warning">{days}d left</Badge> : null}
                </button>
              );
            })}
          </div>
        )}
      </section>

      <section className="overflow-hidden rounded-lg border border-border bg-background">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
              <tr>
                <th className="w-10 px-3 py-2">#</th>
                <th className="px-3 py-2">Product</th>
                <th className="px-3 py-2">Batch · expiry</th>
                <th className="px-3 py-2 text-right">In stock</th>
                <th className="w-28 px-3 py-2 text-right">Return (units)</th>
                <th className="w-28 px-3 py-2 text-right">Rate / unit</th>
                <th className="px-3 py-2 text-right">GST %</th>
                <th className="px-3 py-2 text-right">Amount</th>
                <th className="w-10 px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {lines.length === 0 && (
                <tr>
                  <td colSpan={9} className="px-3 py-12 text-center text-muted-foreground">
                    Find a product above and click the batches going back.
                  </td>
                </tr>
              )}
              {lines.map((line, index) => {
                const amount = taxExclusive(toNumber(line.quantity) * toNumber(line.rate), 0, line.batch.gstRatePercent);
                const update = (patch: Partial<ReturnLine>) => setLines((current) => current.map((candidate) => (candidate.batch.productBatchId === line.batch.productBatchId ? { ...candidate, ...patch } : candidate)));
                return (
                  <tr key={line.batch.productBatchId}>
                    <td className="px-3 py-2 text-muted-foreground">{index + 1}</td>
                    <td className="px-3 py-2 font-medium text-foreground">{line.batch.productName}</td>
                    <td className="px-3 py-2 font-mono text-xs">
                      {line.batch.batchNo} · {expiryLabel(line.batch.expiryDate)}
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{quantity(line.batch.quantityOnHand)}</td>
                    <td className="px-3 py-1.5">
                      <input value={line.quantity} inputMode="decimal" aria-label={`Units of ${line.batch.productName} to return`} onChange={(event) => update({ quantity: event.target.value })} className={cn(input, 'h-8 text-right tabular-nums', toNumber(line.quantity) > line.batch.quantityOnHand && 'border-destructive')} />
                    </td>
                    <td className="px-3 py-1.5">
                      <input value={line.rate} inputMode="decimal" aria-label={`Rate per unit of ${line.batch.productName}`} onChange={(event) => update({ rate: event.target.value })} className={cn(input, 'h-8 text-right tabular-nums')} />
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{quantity(line.batch.gstRatePercent)}</td>
                    <td className="px-3 py-2 text-right font-medium tabular-nums">{money(amount.total)}</td>
                    <td className="px-2 py-2">
                      <button type="button" onClick={() => setLines((current) => current.filter((candidate) => candidate.batch.productBatchId !== line.batch.productBatchId))} aria-label={`Remove ${line.batch.productName}`} className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive">
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        {lines.length > 0 && (
          <p className="flex flex-wrap justify-end gap-x-6 gap-y-1 border-t border-border px-3 py-2 text-sm">
            <span className="text-muted-foreground">Taxable {money(totals.taxable)}</span>
            <span className="text-muted-foreground">GST {money(totals.tax)}</span>
            <span className="font-semibold tabular-nums">Return value ₹{money(Math.round(totals.taxable + totals.tax))}</span>
          </p>
        )}
      </section>

      {error && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}
      <p className="text-xs text-muted-foreground">Dated {todayIso().split('-').reverse().join('-')}. A saved return cannot be edited; a mistake is corrected with a stock adjustment.</p>
    </PharmacyPage>
  );
}
