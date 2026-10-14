import { ApiError, type ReturnableBatchResponse } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowRight, ChevronDown, ChevronRight, Loader2, Plus, Save, Trash2, X } from 'lucide-react';
import { Fragment, useState } from 'react';
import { Pagination } from '@/components/Pagination';
import { Button } from '@/components/ui/button';
import { useToast } from '@/components/ui/toast-context';
import { useAuth } from '@/features/auth/AuthContext';
import { dateLabel, expiryLabel, quantity, toNumber } from '@/features/pharmacy/common/format';
import { StoreBatchPicker } from '@/features/pharmacy/common/StoreBatchPicker';
import { STOCK_LINKS } from '@/features/pharmacy/shell/links';
import { PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { useStoresQuery } from '@/features/pharmacy/stores';
import { cn } from '@/lib/utils';
import { pharmacyApi } from '@/services/apiClient';

interface TransferLine {
  batch: ReturnableBatchResponse;
  quantity: string;
}

const input = 'h-9 rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20';

/**
 * Transfers between stores — main store to a counter, a counter to a ward. A transfer is
 * done in one step: saving it takes the stock out of one store and puts it in the other.
 */
export default function StockTransfersPage() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const { hasPermission } = useAuth();
  const { data: stores } = useStoresQuery();
  const activeStores = (stores ?? []).filter((store) => store.isActive);

  const [page, setPage] = useState(1);
  const [openId, setOpenId] = useState<string | null>(null);
  const [entering, setEntering] = useState(false);
  const [fromStoreId, setFromStoreId] = useState('');
  const [toStoreId, setToStoreId] = useState('');
  const [remarks, setRemarks] = useState('');
  const [lines, setLines] = useState<TransferLine[]>([]);
  const [error, setError] = useState<string | null>(null);

  const { data, isPending } = useQuery({
    queryKey: ['pharmacy', 'transfers', 'list', page],
    queryFn: () => pharmacyApi.getStockTransfers({ page, pageSize: 20 }),
    placeholderData: (previous) => previous,
  });
  const { data: detail } = useQuery({ queryKey: ['pharmacy', 'transfers', 'byId', openId], queryFn: () => pharmacyApi.getStockTransferById(openId!), enabled: Boolean(openId) });

  const mutation = useMutation({
    mutationFn: () =>
      pharmacyApi.createStockTransfer({
        fromStoreId,
        toStoreId,
        remarks: remarks.trim() || undefined,
        lines: lines.map((line) => ({ productId: line.batch.productId, productBatchId: line.batch.productBatchId, quantity: toNumber(line.quantity) })),
      }),
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
      toast({ title: `Transfer saved · ${saved.transferNo}`, description: `${saved.fromStoreName} → ${saved.toStoreName} · ${saved.lineCount} lines`, variant: 'success' });
      setEntering(false);
      setLines([]);
      setRemarks('');
      setError(null);
    },
    onError: (failure) => setError(failure instanceof ApiError ? (failure.validationErrors?.[0]?.message ?? failure.message) : 'Could not save the transfer.'),
  });

  function save() {
    if (!fromStoreId || !toStoreId || fromStoreId === toStoreId) {
      setError('Choose the store the stock is leaving and a different store it is going to.');
      return;
    }
    const over = lines.find((line) => toNumber(line.quantity) > line.batch.quantityOnHand);
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

  return (
    <PharmacyPage
      title="Stock transfers"
      subtitle="Stock moved from one store to another."
      subLinks={STOCK_LINKS}
      actions={
        hasPermission('pharmacy.edit') &&
        activeStores.length > 1 && (
          <Button size="sm" variant={entering ? 'outline' : 'default'} onClick={() => { setEntering(!entering); setError(null); }}>
            {entering ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />}
            {entering ? 'Close' : 'New transfer'}
          </Button>
        )
      }
    >
      {stores && activeStores.length < 2 && (
        <p className="rounded-lg border border-dashed border-border bg-background p-6 text-sm text-muted-foreground">
          Transfers need at least two stores. This pharmacy has one; add a main store, a second counter or a ward sub-store under Setup → Stores.
        </p>
      )}

      {entering && (
        <section className="flex flex-col gap-3 rounded-lg border border-border bg-background p-3">
          <div className="flex flex-wrap items-end gap-3">
            <label className="flex flex-col gap-1 text-xs text-muted-foreground">
              From
              <select value={fromStoreId} onChange={(event) => { setFromStoreId(event.target.value); setLines([]); }} className={cn(input, 'min-w-[180px]')}>
                <option value="">Choose store…</option>
                {activeStores.map((store) => (
                  <option key={store.id} value={store.id}>
                    {store.name}
                  </option>
                ))}
              </select>
            </label>
            <ArrowRight className="mb-2.5 h-4 w-4 text-muted-foreground" />
            <label className="flex flex-col gap-1 text-xs text-muted-foreground">
              To
              <select value={toStoreId} onChange={(event) => setToStoreId(event.target.value)} className={cn(input, 'min-w-[180px]')}>
                <option value="">Choose store…</option>
                {activeStores
                  .filter((store) => store.id !== fromStoreId)
                  .map((store) => (
                    <option key={store.id} value={store.id}>
                      {store.name}
                    </option>
                  ))}
              </select>
            </label>
            <label className="flex min-w-[200px] flex-1 flex-col gap-1 text-xs text-muted-foreground">
              Remarks
              <input value={remarks} maxLength={500} onChange={(event) => setRemarks(event.target.value)} className={cn(input, 'w-full')} />
            </label>
            <Button onClick={save} disabled={mutation.isPending}>
              {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
              Save transfer
            </Button>
          </div>

          {fromStoreId && (
            <StoreBatchPicker
              storeId={fromStoreId}
              pickedBatchIds={lines.map((line) => line.batch.productBatchId)}
              onPick={(batch) => setLines((current) => [...current, { batch, quantity: '' }])}
              placeholder="Find the product to move…"
            />
          )}

          {lines.length > 0 && (
            <table className="w-full text-sm">
              <thead className="text-left text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="py-1.5 pr-3">Product</th>
                  <th className="py-1.5 pr-3">Batch · expiry</th>
                  <th className="py-1.5 pr-3 text-right">In stock</th>
                  <th className="w-32 py-1.5 pr-3 text-right">Move (units)</th>
                  <th className="w-8" />
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {lines.map((line) => (
                  <tr key={line.batch.productBatchId}>
                    <td className="py-1.5 pr-3 font-medium text-foreground">{line.batch.productName}</td>
                    <td className="py-1.5 pr-3 font-mono text-xs">
                      {line.batch.batchNo} · {expiryLabel(line.batch.expiryDate)}
                    </td>
                    <td className="py-1.5 pr-3 text-right tabular-nums text-muted-foreground">{quantity(line.batch.quantityOnHand)}</td>
                    <td className="py-1.5 pr-3">
                      <input
                        value={line.quantity}
                        inputMode="decimal"
                        aria-label={`Units of ${line.batch.productName} to move`}
                        onChange={(event) => setLines((current) => current.map((candidate) => (candidate.batch.productBatchId === line.batch.productBatchId ? { ...candidate, quantity: event.target.value } : candidate)))}
                        className={cn(input, 'h-8 w-full text-right tabular-nums', toNumber(line.quantity) > line.batch.quantityOnHand && 'border-destructive')}
                      />
                    </td>
                    <td>
                      <button type="button" onClick={() => setLines((current) => current.filter((candidate) => candidate.batch.productBatchId !== line.batch.productBatchId))} aria-label={`Remove ${line.batch.productName}`} className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive">
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </td>
                  </tr>
                ))}
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
          Loading transfers…
        </div>
      )}

      {data && (
        <>
          <div className="overflow-x-auto rounded-lg border border-border bg-background">
            <table className="w-full text-sm">
              <thead className="bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
                <tr>
                  <th className="w-8 px-2 py-2" />
                  <th className="px-3 py-2">Transfer no.</th>
                  <th className="px-3 py-2">Date</th>
                  <th className="px-3 py-2">From</th>
                  <th className="px-3 py-2">To</th>
                  <th className="px-3 py-2">Remarks</th>
                  <th className="px-3 py-2 text-right">Lines</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-3 py-12 text-center text-muted-foreground">
                      No stock has been transferred between stores.
                    </td>
                  </tr>
                )}
                {data.items.map((item) => {
                  const open = openId === item.id;
                  return (
                    <Fragment key={item.id}>
                      <tr onClick={() => setOpenId(open ? null : item.id)} className="cursor-pointer hover:bg-muted/40">
                        <td className="px-2 py-2 text-muted-foreground">{open ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}</td>
                        <td className="whitespace-nowrap px-3 py-2 font-mono text-xs font-medium text-primary">{item.transferNo}</td>
                        <td className="px-3 py-2">{dateLabel(item.transferDate)}</td>
                        <td className="px-3 py-2">{item.fromStoreName}</td>
                        <td className="px-3 py-2">{item.toStoreName}</td>
                        <td className="px-3 py-2 text-muted-foreground">{item.remarks ?? '—'}</td>
                        <td className="px-3 py-2 text-right tabular-nums">{item.lineCount}</td>
                      </tr>
                      {open && (
                        <tr>
                          <td colSpan={7} className="bg-muted/20 px-4 py-3">
                            {detail?.id === item.id ? (
                              <ul className="divide-y divide-border text-sm">
                                {detail.lines.map((line) => (
                                  <li key={line.id} className="flex justify-between gap-3 py-1">
                                    <span>
                                      {line.productName} <span className="font-mono text-xs text-muted-foreground">{line.batchNo} · {expiryLabel(line.expiryDate)}</span>
                                    </span>
                                    <span className="tabular-nums">{quantity(line.quantity)}</span>
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
