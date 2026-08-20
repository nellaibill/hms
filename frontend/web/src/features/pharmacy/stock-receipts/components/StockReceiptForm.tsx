import { ApiError, createStockReceiptSchema, type StockReceiptFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ProductSelect } from '@/components/ProductSelect';
import { ProductBatchSelect } from '@/components/ProductBatchSelect';
import { useCreateProductBatchMutation } from '@/features/pharmacy/product-lookup';

interface NewBatchInlineFormProps {
  productId: string;
  onCreated: (batchId: string) => void;
  onCancel: () => void;
}

/**
 * Products has no batch-management screen of its own yet, so without this a real pharmacy
 * user receiving a genuinely new batch (the normal case — most stock receipts are for a
 * batch nobody has entered before) would have no way to create it through any UI in the app.
 * Kept intentionally minimal (no react-hook-form/Zod layer) since it's a small side panel;
 * relies on the backend's own validation (required fields, duplicate batch number) for
 * anything beyond the two checks below, surfaced the same way the rest of this app does.
 */
function NewBatchInlineForm({ productId, onCreated, onCancel }: NewBatchInlineFormProps) {
  const [batchNo, setBatchNo] = useState('');
  const [manufactureDate, setManufactureDate] = useState('');
  const [expiryDate, setExpiryDate] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);
  const mutation = useCreateProductBatchMutation(productId);

  function handleCreate() {
    if (!batchNo.trim()) {
      setLocalError('Batch number is required.');
      return;
    }
    if (!manufactureDate || !expiryDate) {
      setLocalError('Manufacture date and expiry date are required.');
      return;
    }
    if (expiryDate < manufactureDate) {
      setLocalError('Expiry date cannot be before the manufacture date.');
      return;
    }
    setLocalError(null);
    mutation.mutate(
      { batchNo: batchNo.trim(), manufactureDate, expiryDate, isActive: true },
      {
        onSuccess: (batch) => onCreated(batch.id),
        onError: (error) => setLocalError(error instanceof ApiError ? error.message : 'Could not create the batch.'),
      },
    );
  }

  return (
    <div className="flex flex-col gap-2 rounded-md border border-dashed border-input p-3">
      {localError && <p className="text-sm text-destructive">{localError}</p>}
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="newBatchNo">Batch number</Label>
          <Input id="newBatchNo" value={batchNo} onChange={(event) => setBatchNo(event.target.value)} />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="newBatchManufactureDate">Manufacture date</Label>
          <Input id="newBatchManufactureDate" type="date" value={manufactureDate} onChange={(event) => setManufactureDate(event.target.value)} />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="newBatchExpiryDate">Expiry date</Label>
          <Input id="newBatchExpiryDate" type="date" value={expiryDate} onChange={(event) => setExpiryDate(event.target.value)} />
        </div>
      </div>
      <div className="flex gap-2">
        <Button type="button" size="sm" onClick={handleCreate} disabled={mutation.isPending}>
          {mutation.isPending ? 'Creating…' : 'Create batch'}
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </div>
  );
}

interface StockReceiptFormProps {
  onSubmit: (values: StockReceiptFormValues) => void;
  isSubmitting: boolean;
  apiError: ApiError | null;
}

export function StockReceiptForm({ onSubmit, isSubmitting, apiError }: StockReceiptFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    watch,
    formState: { errors },
  } = useForm<StockReceiptFormValues>({
    resolver: zodResolver(createStockReceiptSchema),
    defaultValues: {
      productId: '',
      productBatchId: '',
      quantity: 0,
      remarks: '',
    },
  });

  const productId = watch('productId');

  // Server-side validation failures (docs/ApiStandards.md §5) are mapped onto the same
  // field-level display client validation uses, per docs/FrontendArchitecture.md §9.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }

    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof StockReceiptFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  // react-hook-form's handleSubmit runs Zod validation through a Promise chain even though
  // the parse itself is synchronous, so there's a real window — one to a few event-loop
  // ticks — between the first click and mutation.isPending actually flipping true and
  // disabling this button in the DOM. A fast double-click (or an impatient real user
  // clicking again because the first click didn't visibly react yet) lands inside that
  // window and fires two independent submissions — verified live: two 201s, two receipts.
  // isSubmitting alone can't close that window because it depends on a React re-render;
  // this ref is checked and set imperatively, so it closes it regardless of render timing.
  const submitLockRef = useRef(false);

  useEffect(() => {
    if (!isSubmitting) {
      submitLockRef.current = false;
    }
  }, [isSubmitting]);

  function guardedSubmit(values: StockReceiptFormValues) {
    if (submitLockRef.current) {
      return;
    }
    submitLockRef.current = true;
    onSubmit(values);
  }

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  const [showNewBatch, setShowNewBatch] = useState(false);

  function handleProductChange(newProductId: string, onChange: (value: string) => void) {
    onChange(newProductId);
    // A batch picked under the previous product is meaningless once the product changes.
    setValue('productBatchId', '');
    setShowNewBatch(false);
  }

  return (
    <form onSubmit={handleSubmit(guardedSubmit)} noValidate className="flex max-w-2xl flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="productId">Product</Label>
          <Controller
            control={control}
            name="productId"
            render={({ field }) => (
              <ProductSelect id="productId" value={field.value} onValueChange={(value) => handleProductChange(value, field.onChange)} />
            )}
          />
          {errors.productId && <p className="text-sm text-destructive">{errors.productId.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <div className="flex items-center justify-between gap-2">
            <Label htmlFor="productBatchId">Batch</Label>
            {productId && !showNewBatch && (
              <button
                type="button"
                onClick={() => setShowNewBatch(true)}
                className="text-xs font-medium text-primary underline-offset-2 hover:underline"
              >
                + New batch
              </button>
            )}
          </div>
          {showNewBatch && productId ? (
            <NewBatchInlineForm
              productId={productId}
              onCreated={(batchId) => {
                setValue('productBatchId', batchId);
                setShowNewBatch(false);
              }}
              onCancel={() => setShowNewBatch(false)}
            />
          ) : (
            <Controller
              control={control}
              name="productBatchId"
              render={({ field }) => (
                <ProductBatchSelect id="productBatchId" value={field.value} onValueChange={field.onChange} productId={productId} />
              )}
            />
          )}
          {errors.productBatchId && <p className="text-sm text-destructive">{errors.productBatchId.message}</p>}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="quantity">Quantity received</Label>
        <Input id="quantity" type="number" min="0" step="any" {...register('quantity')} />
        {errors.quantity && <p className="text-sm text-destructive">{errors.quantity.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="remarks">Remarks</Label>
        <textarea
          id="remarks"
          rows={3}
          {...register('remarks')}
          className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          placeholder="Optional notes about this receipt…"
        />
        {errors.remarks && <p className="text-sm text-destructive">{errors.remarks.message}</p>}
      </div>

      <div className="mt-2 flex gap-3">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : 'Record Receipt'}
        </Button>
      </div>
    </form>
  );
}
