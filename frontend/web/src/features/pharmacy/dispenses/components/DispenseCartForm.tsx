import { ApiError, createDispenseCartSchema, type DispenseCartFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, X } from 'lucide-react';
import { useEffect, useMemo, useRef } from 'react';
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ProductSelect } from '@/components/ProductSelect';
import { ProductBatchSelect } from '@/components/ProductBatchSelect';
import { useProductsQuery } from '@/features/pharmacy/product-lookup';

interface DispenseCartFormProps {
  patientId: string;
  onSubmit: (values: DispenseCartFormValues) => void;
  isSubmitting: boolean;
  apiError: ApiError | null;
}

const emptyLine = { productId: '', productBatchId: '', quantity: 0, remarks: '' };

/**
 * Cart-based dispense: several product/batch/quantity lines checked out together for one
 * patient in a single call (DispenseCreatePage's PatientPicker already handles picking the
 * patient once, at the page level). Mirrors ServiceBillingCard's useFieldArray add-row/
 * remove-row/running-total pattern — the closest existing multi-line-item UI in this codebase.
 */
export function DispenseCartForm({ patientId, onSubmit, isSubmitting, apiError }: DispenseCartFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    setValue,
    formState: { errors },
  } = useForm<DispenseCartFormValues>({
    resolver: zodResolver(createDispenseCartSchema),
    defaultValues: {
      patientId,
      admissionId: '',
      lines: [{ ...emptyLine }],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'lines' });
  const lines = useWatch({ control, name: 'lines' });

  const { data: productsData } = useProductsQuery({ pageSize: 100, isActive: true, sort: 'productName' });
  const products = productsData?.items ?? [];

  const total = (lines ?? []).reduce((sum, line) => {
    const product = products.find((p) => p.id === line.productId);
    return sum + (product ? (Number(line.quantity) || 0) * product.sellingPrice : 0);
  }, 0);

  // Client-side duplicate hint only — the backend validator is authoritative
  // (docs/ApiStandards.md §7) and rejects the same product/batch appearing twice, but showing
  // it live as the operator edits (rather than only after a failed submit) is better UX than
  // routing it through react-hook-form's less-predictable array-level error path.
  const hasDuplicateLine = useMemo(() => {
    const pairs = (lines ?? [])
      .map((l) => `${l.productId}|${l.productBatchId}`)
      .filter((pair) => pair !== '|');
    return new Set(pairs).size !== pairs.length;
  }, [lines]);

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
  // prevent a fast double-click from firing two checkouts (verified live for the single-item
  // form; the risk is the same here, just multiplied across every line in the cart).
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

  function handleProductChange(index: number, newProductId: string, onChange: (value: string) => void) {
    onChange(newProductId);
    // A batch picked under the previous product is meaningless once the product changes.
    setValue(`lines.${index}.productBatchId`, '');
  }

  return (
    <form onSubmit={handleSubmit(guardedSubmit)} noValidate className="flex max-w-3xl flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}
      {hasDuplicateLine && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          The same product/batch appears more than once in the cart — remove one.
        </p>
      )}

      <div className="flex flex-col gap-4">
        {fields.map((field, index) => {
          const rowLine = lines?.[index];
          const product = products.find((p) => p.id === rowLine?.productId);
          const lineTotal = product ? (Number(rowLine?.quantity) || 0) * product.sellingPrice : 0;
          const rowErrors = errors.lines?.[index];

          return (
            <div
              key={field.id}
              className={fields.length > 1 ? 'flex flex-col gap-3 border-b border-dashed border-border pb-4' : 'flex flex-col gap-3'}
            >
              <div className="flex flex-wrap items-start gap-3">
                <div className="flex min-w-[220px] flex-1 flex-col gap-1.5">
                  <Label htmlFor={`lines.${index}.productId`}>Product</Label>
                  <Controller
                    control={control}
                    name={`lines.${index}.productId`}
                    render={({ field: f }) => (
                      <ProductSelect
                        id={`lines.${index}.productId`}
                        value={f.value}
                        onValueChange={(value) => handleProductChange(index, value, f.onChange)}
                      />
                    )}
                  />
                  {rowErrors?.productId && <p className="text-sm text-destructive">{rowErrors.productId.message}</p>}
                </div>

                <div className="flex min-w-[200px] flex-1 flex-col gap-1.5">
                  <Label htmlFor={`lines.${index}.productBatchId`}>Batch</Label>
                  <Controller
                    control={control}
                    name={`lines.${index}.productBatchId`}
                    render={({ field: f }) => (
                      <ProductBatchSelect id={`lines.${index}.productBatchId`} value={f.value} onValueChange={f.onChange} productId={rowLine?.productId} />
                    )}
                  />
                  {rowErrors?.productBatchId && <p className="text-sm text-destructive">{rowErrors.productBatchId.message}</p>}
                </div>

                <div className="flex w-28 flex-col gap-1.5">
                  <Label htmlFor={`lines.${index}.quantity`}>Quantity</Label>
                  <Input id={`lines.${index}.quantity`} type="number" min="0" step="any" {...register(`lines.${index}.quantity`)} />
                  {rowErrors?.quantity && <p className="text-sm text-destructive">{rowErrors.quantity.message}</p>}
                </div>

                {fields.length > 1 && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    aria-label="Remove this item"
                    className="mt-6 shrink-0"
                    onClick={() => remove(index)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                )}
              </div>

              <div className="flex flex-wrap items-end gap-4">
                <div className="flex min-w-[220px] flex-1 flex-col gap-1.5">
                  <Label htmlFor={`lines.${index}.remarks`}>Remarks (optional)</Label>
                  <Input id={`lines.${index}.remarks`} placeholder="Optional notes about this item…" {...register(`lines.${index}.remarks`)} />
                </div>
                <p className="pb-2 text-sm text-muted-foreground">{product ? `₹${lineTotal.toFixed(2)}` : '—'}</p>
              </div>
            </div>
          );
        })}
      </div>

      <Button type="button" variant="outline" size="sm" className="w-fit gap-1.5" onClick={() => append({ ...emptyLine })}>
        <Plus className="h-4 w-4" />
        Add another item
      </Button>

      <div className="flex flex-col gap-1.5 sm:w-64">
        <Label htmlFor="admissionId">Admission reference (optional)</Label>
        <Input id="admissionId" placeholder="Admission id, if this checkout is for an inpatient" {...register('admissionId')} />
        {errors.admissionId && <p className="text-sm text-destructive">{errors.admissionId.message}</p>}
      </div>

      <div className="mt-2 flex items-center justify-between gap-3 rounded-md border border-border bg-muted/30 px-4 py-3">
        <span className="text-sm font-medium text-foreground">
          {fields.length} item{fields.length === 1 ? '' : 's'} · Total
        </span>
        <span className="text-lg font-semibold text-foreground">₹{total.toFixed(2)}</span>
      </div>

      <div className="flex gap-3">
        <Button type="submit" disabled={isSubmitting || hasDuplicateLine}>
          {isSubmitting ? 'Checking out…' : 'Checkout'}
        </Button>
      </div>
    </form>
  );
}
