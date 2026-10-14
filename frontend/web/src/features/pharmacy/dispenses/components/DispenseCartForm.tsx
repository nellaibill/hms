import {
  ApiError,
  createDispenseCartSchema,
  type AvailableBatchResponse,
  type DispenseCartFormValues,
  type Patient,
  type Product,
} from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Minus, Plus, ShoppingCart, UserRound } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { ProductSelect } from '@/components/ProductSelect';
import { useProductsQuery } from '@/features/pharmacy/product-lookup';
import { StoreSelect } from '@/features/pharmacy/stores';
import { AvailableBatchSelect } from './AvailableBatchSelect';
import { QuickPickPanel } from './QuickPickPanel';
import { PatientNameLink } from '@/components/PatientNameLink';

interface DispenseCartFormProps {
  patient: Patient;
  onChangePatient: () => void;
  onSubmit: (values: DispenseCartFormValues) => void;
  isSubmitting: boolean;
  apiError: ApiError | null;
}

interface StagingLine {
  product: Product | null;
  batch: AvailableBatchResponse | null;
  quantity: number;
  remarks: string;
}

const emptyStagingLine: StagingLine = { product: null, batch: null, quantity: 1, remarks: '' };

/** What the cart shows for a line beyond the ids the form submits — captured when the line
 * is added, so a cart row never depends on its product still being in some loaded list. */
interface CartLineDetail {
  productName: string;
  batchNo: string;
  unitPrice: number;
  availableQuantity: number;
}

const lineKey = (line: { productId: string; productBatchId: string }) => `${line.productId}|${line.productBatchId}`;

/**
 * POS-style checkout: a single "Add Item" builder feeds a running cart shown in a sticky
 * summary panel (mirrors BillingSummaryCard's grid-cols-[1fr_320px] + lg:sticky pattern) —
 * replaces the old one-row-per-line form, which wasted most of a wide screen on a single
 * narrow column and only showed the running total after scrolling past every line.
 *
 * Cart lines are appended directly via useFieldArray.append() rather than bound to per-line
 * <input>s — once added, a line is a fixed cart entry (read-only + Remove), not something the
 * operator edits in place, so there's nothing for register() to bind to.
 */
