import { ApiError, type ReorderSuggestionResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ClipboardList, Loader2 } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { money, quantity, toNumber } from '@/features/pharmacy/common/format';
import { SupplierLookup, type SupplierOption } from '@/features/pharmacy/common/SupplierLookup';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { pharmacyApi } from '@/services/apiClient';

const NO_SUPPLIER = 'none';

/**
 * What has run low, and how much to buy. Products at or below their reorder level are listed
 * under the supplier each was last bought from, with a suggested quantity that can be
 * changed. Ticked lines become purchase orders — one per supplier — in a single step.
 */
export default function ReorderPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const canOrder = hasPermission('pharmacy.create');

  const [storeId, setStoreId] = useState('');
  const [picked, setPicked] = useState<Record<string, boolean>>({});
  const [quantities, setQuantities] = useState<Record<string, string>>({});
  // Products that have never come in on a purchase have no "last supplier"; the buyer names one.
  const [fallbackSupplier, setFallbackSupplier] = useState<SupplierOption | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { data, isPending, isError } = useQuery({
    queryKey: ['pharmacy', 'reorder', storeId],
    queryFn: () => pharmacyApi.getReorderSuggestions(storeId || undefined),
  });

  // Start with everything that has a suggestion and a known supplier ticked: the common
  // case is "order all of it", and unticking the few exceptions is quicker than ticking.
  useEffect(() => {
    if (!data) return;
    setPicked(Object.fromEntries(data.map((row) => [row.productId, row.suggestedQuantity > 0 && Boolean(row.lastSupplierId)])));
    setQuantities(Object.fromEntries(data.map((row) => [row.productId, row.suggestedQuantity > 0 ? String(row.suggestedQuantity) : ''])));
  }, [data]);

  const groups = useMemo(() => {
    const bySupplier = new Map<string, { supplierId: string | null; supplierName: string; rows: ReorderSuggestionResponse[] }>();
    for (const row of data ?? []) {
      const key = row.lastSupplierId ?? NO_SUPPLIER;
      if (!bySupplier.has(key)) bySupplier.set(key, { supplierId: row.lastSupplierId ?? null, supplierName: row.lastSupplierName ?? 'Never purchased — no supplier yet', rows: [] });
      bySupplier.get(key)!.rows.push(row);
    }
    return [...bySupplier.values()];
  }, [data]);

  // The never-purchased group orders from whichever supplier the buyer names for it; if that
  // is a supplier who already has a group, the two become one order.
  const bySupplierId = new Map<string, { supplierId: string; supplierName: string; rows: ReorderSuggestionResponse[] }>();
  for (const group of groups) {
    const supplierId = group.supplierId ?? fallbackSupplier?.id;
    if (!supplierId) continue;
    const rows = group.rows.filter((row) => picked[row.productId] && toNumber(quantities[row.productId]) > 0);
    if (rows.length === 0) continue;
    const existing = bySupplierId.get(supplierId);
    if (existing) existing.rows.push(...rows);
    else bySupplierId.set(supplierId, { supplierId, supplierName: group.supplierId ? group.supplierName : fallbackSupplier!.name, rows: [...rows] });
  }
  const orderable = [...bySupplierId.values()];
  const lineCount = orderable.reduce((sum, group) => sum + group.rows.length, 0);

  const createMutation = useMutation({
    mutationFn: async () => {
      const created = [];
      for (const group of orderable) {
        created.push(
          await pharmacyApi.createPurchaseOrder({
            storeId: storeId || undefined,
            supplierId: group.supplierId,
            placeOrder: false,
            lines: group.rows.map((row) => ({ productId: row.productId, quantity: toNumber(quantities[row.productId]), expectedRate: row.lastPurchaseRate ?? undefined })),
          }),
        );
      }
      return created;
    },
    onSuccess: (created) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
      toast({ title: `${created.length} draft ${created.length === 1 ? 'order' : 'orders'} created`, description: created.map((order) => `${order.orderNo} · ${order.supplierName}`).join(', '), variant: 'success' });
      navigate(created.length === 1 ? `/pharmacy/purchase-orders/${created[0].id}` : '/pharmacy/purchase-orders');
    },
    onError: (failure) => setError(failure instanceof ApiError ? failure.message : 'Could not create the purchase orders.'),
  });

  return (
    <PharmacyPage
      title="Reorder list"
      subtitle="Products at or below their reorder level, and a suggested quantity to buy."
      subLinks={PURCHASE_LINKS}
      actions={
        canOrder && (
          <Button size="sm" onClick={() => createMutation.mutate()} disabled={lineCount === 0 || createMutation.isPending}>
            {createMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <ClipboardList className="h-4 w-4" />}
            Create {orderable.length || ''} draft {orderable.length === 1 ? 'order' : 'orders'}
          </Button>
        )
      }
    >
      <div className="flex flex-wrap items-end justify-between gap-3">
        <StoreSelect id="reorder-store" value={storeId} onValueChange={setStoreId} allowAll label="Stock in" />
        <p className="text-sm text-muted-foreground">
          {lineCount} {lineCount === 1 ? 'line' : 'lines'} ticked across {orderable.length} {orderable.length === 1 ? 'supplier' : 'suppliers'}. Orders are created as drafts for you to check and send.
        </p>
      </div>

      {error && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {error}
        </p>
      )}

      {isPending && (
        <div className="flex items-center justify-center gap-2 py-16 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Working out what is low…
        </div>
      )}
      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          The reorder list could not be loaded.
        </p>
      )}

      {data && data.length === 0 && (
        <p className="rounded-lg border border-dashed border-border bg-background p-10 text-center text-sm text-muted-foreground">
          Nothing is at or below its reorder level. Set a reorder level on a product for it to appear here when it runs low.
        </p>
      )}

      {groups.map((group) => (
        <section key={group.supplierId ?? NO_SUPPLIER} className="overflow-hidden rounded-lg border border-border bg-background">
          <h2 className="flex items-center justify-between gap-3 border-b border-border bg-muted/40 px-3 py-2 text-sm font-medium text-foreground">
            {group.supplierName}
            <span className="text-xs font-normal text-muted-foreground">{group.rows.length} products</span>
          </h2>
          {!group.supplierId && (
            <div className="flex flex-wrap items-center gap-3 border-b border-border px-3 py-2">
              <p className="text-xs text-muted-foreground">These have never come in on a purchase. Choose who to order them from:</p>
              <div className="w-72">
                <SupplierLookup
                  value={fallbackSupplier}
                  onChange={(supplier) => {
                    setFallbackSupplier(supplier);
                    setPicked((current) => ({ ...current, ...Object.fromEntries(group.rows.filter((row) => row.suggestedQuantity > 0).map((row) => [row.productId, true])) }));
                  }}
                />
              </div>
            </div>
          )}
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="text-left text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="w-10 px-3 py-2" />
                  <th className="px-3 py-2">Product</th>
                  <th className="px-3 py-2 text-right">In stock</th>
                  <th className="px-3 py-2 text-right">Reorder at</th>
                  <th className="px-3 py-2 text-right">On order</th>
                  <th className="px-3 py-2 text-right">Last rate</th>
                  <th className="w-32 px-3 py-2 text-right">Order (packs)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {group.rows.map((row) => (
                  <tr key={row.productId} className="hover:bg-muted/30">
                    <td className="px-3 py-2">
                      <input
                        type="checkbox"
                        checked={Boolean(picked[row.productId])}
                        disabled={!group.supplierId && !fallbackSupplier}
                        aria-label={`Order ${row.productName}`}
                        onChange={(event) => setPicked((current) => ({ ...current, [row.productId]: event.target.checked }))}
                      />
                    </td>
                    <td className="px-3 py-2">
                      <div className="font-medium text-foreground">{row.productName}</div>
                      <div className="text-xs text-muted-foreground">
                        {row.genericName ? `${row.genericName} · ` : ''}
                        {row.unitsPerPack > 1 ? `pack of ${quantity(row.unitsPerPack)}` : 'single unit'}
                      </div>
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums">
                      {quantity(row.availableQuantity)}
                      <div>
                        <Badge variant={row.status === 'OutOfStock' ? 'destructive' : 'warning'}>{row.status === 'OutOfStock' ? 'Out of stock' : 'Low'}</Badge>
                      </div>
                    </td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{quantity(row.reorderLevel)}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{row.onOrderQuantity > 0 ? `${quantity(row.onOrderQuantity)} packs` : '—'}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{row.lastPurchaseRate != null ? money(row.lastPurchaseRate) : '—'}</td>
                    <td className="px-3 py-1.5 text-right">
                      <input
                        value={quantities[row.productId] ?? ''}
                        inputMode="decimal"
                        disabled={!group.supplierId && !fallbackSupplier}
                        aria-label={`Packs of ${row.productName} to order`}
                        onChange={(event) => setQuantities((current) => ({ ...current, [row.productId]: event.target.value }))}
                        className="h-8 w-24 rounded border border-input bg-background px-2 text-right text-sm tabular-nums outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:opacity-50"
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ))}
    </PharmacyPage>
  );
}
