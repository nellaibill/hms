import { ApiError, type Product, type PurchaseOrderResponse, type SavePurchaseOrderRequest } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Ban, Loader2, PackagePlus, Printer, Save, Send, Trash2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { PrintPageStyle } from '@/features/branding/components/PrintPageStyle';
import { dateLabel, money, quantity, toNumber } from '@/features/pharmacy/common/format';
import { ORDER_STATUS } from '@/features/pharmacy/common/labels';
import { LookupInput, type LookupInputHandle } from '@/features/pharmacy/common/LookupInput';
import { SupplierLookup, type SupplierOption } from '@/features/pharmacy/common/SupplierLookup';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { cn } from '@/lib/utils';
import { pharmacyApi, productsApi } from '@/services/apiClient';

interface DraftLine {
  productId: string;
  productName: string;
  unitsPerPack?: number;
  quantity: string;
  rate: string;
}

const input = 'h-9 w-full rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';
const caption = 'text-[11px] font-medium uppercase tracking-wide text-muted-foreground';

async function searchProducts(term: string): Promise<Product[]> {
  return (await productsApi.getProducts({ search: term, pageSize: 20, isActive: true, sort: 'productName' })).items;
}

/**
 * A purchase order: written as a draft, sent to the supplier, then received against as their
 * invoices arrive. While it is a draft everything can be changed; once sent it is the record
 * of what was asked for, and this screen shows how much of it has come.
 */