export function DispenseCartForm({ patient, onChangePatient, onSubmit, isSubmitting, apiError }: DispenseCartFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    watch,
    formState: { errors },
  } = useForm<DispenseCartFormValues>({
    resolver: zodResolver(createDispenseCartSchema),
    defaultValues: { patientId: patient.id, admissionId: '', storeId: '', lines: [] },
  });

  const { fields, append, remove, update, replace } = useFieldArray({ control, name: 'lines' });
  const storeId = watch('storeId') || undefined;

  function handleStoreChange(nextStoreId: string) {
    // Every cart line names a batch as held in the store it was picked from; another store
    // holds different batches and quantities, so the cart can't carry over.
    setValue('storeId', nextStoreId);
    replace([]);
    setLineDetails({});
    setStaging(emptyStagingLine);
    setStagingError(null);
  }

  const [staging, setStaging] = useState<StagingLine>(emptyStagingLine);
  const [stagingError, setStagingError] = useState<string | null>(null);
  const [lineDetails, setLineDetails] = useState<Record<string, CartLineDetail>>({});

  // Only the Quick Pick tiles — a fixed first page, not the catalog. Anything beyond it is
  // reached through the Product field's server-side search.
  const { data: quickPickData } = useProductsQuery({ pageSize: 100, isActive: true, sort: 'productName' });
  const quickPickProducts = quickPickData?.items ?? [];

  function lineTotal(line: { productId: string; productBatchId: string; quantity: number }) {
    return (Number(line.quantity) || 0) * (lineDetails[lineKey(line)]?.unitPrice ?? 0);
  }

  const total = fields.reduce((sum, field) => sum + lineTotal(field), 0);
  const totalQuantity = fields.reduce((sum, field) => sum + (Number(field.quantity) || 0), 0);

  // Server-side validation failures (docs/ApiStandards.md §5) that target top-level fields map
  // onto the same field-level display client validation uses — line-level failures (e.g. "Line
  // 2: insufficient stock") aren't individually field-mapped, so they surface via generalError
  // below instead, same as DispenseForm's own approach for its 409s.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }

    for (const issue of apiError.validationErrors) {
      if (issue.field === 'PatientId' || issue.field === 'AdmissionId') {
        const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as 'patientId' | 'admissionId';
        setError(fieldName, { type: 'server', message: issue.message });
      }
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  // See DispenseForm's identical guard for the full explanation: isSubmitting alone can't
  // prevent a fast double-click from firing two checkouts.
  const submitLockRef = useRef(false);

  useEffect(() => {
    if (!isSubmitting) {
      submitLockRef.current = false;
    }
  }, [isSubmitting]);

  function guardedSubmit(values: DispenseCartFormValues) {
    if (submitLockRef.current) {
      return;
    }
    submitLockRef.current = true;
    onSubmit(values);
  }

  function selectProduct(product: Product) {
    // A batch picked under the previous product is meaningless once the product changes;
    // clearing it lets AvailableBatchSelect default to the new product's earliest expiry.
    setStaging((s) => ({ ...s, product, batch: null }));
    setStagingError(null);
  }

  function handleAddToCart() {
    const { product, batch, quantity, remarks } = staging;
    if (!product) {
      setStagingError('Choose a product.');
      return;
    }
    if (!batch) {
      setStagingError('Choose a batch.');
      return;
    }
    if (!quantity || quantity <= 0) {
      setStagingError('Quantity must be greater than 0.');
      return;
    }
    if (quantity > batch.quantityOnHand) {
      setStagingError(`Only ${batch.quantityOnHand} on hand in batch ${batch.batchNo}. Add the rest from another batch.`);
      return;
    }

    const line = { productId: product.id, productBatchId: batch.productBatchId, quantity, remarks };
    setLineDetails((details) => ({
      ...details,
      [lineKey(line)]: {
        productName: product.productName,
        batchNo: batch.batchNo,
        // The server's figure for this batch (its own MRP or rate, per unit) — the same one
        // the dispense will bill, rather than a price worked out again here.
        unitPrice: batch.unitPrice,
        availableQuantity: batch.quantityOnHand,
      },
    }));
    append(line);
    setStaging(emptyStagingLine);
    setStagingError(null);
  }

  function handleStagingKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter') {
      e.preventDefault();
      handleAddToCart();
    }
  }

  return (
    <form onSubmit={handleSubmit(guardedSubmit)} noValidate className="flex flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}
      {errors.lines?.message && fields.length === 0 && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {errors.lines.message}
        </p>
      )}

      <div className="grid grid-cols-1 items-start gap-4 lg:grid-cols-[minmax(0,1fr)_340px]">
        <div className="flex flex-col gap-4">
          <QuickPickPanel products={quickPickProducts} selectedProductId={staging.product?.id ?? ''} onSelectProduct={selectProduct} />

          <Card>
            <CardHeader className="flex-row items-center gap-3 space-y-0 p-4">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
                <Plus className="h-4.5 w-4.5" />
              </span>
              <CardTitle className="text-base">Add Item</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-3 p-4 pt-0">
              <div className="flex flex-wrap items-end gap-3">
                <div className="flex min-w-[220px] flex-1 flex-col gap-1.5">
                  <Label htmlFor="staging-product">Product</Label>
                  {/* onProductChange carries the whole product (name and price for the cart);
                      the id-only onValueChange has nothing left to do here. */}
                  <ProductSelect id="staging-product" value={staging.product?.id ?? ''} onValueChange={() => {}} onProductChange={selectProduct} />
                </div>

                <div className="flex min-w-[260px] flex-1 flex-col gap-1.5">
                  <Label htmlFor="staging-batch">Batch</Label>
                  <AvailableBatchSelect
                    id="staging-batch"
                    productId={staging.product?.id}
                    storeId={storeId}
                    value={staging.batch?.productBatchId ?? ''}
                    onBatchChange={(batch) => setStaging((s) => ({ ...s, batch }))}
                    excludeBatchIds={fields.filter((f) => f.productId === staging.product?.id).map((f) => f.productBatchId)}
                  />
                </div>

                <div className="flex w-24 flex-col gap-1.5">
                  <Label htmlFor="staging-quantity">Qty</Label>
                  <Input
                    id="staging-quantity"
                    type="number"
                    min="0"
                    max={staging.batch?.quantityOnHand}
                    step="any"
                    value={staging.quantity}
                    onChange={(e) => setStaging((s) => ({ ...s, quantity: Number(e.target.value) }))}
                    onKeyDown={handleStagingKeyDown}
                  />
                </div>

                <Button type="button" className="gap-1.5" onClick={handleAddToCart}>
                  <Plus className="h-4 w-4" />
                  Add
                </Button>
              </div>

              <Input
                placeholder="Remarks (optional)"
                value={staging.remarks}
                onChange={(e) => setStaging((s) => ({ ...s, remarks: e.target.value }))}
                onKeyDown={handleStagingKeyDown}
              />

              {stagingError && <p className="text-sm text-destructive">{stagingError}</p>}
            </CardContent>
          </Card>

        </div>

        <Card className="lg:sticky lg:top-20">
          <CardHeader className="flex-row items-center gap-3 space-y-0 p-4">
            <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-accent text-accent-foreground">
              <ShoppingCart className="h-4.5 w-4.5" />
            </span>
            <CardTitle className="text-base">Cart</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3 p-4 pt-0">
            <div className="flex items-center justify-between gap-3 rounded-md bg-muted/40 px-3 py-2">
              <div className="flex items-center gap-2.5">
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                  <UserRound className="h-4 w-4" />
                </span>
                <div className="flex flex-col">
                  <PatientNameLink patientId={patient.id} newTab className="text-sm">
                    {patient.title} {patient.firstName} {patient.lastName}
                  </PatientNameLink>
                  <span className="text-xs text-muted-foreground">
                    {patient.uhid} · {patient.primaryPhone}
                  </span>
                </div>
              </div>
              <Button type="button" variant="ghost" size="sm" onClick={onChangePatient}>
                Change
              </Button>
            </div>

            <StoreSelect id="dispense-store" label="Dispense from" value={storeId ?? ''} onValueChange={handleStoreChange} />

            {/* Checkout-level (not per-item), so it lives in the cart beside the patient rather
                than floating loose under the Add Item card in a fixed-width box. */}
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="admissionId">Admission reference (optional)</Label>
              <Input id="admissionId" placeholder="Admission ID, for inpatients" {...register('admissionId')} />
              {errors.admissionId && <p className="text-sm text-destructive">{errors.admissionId.message}</p>}
            </div>

            <Separator />

            {fields.length === 0 ? (
              <p className="py-2 text-center text-sm text-muted-foreground">No items added yet.</p>
            ) : (
              <div className="flex flex-col gap-3">
                {fields.map((field, index) => {
                  const detail = lineDetails[lineKey(field)];
                  return (
                    <CartLineRow
                      key={field.id}
                      productName={detail?.productName ?? 'Unknown product'}
                      batchNo={detail?.batchNo ?? ''}
                      quantity={field.quantity}
                      availableQuantity={detail?.availableQuantity ?? field.quantity}
                      lineTotal={lineTotal(field)}
                      onQuantityChange={(quantity) => update(index, { ...field, quantity })}
                      onRemove={() => remove(index)}
                    />
                  );
                })}
              </div>
            )}

            <Separator />

            <div className="flex flex-col gap-1 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Total items</span>
                <span className="font-medium text-foreground">{fields.length}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Total quantity</span>
                <span className="font-medium text-foreground">{totalQuantity}</span>
              </div>
            </div>

            <Button type="submit" className="w-full" disabled={isSubmitting || fields.length === 0}>
              {isSubmitting ? 'Checking out…' : `Checkout · ₹${total.toFixed(2)}`}
            </Button>
          </CardContent>
        </Card>
      </div>
    </form>
  );
}

