import { ApiError, createDispenseCartSchema, type DispenseCartFormValues, type Patient } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Minus, Plus, ShoppingCart } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { ProductSelect } from '@/components/ProductSelect';
import { ProductBatchSelect } from '@/components/ProductBatchSelect';
import { useProductsQuery } from '@/features/pharmacy/product-lookup';
import { QuickPickPanel } from './QuickPickPanel';

interface DispenseCartFormProps {
  patient: Patient;
  onChangePatient: () => void;
  onSubmit: (values: DispenseCartFormValues) => void;
  isSubmitting: boolean;
  apiError: ApiError | null;
}

interface StagingLine {
  productId: string;
  productBatchId: string;
  quantity: number;
  remarks: string;
}

const emptyStagingLine: StagingLine = { productId: '', productBatchId: '', quantity: 1, remarks: '' };

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
    formState: { errors },
  } = useForm<DispenseCartFormValues>({
    resolver: zodResolver(createDispenseCartSchema),
    defaultValues: { patientId: patient.id, admissionId: '', lines: [] },
  });

  const { fields, append, remove, update } = useFieldArray({ control, name: 'lines' });

  const [staging, setStaging] = useState<StagingLine>(emptyStagingLine);
  const [stagingError, setStagingError] = useState<string | null>(null);

  const { data: productsData } = useProductsQuery({ pageSize: 100, isActive: true, sort: 'productName' });
  const products = productsData?.items ?? [];

  function lineTotal(line: { productId: string; quantity: number }) {
    const product = products.find((p) => p.id === line.productId);
    return product ? (Number(line.quantity) || 0) * product.sellingPrice : 0;
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

  function handleAddToCart() {
    if (!staging.productId) {
      setStagingError('Choose a product.');
      return;
    }
    if (!staging.productBatchId) {
      setStagingError('Choose a batch.');
      return;
    }
    if (!staging.quantity || staging.quantity <= 0) {
      setStagingError('Quantity must be greater than 0.');
      return;
    }
    const isDuplicate = fields.some((f) => f.productId === staging.productId && f.productBatchId === staging.productBatchId);
    if (isDuplicate) {
      setStagingError('That product/batch is already in the cart — remove it there to change the quantity.');
      return;
    }

    append({ ...staging });
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
          <QuickPickPanel
            products={products}
            selectedProductId={staging.productId}
            onSelectProduct={(productId) => setStaging((s) => ({ ...s, productId, productBatchId: '' }))}
          />

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
                  <ProductSelect
                    id="staging-product"
                    value={staging.productId}
                    onValueChange={(value) => setStaging((s) => ({ ...s, productId: value, productBatchId: '' }))}
                  />
                </div>

                <div className="flex min-w-[200px] flex-1 flex-col gap-1.5">
                  <Label htmlFor="staging-batch">Batch</Label>
                  <ProductBatchSelect
                    id="staging-batch"
                    value={staging.productBatchId}
                    onValueChange={(value) => setStaging((s) => ({ ...s, productBatchId: value }))}
                    productId={staging.productId}
                  />
                </div>

                <div className="flex w-24 flex-col gap-1.5">
                  <Label htmlFor="staging-quantity">Qty</Label>
                  <Input
                    id="staging-quantity"
                    type="number"
                    min="0"
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

          <div className="flex flex-col gap-1.5 sm:w-64">
            <Label htmlFor="admissionId">Admission reference (optional)</Label>
            <Input id="admissionId" placeholder="Admission id, if this checkout is for an inpatient" {...register('admissionId')} />
            {errors.admissionId && <p className="text-sm text-destructive">{errors.admissionId.message}</p>}
          </div>
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
              <div className="flex flex-col">
                <span className="text-sm font-medium text-foreground">
                  {patient.title} {patient.firstName} {patient.lastName}
                </span>
                <span className="text-xs text-muted-foreground">
                  {patient.uhid} · {patient.primaryPhone}
                </span>
              </div>
              <Button type="button" variant="ghost" size="sm" onClick={onChangePatient}>
                Change
              </Button>
            </div>

            <Separator />

            {fields.length === 0 ? (
              <p className="py-2 text-center text-sm text-muted-foreground">No items added yet.</p>
            ) : (
              <div className="flex flex-col gap-3">
                {fields.map((field, index) => (
                  <CartLineRow
                    key={field.id}
                    productName={products.find((p) => p.id === field.productId)?.productName ?? 'Unknown product'}
                    quantity={field.quantity}
                    lineTotal={lineTotal(field)}
                    onQuantityChange={(quantity) => update(index, { ...field, quantity })}
                    onRemove={() => remove(index)}
                  />
                ))}
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
  quantity: number;
  lineTotal: number;
  onQuantityChange: (quantity: number) => void;
  onRemove: () => void;
}

/**
 * Quantity is edited locally (its own text buffer) rather than bound straight to the form
 * field, so a keystroke that momentarily isn't a valid positive number (e.g. clearing the
 * field to retype) doesn't get instantly overwritten by the last-committed form value on
 * every render — commits happen on blur/Enter, and the +/- buttons commit immediately since
 * they always compute from the last-committed quantity.
 */
function CartLineRow({ productName, quantity, lineTotal, onQuantityChange, onRemove }: CartLineRowProps) {
  const [text, setText] = useState(String(quantity));

  useEffect(() => {
    setText(String(quantity));
  }, [quantity]);

  function commit() {
    const parsed = Number(text);
    if (Number.isFinite(parsed) && parsed > 0) {
      onQuantityChange(parsed);
    } else {
      setText(String(quantity));
    }
  }

  return (
    <div className="flex items-start justify-between gap-2 text-sm">
      <div className="flex flex-1 flex-col gap-1">
        <span className="font-medium text-foreground">{productName}</span>
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
          <Button type="button" variant="outline" size="icon" className="h-6 w-6" onClick={() => onQuantityChange(quantity + 1)}>
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
