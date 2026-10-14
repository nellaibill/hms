import { ApiError, type CreateGoodsReceiptRequest, type GoodsReceiptLineRequest, type Product, type ProductBatch } from '@hms/shared';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { FileSpreadsheet, Loader2, Pencil, Save, Trash2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { useToast } from '@/components/ui/toast-context';
import { expiryLabel, money, parseExpiry, quantity, todayIso, toNumber } from '@/features/pharmacy/common/format';
import { roundToRupee, taxExclusive } from '@/features/pharmacy/common/gst';
import { LookupInput, type LookupInputHandle } from '@/features/pharmacy/common/LookupInput';
import { SupplierLookup, type SupplierOption } from '@/features/pharmacy/common/SupplierLookup';
import { useHotkeys } from '@/features/pharmacy/common/useHotkeys';
import { readPurchaseFile } from '@/features/pharmacy/import/spreadsheet';
import { PURCHASE_LINKS } from '@/features/pharmacy/shell/links';
import { KeyHint, PharmacyPage } from '@/features/pharmacy/shell/PharmacyPage';
import { StoreSelect } from '@/features/pharmacy/stores';
import { cn } from '@/lib/utils';
import { pharmacyApi, productsApi } from '@/services/apiClient';

/** A line as it sits in the grid: what the server needs, plus what the grid shows. */
interface PurchaseLine {
  key: string;
  product: Product;
  batchNo: string;
  expiryDate: string;
  quantity: number;
  freeQuantity: number;
  purchaseRate: number;
  mrp: number;
  discountPercent: number;
  gstRatePercent: number;
}

/** The entry strip's fields, kept as text while they are being typed. */
interface Draft {
  batchNo: string;
  expiry: string;
  quantity: string;
  free: string;
  rate: string;
  mrp: string;
  discount: string;
  gst: string;
}

const EMPTY_DRAFT: Draft = { batchNo: '', expiry: '', quantity: '', free: '', rate: '', mrp: '', discount: '', gst: '' };
const DRAFT_ORDER: (keyof Draft)[] = ['batchNo', 'expiry', 'quantity', 'free', 'rate', 'mrp', 'discount', 'gst'];

const cellInput =
  'h-9 w-full rounded-md border border-input bg-background px-2 text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:bg-muted/40';

function lineAmount(line: PurchaseLine) {
  return taxExclusive(line.quantity * line.purchaseRate, line.discountPercent, line.gstRatePercent);
}

/** What the pharmacy keeps on a pack sold at MRP, as a share of MRP — the number a buyer
 * checks a purchase rate against. Free packs and discount are counted in. */
function marginPercent(line: PurchaseLine): number | null {
  const packs = line.quantity + line.freeQuantity;
  if (packs <= 0 || line.mrp <= 0) return null;
  const landed = lineAmount(line).total / packs;
  return ((line.mrp - landed) / line.mrp) * 100;
}

async function searchProducts(term: string): Promise<Product[]> {
  const page = await productsApi.getProducts({ search: term, pageSize: 20, isActive: true, sort: 'productName' });
  return page.items;
}

/**
 * Purchase entry — a supplier's invoice keyed in the way it is printed: the header once, then
 * a line per product with batch, expiry, quantity, free quantity, rate, MRP, discount and GST.
 * Saving it puts the goods into stock and sets each batch's prices. Built to be driven from
 * the keyboard: pick a product, Enter through the fields, and the line drops into the grid
 * with the cursor back on the product box.
 */
export default function PurchaseEntryPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { toast } = useToast();

  const [params] = useSearchParams();
  const purchaseOrderId = params.get('po') ?? undefined;

  const [supplier, setSupplier] = useState<SupplierOption | null>(null);
  const [invoiceNo, setInvoiceNo] = useState('');
  const [invoiceDate, setInvoiceDate] = useState(todayIso());
  const [receivedDate, setReceivedDate] = useState(todayIso());
  const [storeId, setStoreId] = useState('');
  const [isInterState, setIsInterState] = useState(false);
  const [otherCharges, setOtherCharges] = useState('');
  const [remarks, setRemarks] = useState('');

  const [lines, setLines] = useState<PurchaseLine[]>([]);
  const [product, setProduct] = useState<Product | null>(null);
  const [draft, setDraft] = useState<Draft>(EMPTY_DRAFT);
  const [entryError, setEntryError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);

  const [importing, setImporting] = useState(false);
  const [importNotes, setImportNotes] = useState<string[]>([]);
  const fileRef = useRef<HTMLInputElement>(null);
  const productRef = useRef<LookupInputHandle>(null);
  const invoiceNoRef = useRef<HTMLInputElement>(null);
  const fieldRefs = useRef<Partial<Record<keyof Draft, HTMLInputElement | null>>>({});

  // The product's batches already on file: typing a known batch number fills in its expiry
  // and prices, so a repeat purchase of the same batch is two keystrokes.
  const { data: knownBatches } = useQuery({
    queryKey: ['pharmacy', 'purchase-entry', 'batches', product?.id],
    queryFn: async () => (await productsApi.getProductBatches(product!.id, { pageSize: 100 })).items,
    enabled: Boolean(product),
  });

  // Receiving against a purchase order: the supplier comes from the order, and what is
  // still to arrive is listed so each line can be started with one click.
  const { data: order } = useQuery({
    queryKey: ['pharmacy', 'purchase-orders', 'byId', purchaseOrderId],
    queryFn: () => pharmacyApi.getPurchaseOrderById(purchaseOrderId!),
    enabled: Boolean(purchaseOrderId),
  });

  useEffect(() => {
    if (order) setSupplier({ id: order.supplierId, name: order.supplierName, code: '' });
  }, [order]);

  async function startOrderLine(productId: string, pending: number, rate: number) {
    const next = await productsApi.getProductById(productId);
    selectProduct(next);
    setDraft((current) => ({ ...current, quantity: String(pending), rate: rate > 0 ? String(rate) : current.rate }));
  }

  const saveMutation = useMutation({
    mutationFn: (request: CreateGoodsReceiptRequest) => pharmacyApi.createGoodsReceipt(request),
    onSuccess: (receipt) => {
      void queryClient.invalidateQueries({ queryKey: ['pharmacy'] });
      toast({ title: `Purchase saved · ${receipt.receiptNo}`, description: `${receipt.supplierName} · ₹${money(receipt.netAmount)}`, variant: 'success' });
      navigate(`/pharmacy/purchases/${receipt.id}`);
    },
    onError: (error) => setFormError(error instanceof ApiError ? error.message : 'Could not save the purchase.'),
  });

  const totals = lines.reduce(
    (sum, line) => {
      const amount = lineAmount(line);
      return { gross: sum.gross + amount.gross, discount: sum.discount + amount.discount, taxable: sum.taxable + amount.taxable, tax: sum.tax + amount.tax };
    },
    { gross: 0, discount: 0, taxable: 0, tax: 0 },
  );
  const charges = toNumber(otherCharges);
  const { roundOff, net } = roundToRupee(totals.taxable + totals.tax + charges);

  function selectProduct(next: Product) {
    setProduct(next);
    setEntryError(null);
    setDraft({
      ...EMPTY_DRAFT,
      mrp: next.mrp > 0 ? String(next.mrp) : '',
      rate: next.costPrice > 0 ? String(next.costPrice) : '',
      gst: next.gstRatePercent != null ? String(next.gstRatePercent) : '',
    });
    // The batch box mounts with the product; focus it once it exists.
    setTimeout(() => fieldRefs.current.batchNo?.focus(), 0);
  }

  function applyKnownBatch(batchNo: string) {
    const known = knownBatches?.find((batch: ProductBatch) => batch.batchNo.toLowerCase() === batchNo.trim().toLowerCase());
    if (!known) return;
    setDraft((current) => ({
      ...current,
      expiry: current.expiry || expiryLabel(known.expiryDate),
      mrp: known.mrp != null ? String(known.mrp) : current.mrp,
      rate: known.purchaseRate != null ? String(known.purchaseRate) : current.rate,
    }));
  }

  function addLine() {
    if (!product) {
      productRef.current?.focus();
      return;
    }

    const expiryDate = parseExpiry(draft.expiry);
    const next: PurchaseLine = {
      key: crypto.randomUUID(),
      product,
      batchNo: draft.batchNo.trim().toUpperCase(),
      expiryDate: expiryDate ?? '',
      quantity: toNumber(draft.quantity),
      freeQuantity: toNumber(draft.free),
      purchaseRate: toNumber(draft.rate),
      mrp: toNumber(draft.mrp),
      discountPercent: toNumber(draft.discount),
      gstRatePercent: toNumber(draft.gst),
    };

    const problem = !next.batchNo
      ? ['batchNo', 'Enter the batch number.']
      : !expiryDate
        ? ['expiry', 'Enter the expiry as MM/YY, e.g. 03/27.']
        : expiryDate < todayIso()
          ? ['expiry', 'This batch has already expired.']
          : next.quantity + next.freeQuantity <= 0
            ? ['quantity', 'Enter a quantity.']
            : next.mrp <= 0
              ? ['mrp', 'Enter the MRP.']
              : next.discountPercent > 100 || next.gstRatePercent > 100
                ? ['discount', 'Discount and GST are percentages (0 to 100).']
                : lines.some((line) => line.product.id === product.id && line.batchNo === next.batchNo)
                  ? ['batchNo', 'This batch is already on the invoice. Edit that line instead.']
                  : null;

    if (problem) {
      setEntryError(problem[1]);
      fieldRefs.current[problem[0] as keyof Draft]?.focus();
      return;
    }

    setLines((current) => [...current, next]);
    setProduct(null);
    setDraft(EMPTY_DRAFT);
    setEntryError(null);
    setFormError(null);
    productRef.current?.focus();
  }

  function editLine(line: PurchaseLine) {
    setLines((current) => current.filter((candidate) => candidate.key !== line.key));
    setProduct(line.product);
    setDraft({
      batchNo: line.batchNo,
      expiry: expiryLabel(line.expiryDate),
      quantity: String(line.quantity),
      free: line.freeQuantity ? String(line.freeQuantity) : '',
      rate: String(line.purchaseRate),
      mrp: String(line.mrp),
      discount: line.discountPercent ? String(line.discountPercent) : '',
      gst: String(line.gstRatePercent),
    });
    setTimeout(() => fieldRefs.current.quantity?.focus(), 0);
  }

  function handleFieldKeyDown(field: keyof Draft, event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key !== 'Enter') return;
    event.preventDefault();
    if (field === 'batchNo') applyKnownBatch(draft.batchNo);

    const nextField = DRAFT_ORDER[DRAFT_ORDER.indexOf(field) + 1];
    if (nextField) {
      fieldRefs.current[nextField]?.focus();
      fieldRefs.current[nextField]?.select();
    } else {
      addLine();
    }
  }

  /**
   * Loads the lines of a supplier's invoice from the Excel or CSV file their billing software
   * exports. Each row's product is matched by exact name against the catalog; rows that
   * cannot be matched or are incomplete are listed rather than guessed at.
   */
  async function importLines(file: File | undefined) {
    if (!file) return;
    setImporting(true);
    setImportNotes([]);
    try {
      const sheet = await readPurchaseFile(file);
      const added: PurchaseLine[] = [];
      const notes: string[] = [];

      for (const [index, row] of sheet.rows.entries()) {
        const rowNo = sheet.firstRowNumber + index;
        const name = String(row.productName ?? '').trim();
        const found = (await productsApi.getProducts({ search: name, pageSize: 20, isActive: true })).items.find((candidate) => candidate.productName.toLowerCase() === name.toLowerCase());
        const batchNo = String(row.batchNo ?? '').trim().toUpperCase();
        const expiryDate = typeof row.expiryDate === 'string' ? row.expiryDate : '';
        const packs = toNumber(row.quantity);
        const free = toNumber(row.freeQuantity);
        const mrp = toNumber(row.mrp);

        const problem = !found
          ? 'no product with this name in the catalog'
          : !batchNo
            ? 'no batch number'
            : !expiryDate
              ? 'expiry missing or unreadable'
              : expiryDate < todayIso()
                ? 'batch already expired'
                : packs + free <= 0
                  ? 'no quantity'
                  : mrp <= 0
                    ? 'no MRP'
                    : [...lines, ...added].some((line) => line.product.id === found.id && line.batchNo === batchNo)
                      ? 'this batch is already on the invoice'
                      : null;

        if (problem || !found) {
          notes.push(`Row ${rowNo}: ${name} — ${problem}.`);
          continue;
        }

        added.push({
          key: crypto.randomUUID(),
          product: found,
          batchNo,
          expiryDate,
          quantity: packs,
          freeQuantity: free,
          purchaseRate: toNumber(row.purchaseRate),
          mrp,
          discountPercent: Math.min(toNumber(row.discountPercent), 100),
          gstRatePercent: row.gstRatePercent != null ? Math.min(toNumber(row.gstRatePercent), 100) : (found.gstRatePercent ?? 0),
        });
      }

      setLines((current) => [...current, ...added]);
      setImportNotes([`${added.length} of ${sheet.rows.length} lines added from ${file.name}.`, ...notes]);
    } catch (failure) {
      setImportNotes([failure instanceof Error ? failure.message : 'Could not read this file.']);
    } finally {
      setImporting(false);
      if (fileRef.current) fileRef.current.value = '';
    }
  }

  function handleSave() {
    if (saveMutation.isPending) return;

    if (!supplier) {
      setFormError('Choose the supplier.');
      return;
    }
    if (!invoiceNo.trim()) {
      setFormError('Enter the supplier invoice number.');
      invoiceNoRef.current?.focus();
      return;
    }
    if (lines.length === 0) {
      setFormError(product ? 'Press Enter on the last field to add the line you are entering.' : 'Add at least one line.');
      return;
    }
    setFormError(null);

    const requestLines: GoodsReceiptLineRequest[] = lines.map((line) => ({
      productId: line.product.id,
      batchNo: line.batchNo,
      expiryDate: line.expiryDate,
      quantity: line.quantity,
      freeQuantity: line.freeQuantity,
      purchaseRate: line.purchaseRate,
      mrp: line.mrp,
      discountPercent: line.discountPercent,
      gstRatePercent: line.gstRatePercent,
    }));

    saveMutation.mutate({
      storeId: storeId || undefined,
      supplierId: supplier.id,
      purchaseOrderId,
      supplierInvoiceNo: invoiceNo.trim(),
      supplierInvoiceDate: invoiceDate || null,
      receivedDate: receivedDate || null,
      isInterState,
      otherCharges: charges,
      remarks: remarks.trim() || undefined,
      lines: requestLines,
    });
  }

  useHotkeys({
    F2: () => productRef.current?.focus(),
    F10: handleSave,
    'ctrl+s': handleSave,
  });

  const draftField = (field: keyof Draft, label: string, options: { numeric?: boolean; placeholder?: string; list?: string; width: string }) => (
    <label className={cn('flex flex-col gap-1', options.width)}>
      <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</span>
      <input
        ref={(element) => {
          fieldRefs.current[field] = element;
        }}
        value={draft[field]}
        disabled={!product}
        inputMode={options.numeric ? 'decimal' : undefined}
        placeholder={options.placeholder}
        list={options.list}
        autoComplete="off"
        onChange={(event) => setDraft((current) => ({ ...current, [field]: event.target.value }))}
        onBlur={field === 'batchNo' ? () => applyKnownBatch(draft.batchNo) : undefined}
        onKeyDown={(event) => handleFieldKeyDown(field, event)}
        className={cn(cellInput, options.numeric && 'text-right tabular-nums')}
      />
    </label>
  );

  return (
    <PharmacyPage
      title="Purchase entry"
      subtitle="Key in a supplier invoice. Saving it receives the goods into stock."
      subLinks={PURCHASE_LINKS}
      actions={
        <>
          <input ref={fileRef} type="file" accept=".xlsx,.csv" className="hidden" onChange={(event) => void importLines(event.target.files?.[0])} />
          <Button size="sm" variant="outline" onClick={() => fileRef.current?.click()} disabled={importing}>
            {importing ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileSpreadsheet className="h-4 w-4" />}
            Import lines from Excel
          </Button>
          <Button size="sm" onClick={handleSave} disabled={saveMutation.isPending}>
            {saveMutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            Save purchase
          </Button>
        </>
      }
    >
      {importNotes.length > 0 && (
        <section className="rounded-lg border border-border bg-background p-3 text-sm" role="status">
          <p className="font-medium text-foreground">{importNotes[0]}</p>
          {importNotes.length > 1 && (
            <ul className="mt-1 max-h-32 list-disc overflow-y-auto pl-5 text-xs text-destructive">
              {importNotes.slice(1).map((note) => (
                <li key={note}>{note}</li>
              ))}
            </ul>
          )}
        </section>
      )}

      <section className="grid grid-cols-2 gap-3 rounded-lg border border-border bg-background p-3 md:grid-cols-4 xl:grid-cols-6">
        <label className="col-span-2 flex flex-col gap-1">
          <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Supplier</span>
          <SupplierLookup value={supplier} onChange={(next) => { setSupplier(next); setFormError(null); setTimeout(() => invoiceNoRef.current?.focus(), 0); }} autoFocus />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Invoice no.</span>
          <input
            ref={invoiceNoRef}
            value={invoiceNo}
            onChange={(event) => setInvoiceNo(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') productRef.current?.focus();
            }}
            className={cn(cellInput, 'h-10')}
          />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Invoice date</span>
          <input type="date" value={invoiceDate} max={todayIso()} onChange={(event) => setInvoiceDate(event.target.value)} className={cn(cellInput, 'h-10')} />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Received on</span>
          <input type="date" value={receivedDate} max={todayIso()} onChange={(event) => setReceivedDate(event.target.value)} className={cn(cellInput, 'h-10')} />
        </label>
        <label className="flex items-center gap-2 self-end pb-2 text-sm text-foreground">
          <Switch checked={isInterState} onCheckedChange={setIsInterState} aria-label="Supplier is in another state" />
          Other state (IGST)
        </label>
        <div className="col-span-2 empty:hidden">
          <StoreSelect id="purchase-store" value={storeId} onValueChange={setStoreId} label="Receive into" />
        </div>
      </section>

      {order && (
        <section className="rounded-lg border border-border bg-background p-3 text-sm">
          <p className="font-medium text-foreground">
            Receiving against {order.orderNo}
            <span className="ml-2 font-normal text-muted-foreground">Click an item to start its line with the quantity still to come.</span>
          </p>
          <div className="mt-2 flex flex-wrap gap-2">
            {order.lines.filter((line) => line.pendingQuantity > 0 && !lines.some((entered) => entered.product.id === line.productId)).length === 0 && (
              <span className="text-muted-foreground">Everything on the order has been entered or received.</span>
            )}
            {order.lines
              .filter((line) => line.pendingQuantity > 0 && !lines.some((entered) => entered.product.id === line.productId))
              .map((line) => (
                <button
                  key={line.id}
                  type="button"
                  onClick={() => void startOrderLine(line.productId, line.pendingQuantity, line.expectedRate)}
                  className="rounded-md border border-border px-2.5 py-1 hover:border-primary/50 hover:bg-accent"
                >
                  {line.productName} <span className="tabular-nums text-muted-foreground">× {quantity(line.pendingQuantity)}</span>
                </button>
              ))}
          </div>
        </section>
      )}

      <section className="rounded-lg border border-primary/30 bg-primary/5 p-3">
        <div className="flex flex-wrap items-end gap-2">
          <label className="flex min-w-[260px] flex-1 flex-col gap-1">
            <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Product</span>
            <LookupInput<Product>
              ref={productRef}
              queryKey={['pharmacy', 'purchase-entry', 'products']}
              search={searchProducts}
              getKey={(candidate) => candidate.id}
              onSelect={selectProduct}
              ariaLabel="Product"
              placeholder="Type brand, generic or code…"
              displayValue={product?.productName}
              emptyText="No product found. Add it under Products first."
              inputClassName="h-9"
              renderItem={(candidate) => (
                <div className="flex items-baseline justify-between gap-3">
                  <span>
                    <span className="font-medium">{candidate.productName}</span>
                    {candidate.genericName && <span className="ml-2 text-xs text-muted-foreground">{candidate.genericName}</span>}
                  </span>
                  <span className="shrink-0 text-xs text-muted-foreground">
                    {candidate.unitsPerPack > 1 ? `pack of ${quantity(candidate.unitsPerPack)} · ` : ''}MRP {money(candidate.mrp)}
                  </span>
                </div>
              )}
            />
          </label>
          {draftField('batchNo', 'Batch', { width: 'w-28', list: 'purchase-known-batches' })}
          {draftField('expiry', 'Expiry', { width: 'w-20', placeholder: 'MM/YY' })}
          {draftField('quantity', 'Qty', { width: 'w-16', numeric: true })}
          {draftField('free', 'Free', { width: 'w-14', numeric: true })}
          {draftField('rate', 'Rate', { width: 'w-20', numeric: true })}
          {draftField('mrp', 'MRP', { width: 'w-20', numeric: true })}
          {draftField('discount', 'Disc %', { width: 'w-16', numeric: true })}
          {draftField('gst', 'GST %', { width: 'w-16', numeric: true })}
          <Button size="sm" variant="secondary" className="h-9" onClick={addLine} disabled={!product}>
            Add
          </Button>
        </div>
        <datalist id="purchase-known-batches">
          {knownBatches?.map((batch) => <option key={batch.id} value={batch.batchNo} />)}
        </datalist>
        <p className={cn('mt-1.5 text-xs', entryError ? 'text-destructive' : 'text-muted-foreground')} role={entryError ? 'alert' : undefined}>
          {entryError ??
            (product
              ? `${product.productName}${product.unitsPerPack > 1 ? ` · pack of ${quantity(product.unitsPerPack)}, so quantity and rate are per pack` : ''}. Enter moves to the next field; Enter on GST % adds the line.`
              : 'Pick a product to start a line.')}
        </p>
      </section>

      <section className="flex min-h-[220px] flex-1 flex-col overflow-hidden rounded-lg border border-border bg-background">
        <div className="flex-1 overflow-auto">
          <table className="w-full text-sm">
            <thead className="sticky top-0 z-10 bg-sidebar-active text-left text-[11px] font-medium uppercase tracking-wide text-sidebar-active-foreground">
              <tr>
                <th className="w-10 px-3 py-2">#</th>
                <th className="px-3 py-2">Product</th>
                <th className="px-3 py-2">Batch</th>
                <th className="px-3 py-2">Expiry</th>
                <th className="px-3 py-2 text-right">Qty</th>
                <th className="px-3 py-2 text-right">Free</th>
                <th className="px-3 py-2 text-right">Rate</th>
                <th className="px-3 py-2 text-right">MRP</th>
                <th className="px-3 py-2 text-right">Disc %</th>
                <th className="px-3 py-2 text-right">GST %</th>
                <th className="px-3 py-2 text-right">Margin</th>
                <th className="px-3 py-2 text-right">Amount</th>
                <th className="w-20 px-3 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {lines.length === 0 && (
                <tr>
                  <td colSpan={13} className="px-3 py-12 text-center text-sm text-muted-foreground">
                    No lines yet. Lines appear here as you add them.
                  </td>
                </tr>
              )}
              {lines.map((line, index) => {
                const amount = lineAmount(line);
                const margin = marginPercent(line);
                return (
                  <tr key={line.key} className="hover:bg-muted/30">
                    <td className="px-3 py-2 text-muted-foreground">{index + 1}</td>
                    <td className="px-3 py-2">
                      <div className="font-medium text-foreground">{line.product.productName}</div>
                      <div className="text-xs text-muted-foreground">
                        {line.product.unitsPerPack > 1 ? `Pack of ${quantity(line.product.unitsPerPack)}` : 'Single unit'}
                        {line.product.hsnCode ? ` · HSN ${line.product.hsnCode}` : ''}
                      </div>
                    </td>
                    <td className="px-3 py-2 font-mono text-xs">{line.batchNo}</td>
                    <td className="px-3 py-2">{expiryLabel(line.expiryDate)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{quantity(line.quantity)}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{line.freeQuantity ? quantity(line.freeQuantity) : '—'}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(line.purchaseRate)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{money(line.mrp)}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-muted-foreground">{line.discountPercent ? quantity(line.discountPercent) : '—'}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{quantity(line.gstRatePercent)}</td>
                    <td className={cn('px-3 py-2 text-right tabular-nums', margin != null && margin < 0 ? 'font-medium text-destructive' : 'text-muted-foreground')}>
                      {margin == null ? '—' : `${margin.toFixed(1)}%`}
                    </td>
                    <td className="px-3 py-2 text-right font-medium tabular-nums">{money(amount.total)}</td>
                    <td className="px-3 py-2">
                      <div className="flex justify-end gap-1">
                        <button type="button" onClick={() => editLine(line)} aria-label={`Edit line ${index + 1}`} className="rounded p-1 text-muted-foreground hover:bg-accent hover:text-foreground">
                          <Pencil className="h-3.5 w-3.5" />
                        </button>
                        <button
                          type="button"
                          onClick={() => setLines((current) => current.filter((candidate) => candidate.key !== line.key))}
                          aria-label={`Remove line ${index + 1}`}
                          className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>

      <section className="grid gap-3 lg:grid-cols-[minmax(0,1fr)_340px]">
        <div className="flex flex-col gap-3 rounded-lg border border-border bg-background p-3">
          <div className="grid gap-3 sm:grid-cols-[160px_minmax(0,1fr)]">
            <label className="flex flex-col gap-1">
              <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Other charges</span>
              <input value={otherCharges} inputMode="decimal" placeholder="Freight, packing" onChange={(event) => setOtherCharges(event.target.value)} className={cn(cellInput, 'text-right tabular-nums')} />
            </label>
            <label className="flex flex-col gap-1">
              <span className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">Remarks</span>
              <input value={remarks} maxLength={500} onChange={(event) => setRemarks(event.target.value)} className={cellInput} />
            </label>
          </div>
          {formError && (
            <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {formError}
            </p>
          )}
          <div className="mt-auto flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
            <KeyHint keys="F2">Product</KeyHint>
            <KeyHint keys="Enter">Next field / add line</KeyHint>
            <KeyHint keys="F10">Save</KeyHint>
          </div>
        </div>

        <dl className="rounded-lg border border-border bg-background p-3 text-sm">
          <div className="flex justify-between py-0.5">
            <dt className="text-muted-foreground">Lines</dt>
            <dd className="tabular-nums">{lines.length}</dd>
          </div>
          <div className="flex justify-between py-0.5">
            <dt className="text-muted-foreground">Gross</dt>
            <dd className="tabular-nums">{money(totals.gross)}</dd>
          </div>
          <div className="flex justify-between py-0.5">
            <dt className="text-muted-foreground">Discount</dt>
            <dd className="tabular-nums">− {money(totals.discount)}</dd>
          </div>
          <div className="flex justify-between py-0.5">
            <dt className="text-muted-foreground">Taxable</dt>
            <dd className="tabular-nums">{money(totals.taxable)}</dd>
          </div>
          <div className="flex justify-between py-0.5">
            <dt className="text-muted-foreground">{isInterState ? 'IGST' : 'CGST + SGST'}</dt>
            <dd className="tabular-nums">{money(totals.tax)}</dd>
          </div>
          {charges > 0 && (
            <div className="flex justify-between py-0.5">
              <dt className="text-muted-foreground">Other charges</dt>
              <dd className="tabular-nums">{money(charges)}</dd>
            </div>
          )}
          <div className="flex justify-between py-0.5">
            <dt className="text-muted-foreground">Round off</dt>
            <dd className="tabular-nums">{money(roundOff)}</dd>
          </div>
          <div className="mt-2 flex items-baseline justify-between border-t border-border pt-2">
            <dt className="font-medium text-foreground">Invoice total</dt>
            <dd className="text-2xl font-semibold tabular-nums text-foreground">₹{money(net)}</dd>
          </div>
        </dl>
      </section>
    </PharmacyPage>
  );
}