interface CartLineRowProps {
  productName: string;
  batchNo: string;
  quantity: number;
  /** On hand in this line's batch when it was added — the most the line can be raised to. */
  availableQuantity: number;
  lineTotal: number;
  onQuantityChange: (quantity: number) => void;
  onRemove: () => void;
}

/**
 * Quantity is edited locally (its own text buffer) rather than bound straight to the form
 * field, so a keystroke that momentarily isn't a valid positive number (e.g. clearing the
 * field to retype) doesn't get instantly overwritten by the last-committed form value on
 * every render — commits happen on blur/Enter, and the +/- buttons commit immediately since
 * they always compute from the last-committed quantity. A quantity above what the batch
 * holds is refused here rather than left for checkout to reject the whole cart over.
 */
function CartLineRow({ productName, batchNo, quantity, availableQuantity, lineTotal, onQuantityChange, onRemove }: CartLineRowProps) {
  const [text, setText] = useState(String(quantity));

  useEffect(() => {
    setText(String(quantity));
  }, [quantity]);

  function commit() {
    const parsed = Number(text);
    if (Number.isFinite(parsed) && parsed > 0 && parsed <= availableQuantity) {
      onQuantityChange(parsed);
    } else {
      setText(String(quantity));
    }
  }

  return (
    <div className="flex items-start justify-between gap-2 text-sm">
      <div className="flex flex-1 flex-col gap-1">
        <span className="font-medium text-foreground">{productName}</span>
        <span className="text-xs text-muted-foreground">
          Batch {batchNo} · {availableQuantity} on hand
        </span>
        <div className="flex items-center gap-1.5">
          <Button
            type="button"
            variant="outline"
            size="icon"
            className="h-6 w-6"
            disabled={quantity <= 1}
            onClick={() => onQuantityChange(quantity - 1)}
          >
            <Minus className="h-3 w-3" />
          </Button>
          <input
            type="text"
            inputMode="decimal"
            value={text}
            onChange={(e) => setText(e.target.value)}
            onBlur={commit}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                commit();
              }
            }}
            className="h-6 w-12 rounded border border-input bg-background text-center text-xs text-foreground"
          />
          <Button
            type="button"
            variant="outline"
            size="icon"
            className="h-6 w-6"
            disabled={quantity + 1 > availableQuantity}
            onClick={() => onQuantityChange(quantity + 1)}
          >
            <Plus className="h-3 w-3" />
          </Button>
        </div>
        <button type="button" className="w-fit text-left text-xs text-muted-foreground hover:text-destructive" onClick={onRemove}>
          Remove
        </button>
      </div>
      <span className="shrink-0 font-medium text-foreground">₹{lineTotal.toFixed(2)}</span>
    </div>
  );
}