export default function PurchaseOrderPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canSeeCost = hasPermission('pharmacy.view-cost');

  const [supplier, setSupplier] = useState<SupplierOption | null>(null);
  const [expectedDate, setExpectedDate] = useState('');
  const [remarks, setRemarks] = useState('');
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [error, setError] = useState<string | null>(null);
  const productRef = useRef<LookupInputHandle>(null);
  const quantityRefs = useRef(new Map<string, HTMLInputElement>());

  const { data: order, isPending } = useQuery({
    queryKey: ['pharmacy', 'purchase-orders', 'byId', id],
    queryFn: () => pharmacyApi.getPurchaseOrderById(id!),
    enabled: Boolean(id),
  });

  const isDraft = !id || order?.status === 'Draft';
  const isOpen = order?.status === 'Ordered' || order?.status === 'PartlyReceived';

  useEffect(() => {
    if (!order) return;
    setSupplier({ id: order.supplierId, name: order.supplierName, code: '' });
    setExpectedDate(order.expectedDate ?? '');
    setRemarks(order.remarks ?? '');
    setLines(order.lines.map((line) => ({ productId: line.productId, productName: line.productName, quantity: String(line.quantity), rate: String(line.expectedRate) })));
  }, [order]);

  function saved(result: PurchaseOrderResponse, message: string) {
    void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
    toast({ title: message, description: `${result.orderNo} · ${result.supplierName}`, variant: 'success' });
    setError(null);
    if (!id) navigate(`/pharmacy/purchase-orders/${result.id}`, { replace: true });
  }

  const onError = (failure: unknown) => setError(failure instanceof ApiError ? (failure.validationErrors?.[0]?.message ?? failure.message) : 'Could not save the order.');

  const saveMutation = useMutation({
    mutationFn: (request: SavePurchaseOrderRequest) => (id ? pharmacyApi.updatePurchaseOrder(id, request) : pharmacyApi.createPurchaseOrder(request)),
    onSuccess: (result, request) => saved(result, request.placeOrder ? 'Order placed' : 'Draft saved'),
    onError,
  });
  const placeMutation = useMutation({ mutationFn: () => pharmacyApi.placePurchaseOrder(id!), onSuccess: (result) => saved(result, 'Order placed'), onError });
  const cancelMutation = useMutation({ mutationFn: () => pharmacyApi.cancelPurchaseOrder(id!), onSuccess: (result) => saved(result, 'Order cancelled'), onError });
  const busy = saveMutation.isPending || placeMutation.isPending || cancelMutation.isPending;

  function addProduct(product: Product) {
    setError(null);
    if (!lines.some((line) => line.productId === product.id)) {
      setLines((current) => [...current, { productId: product.id, productName: product.productName, unitsPerPack: product.unitsPerPack, quantity: '1', rate: product.costPrice > 0 ? String(product.costPrice) : '' }]);
    }
    setTimeout(() => {
      const box = quantityRefs.current.get(product.id);
      box?.focus();
      box?.select();
    }, 0);
  }

  function save(placeOrder: boolean) {
    if (!supplier) {
      setError('Choose the supplier.');
      return;
    }
    const valid = lines.filter((line) => toNumber(line.quantity) > 0);
    if (valid.length === 0) {
      setError('Add at least one product with a quantity.');
      return;
    }
    saveMutation.mutate({
      supplierId: supplier.id,
      expectedDate: expectedDate || null,
      remarks: remarks.trim() || undefined,
      placeOrder,
      lines: valid.map((line) => ({ productId: line.productId, quantity: toNumber(line.quantity), expectedRate: line.rate.trim() === '' ? undefined : toNumber(line.rate) })),
    });
  }

  const estimated = lines.reduce((sum, line) => sum + toNumber(line.quantity) * toNumber(line.rate), 0);

  return (
    <PharmacyPage
      title={order ? `Purchase order ${order.orderNo}` : 'New purchase order'}
      subtitle={order ? `${order.supplierName} · ${dateLabel(order.orderDate)}` : 'Draft an order, then send it to the supplier.'}
      subLinks={PURCHASE_LINKS}
      actions={
        <>
          {order && <Badge variant={ORDER_STATUS[order.status].variant}>{ORDER_STATUS[order.status].label}</Badge>}
          {order && (
            <Button size="sm" variant="outline" onClick={() => window.print()}>
              <Printer className="h-4 w-4" />
              Print
            </Button>
          )}
          {isDraft && hasPermission('pharmacy.create') && (
            <>
              <Button size="sm" variant="outline" onClick={() => save(false)} disabled={busy}>
                <Save className="h-4 w-4" />
                Save draft
              </Button>
              <Button size="sm" onClick={() => save(true)} disabled={busy}>
                {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                Place order
              </Button>
            </>
          )}
          {isOpen && hasPermission('pharmacy.create') && (
            <Button size="sm" asChild>
              <Link to={`/pharmacy/purchases/new?po=${order!.id}`}>
                <PackagePlus className="h-4 w-4" />
                Receive goods
              </Link>
            </Button>
          )}
          {order && (order.status === 'Draft' || isOpen) && hasPermission('pharmacy.edit') && (
            <Button size="sm" variant="outline" onClick={() => cancelMutation.mutate()} disabled={busy}>
              <Ban className="h-4 w-4" />
              Cancel order
            </Button>
          )}
        </>
      }
    >
      {id && isPending ? (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading…
        </div>
      ) : (
        <div className="print-target flex flex-col gap-4">
          <PrintPageStyle document="report" />
          <section className="grid gap-3 rounded-lg border border-border bg-background p-3 md:grid-cols-[minmax(0,2fr)_180px_minmax(0,2fr)]">
            <label className="flex flex-col gap-1">
              <span className={caption}>Supplier</span>
              {isDraft ? <SupplierLookup value={supplier} onChange={(next) => { setSupplier(next); productRef.current?.focus(); }} autoFocus={!id} /> : <span className="py-2 text-sm font-medium text-foreground">{order?.supplierName}</span>}
            </label>
            <label className="flex flex-col gap-1">
              <span className={caption}>Expected by</span>
              {isDraft ? <input type="date" value={expectedDate} onChange={(event) => setExpectedDate(event.target.value)} className={cn(input, 'h-10')} /> : <span className="py-2 text-sm">{dateLabel(order?.expectedDate)}</span>}
            </label>
            <label className="flex flex-col gap-1">
              <span className={caption}>Remarks</span>
              {isDraft ? <input value={remarks} maxLength={500} onChange={(event) => setRemarks(event.target.value)} className={cn(input, 'h-10')} /> : <span className="py-2 text-sm">{order?.remarks || '—'}</span>}
            </label>
          </section>

          {isDraft && (
            <div className="print:hidden">
              <LookupInput<Product>
                ref={productRef}
                queryKey={['pharmacy', 'purchase-order', 'products']}
                search={searchProducts}
                getKey={(product) => product.id}
                onSelect={addProduct}
                ariaLabel="Add product"
                placeholder="Add a product: type brand, generic or code…"
                emptyText="No product found."
                renderItem={(product) => (
                  <div className="flex items-baseline justify-between gap-3">
                    <span>
                      <span className="font-medium">{product.productName}</span>
                      {product.genericName && <span className="ml-2 text-xs text-muted-foreground">{product.genericName}</span>}
                    </span>
                    <span className="shrink-0 text-xs text-muted-foreground">{product.unitsPerPack > 1 ? `pack of ${quantity(product.unitsPerPack)}` : 'single unit'}</span>
                  </div>
                )}
              />
            </div>
          )}

          <section className="overflow-hidden rounded-lg border border-border bg-background">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                  <tr>
                    <th className="w-10 px-3 py-2">#</th>
                    <th className="px-3 py-2">Product</th>
                    <th className="w-32 px-3 py-2 text-right">Order (packs)</th>
                    {!isDraft && <th className="w-28 px-3 py-2 text-right">Received</th>}
                    {!isDraft && <th className="w-28 px-3 py-2 text-right">Pending</th>}
                    {canSeeCost && <th className="w-28 px-3 py-2 text-right">Rate</th>}
                    {canSeeCost && <th className="w-28 px-3 py-2 text-right">Amount</th>}
                    {isDraft && <th className="w-10 px-3 py-2 print:hidden" />}
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {lines.length === 0 && (
                    <tr>
                      <td colSpan={8} className="px-3 py-12 text-center text-muted-foreground">
                        No products on this order yet.
                      </td>
                    </tr>
                  )}
                  {lines.map((line, index) => {
                    const saved = order?.lines.find((candidate) => candidate.productId === line.productId);
                    return (
                      <tr key={line.productId}>
                        <td className="px-3 py-2 text-muted-foreground">{index + 1}</td>
                        <td className="px-3 py-2 font-medium text-foreground">{line.productName}</td>
                        <td className="px-3 py-1.5 text-right">
                          {isDraft ? (
                            <input
                              ref={(element) => {
                                if (element) quantityRefs.current.set(line.productId, element);
                                else quantityRefs.current.delete(line.productId);
                              }}
                              value={line.quantity}
                              inputMode="decimal"
                              aria-label={`Packs of ${line.productName}`}
                              onChange={(event) => setLines((current) => current.map((candidate) => (candidate.productId === line.productId ? { ...candidate, quantity: event.target.value } : candidate)))}
                              onKeyDown={(event) => event.key === 'Enter' && productRef.current?.focus()}
                              className={cn(input, 'h-8 text-right tabular-nums')}
                            />
                          ) : (
                            <span className="tabular-nums">{quantity(toNumber(line.quantity))}</span>
                          )}
                        </td>
                        {!isDraft && <td className="px-3 py-2 text-right tabular-nums">{quantity(saved?.receivedQuantity ?? 0)}</td>}
                        {!isDraft && <td className={cn('px-3 py-2 text-right tabular-nums', (saved?.pendingQuantity ?? 0) > 0 ? 'font-medium text-warning' : 'text-muted-foreground')}>{quantity(saved?.pendingQuantity ?? 0)}</td>}
                        {canSeeCost && (
                          <td className="px-3 py-1.5 text-right">
                            {isDraft ? (
                              <input
                                value={line.rate}
                                inputMode="decimal"
                                aria-label={`Rate for ${line.productName}`}
                                onChange={(event) => setLines((current) => current.map((candidate) => (candidate.productId === line.productId ? { ...candidate, rate: event.target.value } : candidate)))}
                                className={cn(input, 'h-8 text-right tabular-nums')}
                              />
                            ) : (
                              <span className="tabular-nums">{money(toNumber(line.rate))}</span>
                            )}
                          </td>
                        )}
                        {canSeeCost && <td className="px-3 py-2 text-right tabular-nums">{money(toNumber(line.quantity) * toNumber(line.rate))}</td>}
                        {isDraft && (
                          <td className="px-2 py-2 print:hidden">
                            <button type="button" onClick={() => setLines((current) => current.filter((candidate) => candidate.productId !== line.productId))} aria-label={`Remove ${line.productName}`} className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive">
                              <Trash2 className="h-3.5 w-3.5" />
                            </button>
                          </td>
                        )}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            {canSeeCost && lines.length > 0 && (
              <p className="flex justify-end gap-3 border-t border-border px-3 py-2 text-sm">
                <span className="text-muted-foreground">Estimated value before tax</span>
                <span className="font-semibold tabular-nums">₹{money(estimated)}</span>
              </p>
            )}
          </section>

          {error && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive print:hidden">
              {error}
            </p>
          )}
        </div>
      )}
    </PharmacyPage>
  );
}
