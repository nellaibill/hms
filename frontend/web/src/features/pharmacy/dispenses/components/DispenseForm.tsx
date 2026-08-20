import { ApiError, createDispenseSchema, type DispenseFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ProductSelect } from '@/components/ProductSelect';
import { ProductBatchSelect } from '@/components/ProductBatchSelect';

interface DispenseFormProps {
  patientId: string;
  onSubmit: (values: DispenseFormValues) => void;
  isSubmitting: boolean;
  apiError: ApiError | null;
}

export function DispenseForm({ patientId, onSubmit, isSubmitting, apiError }: DispenseFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    watch,
    formState: { errors },
  } = useForm<DispenseFormValues>({
    resolver: zodResolver(createDispenseSchema),
    defaultValues: {
      productId: '',
      productBatchId: '',
      patientId,
      admissionId: '',
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
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof DispenseFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  // Surfaces the real backend error message directly (409s: expired batch / insufficient
  // stock / invalid references) rather than swallowing it into a generic message — the
  // backend's message is already human-readable (docs/ApiStandards.md §5).
  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  // See StockReceiptForm's identical guard for the full explanation: isSubmitting alone
  // can't prevent a fast double-click from firing two dispenses (verified live), because
  // react-hook-form's async validation leaves a window before mutation.isPending actually
  // disables this button in the DOM. This ref closes that window imperatively. Doubly
  // important here — a duplicate dispense doesn't just corrupt a balance, it means a
  // patient's record shows twice the medication actually issued.
  const submitLockRef = useRef(false);

  useEffect(() => {
    if (!isSubmitting) {
      submitLockRef.current = false;
    }
  }, [isSubmitting]);

  function guardedSubmit(values: DispenseFormValues) {
    if (submitLockRef.current) {
      return;
    }
    submitLockRef.current = true;
    onSubmit(values);
  }

  function handleProductChange(newProductId: string, onChange: (value: string) => void) {
    onChange(newProductId);
    // A batch picked under the previous product is meaningless once the product changes.
    setValue('productBatchId', '');
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
          <Label htmlFor="productBatchId">Batch</Label>
          <Controller
            control={control}
            name="productBatchId"
            render={({ field }) => (
              <ProductBatchSelect id="productBatchId" value={field.value} onValueChange={field.onChange} productId={productId} />
            )}
          />
          {errors.productBatchId && <p className="text-sm text-destructive">{errors.productBatchId.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="quantity">Quantity to dispense</Label>
          <Input id="quantity" type="number" min="0" step="any" {...register('quantity')} />
          {errors.quantity && <p className="text-sm text-destructive">{errors.quantity.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="admissionId">Admission reference (optional)</Label>
          <Input id="admissionId" placeholder="Admission id, if this dispense is for an inpatient" {...register('admissionId')} />
          {errors.admissionId && <p className="text-sm text-destructive">{errors.admissionId.message}</p>}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="remarks">Remarks</Label>
        <textarea
          id="remarks"
          rows={3}
          {...register('remarks')}
          className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
          placeholder="Optional notes about this dispense…"
        />
        {errors.remarks && <p className="text-sm text-destructive">{errors.remarks.message}</p>}
      </div>

      <div className="mt-2 flex gap-3">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : 'Dispense Stock'}
        </Button>
      </div>
    </form>
  );
}
