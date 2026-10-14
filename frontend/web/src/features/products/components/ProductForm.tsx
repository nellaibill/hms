import {
  ApiError,
  DOSAGE_FORMS,
  DRUG_SCHEDULES,
  STORAGE_CONDITIONS,
  productProfileSchema,
  type ProductProfileFormValues,
} from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { getDisplayLabel, getMasterConfig, useMasterOptionsQuery } from '@/features/masters';
import { DRUG_SCHEDULE_LABELS, STORAGE_CONDITION_LABELS } from '../drugProfile';

interface ProductFormProps {
  defaultValues?: Partial<ProductProfileFormValues>;
  mode: 'create' | 'edit' | 'view';
  onSubmit: (values: ProductProfileFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

interface TextFieldProps {
  name: keyof ProductProfileFormValues;
  label: string;
  register: UseFormRegister<ProductProfileFormValues>;
  errors: FieldErrors<ProductProfileFormValues>;
  readOnly?: boolean;
  type?: 'text' | 'number';
  step?: string;
  required?: boolean;
}

function TextField({ name, label, register, errors, readOnly, type = 'text', step, required }: TextFieldProps) {
  const error = errors[name];
  const inputId = `product-field-${name}`;
  return (
    <div className="flex min-w-[200px] flex-1 flex-col gap-1">
      <Label htmlFor={inputId}>
        {label}
        {required && <span className="text-destructive"> *</span>}
      </Label>
      <Input
        id={inputId}
        type={type}
        step={step}
        disabled={readOnly}
        {...register(name, type === 'number' ? { valueAsNumber: true } : undefined)}
      />
      {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
    </div>
  );
}

interface ReferenceSelectProps {
  name: keyof ProductProfileFormValues;
  label: string;
  entityKey: string;
  control: Control<ProductProfileFormValues>;
  errors: FieldErrors<ProductProfileFormValues>;
  readOnly?: boolean;
}

function ReferenceSelect({ name, label, entityKey, control, errors, readOnly }: ReferenceSelectProps) {
  const optionsQuery = useMasterOptionsQuery(entityKey);
  const config = getMasterConfig(entityKey);
  const options = (optionsQuery.data ?? [])
    .filter((record) => record.isActive)
    .map((record) => ({ value: record.id, label: config ? getDisplayLabel(config, record) : record.id }));
  const error = errors[name];
  const inputId = `product-field-${name}`;

  return (
    <div className="flex min-w-[200px] flex-1 flex-col gap-1">
      <Label htmlFor={inputId}>
        {label}
        <span className="text-destructive"> *</span>
      </Label>
      <Controller
        name={name}
        control={control}
        rules={{ required: `${label} is required.` }}
        render={({ field }) => (
          <Select value={String(field.value ?? '')} onValueChange={field.onChange} disabled={readOnly}>
            <SelectTrigger id={inputId} aria-label={label}>
              <SelectValue placeholder={`Select ${label.toLowerCase()}…`} />
            </SelectTrigger>
            <SelectContent>
              {options.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      />
      {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
    </div>
  );
}

/** Radix Select can't hold an empty-string item value, so the "nothing chosen" option is
 * carried as this sentinel in the control and translated back to '' in the form. */
const NONE_VALUE = '__none__';

interface EnumSelectProps {
  name: keyof ProductProfileFormValues;
  label: string;
  options: { value: string; label: string }[];
  control: Control<ProductProfileFormValues>;
  errors: FieldErrors<ProductProfileFormValues>;
  readOnly?: boolean;
}

/** A select over a fixed set of values (an enum), as opposed to ReferenceSelect's Masters records. */
function EnumSelect({ name, label, options, control, errors, readOnly }: EnumSelectProps) {
  const error = errors[name];
  const inputId = `product-field-${name}`;

  return (
    <div className="flex min-w-[200px] flex-1 flex-col gap-1">
      <Label htmlFor={inputId}>{label}</Label>
      <Controller
        name={name}
        control={control}
        render={({ field }) => (
          <Select
            value={String(field.value ?? '') || NONE_VALUE}
            onValueChange={(value) => field.onChange(value === NONE_VALUE ? '' : value)}
            disabled={readOnly}
          >
            <SelectTrigger id={inputId} aria-label={label}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {options.map((option) => (
                <SelectItem key={option.value || NONE_VALUE} value={option.value || NONE_VALUE}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      />
      {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
    </div>
  );
}

export function ProductForm({ defaultValues, mode, onSubmit, isSubmitting, submitLabel, apiError }: ProductFormProps) {
  const readOnly = mode === 'view';
  const codeReadOnly = readOnly || mode === 'edit';

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ProductProfileFormValues>({
    resolver: zodResolver(productProfileSchema),
    defaultValues: {
      sku: '',
      productCode: '',
      productName: '',
      genericName: '',
      description: '',
      brandId: '',
      manufacturerId: '',
      categoryId: '',
      subCategoryId: '',
      groupId: '',
      uomId: '',
      baseUomId: '',
      isBatchTracked: false,
      isSerialized: false,
      isActive: true,
      reorderLevel: 0,
      minStockLevel: 0,
      maxStockLevel: 0,
      mrp: 0,
      costPrice: 0,
      sellingPrice: 0,
      hsnCode: '',
      weight: undefined,
      volume: undefined,
      dosageForm: '',
      strength: '',
      unitsPerPack: 1,
      schedule: 'None',
      storageCondition: 'RoomTemperature',
      rackLocation: '',
      gstRatePercent: undefined,
      ...defaultValues,
    },
  });

  // Server-side validation failures are mapped onto the same field-level display client
  // validation uses, mirroring UserForm.tsx's pattern.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }
    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof ProductProfileFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="mx-auto flex w-full max-w-4xl flex-col gap-5">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Identity</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <TextField name="sku" label="SKU" register={register} errors={errors} readOnly={codeReadOnly} required />
          <TextField name="productCode" label="Product Code" register={register} errors={errors} readOnly={codeReadOnly} required />
          <TextField name="productName" label="Product Name" register={register} errors={errors} readOnly={readOnly} required />
          <TextField name="genericName" label="Generic Name" register={register} errors={errors} readOnly={readOnly} />
          <TextField name="description" label="Description" register={register} errors={errors} readOnly={readOnly} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Classification</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <ReferenceSelect name="brandId" label="Brand" entityKey="brand" control={control} errors={errors} readOnly={readOnly} />
          <ReferenceSelect
            name="manufacturerId"
            label="Manufacturer"
            entityKey="manufacturer"
            control={control}
            errors={errors}
            readOnly={readOnly}
          />
          <ReferenceSelect
            name="categoryId"
            label="Category"
            entityKey="productCategory"
            control={control}
            errors={errors}
            readOnly={readOnly}
          />
          <ReferenceSelect
            name="subCategoryId"
            label="Sub-Category"
            entityKey="productSubCategory"
            control={control}
            errors={errors}
            readOnly={readOnly}
          />
          <ReferenceSelect name="groupId" label="Group" entityKey="productGroup" control={control} errors={errors} readOnly={readOnly} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Units &amp; Stock</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-4">
            <ReferenceSelect
              name="uomId"
              label="Selling Unit"
              entityKey="unitOfMeasure"
              control={control}
              errors={errors}
              readOnly={readOnly}
            />
            <ReferenceSelect
              name="baseUomId"
              label="Base Unit"
              entityKey="unitOfMeasure"
              control={control}
              errors={errors}
              readOnly={readOnly}
            />
            <TextField name="reorderLevel" label="Reorder Level" type="number" step="any" register={register} errors={errors} readOnly={readOnly} />
            <TextField name="minStockLevel" label="Min Stock Level" type="number" step="any" register={register} errors={errors} readOnly={readOnly} />
            <TextField name="maxStockLevel" label="Max Stock Level" type="number" step="any" register={register} errors={errors} readOnly={readOnly} />
          </div>
          <div className="flex flex-wrap gap-6">
            <div className="flex flex-col gap-1">
              <span className="text-sm font-medium leading-none text-foreground">Batch Tracked</span>
              <Controller
                name="isBatchTracked"
                control={control}
                render={({ field }) => (
                  <div className="flex h-10 items-center">
                    <Switch checked={Boolean(field.value)} onCheckedChange={field.onChange} disabled={readOnly} aria-label="Batch Tracked" />
                  </div>
                )}
              />
            </div>
            <div className="flex flex-col gap-1">
              <span className="text-sm font-medium leading-none text-foreground">Serialized</span>
              <Controller
                name="isSerialized"
                control={control}
                render={({ field }) => (
                  <div className="flex h-10 items-center">
                    <Switch checked={Boolean(field.value)} onCheckedChange={field.onChange} disabled={readOnly} aria-label="Serialized" />
                  </div>
                )}
              />
            </div>
            <div className="flex flex-col gap-1">
              <span className="text-sm font-medium leading-none text-foreground">Status</span>
              <Controller
                name="isActive"
                control={control}
                render={({ field }) => (
                  <div className="flex h-10 items-center gap-2">
                    <Switch checked={Boolean(field.value)} onCheckedChange={field.onChange} disabled={readOnly} aria-label="Active" />
                    <span className="text-sm text-muted-foreground">{field.value ? 'Active' : 'Inactive'}</span>
                  </div>
                )}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Drug Details</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-4">
            <EnumSelect
              name="dosageForm"
              label="Dosage Form"
              options={[{ value: '', label: 'Not a drug' }, ...DOSAGE_FORMS.map((form) => ({ value: form, label: form }))]}
              control={control}
              errors={errors}
              readOnly={readOnly}
            />
            <TextField name="strength" label="Strength" register={register} errors={errors} readOnly={readOnly} />
            <EnumSelect
              name="schedule"
              label="Schedule"
              options={DRUG_SCHEDULES.map((schedule) => ({ value: schedule, label: DRUG_SCHEDULE_LABELS[schedule] }))}
              control={control}
              errors={errors}
              readOnly={readOnly}
            />
            <EnumSelect
              name="storageCondition"
              label="Storage"
              options={STORAGE_CONDITIONS.map((condition) => ({ value: condition, label: STORAGE_CONDITION_LABELS[condition] }))}
              control={control}
              errors={errors}
              readOnly={readOnly}
            />
            <TextField name="rackLocation" label="Rack Location" register={register} errors={errors} readOnly={readOnly} />
            <TextField name="unitsPerPack" label="Units per Pack" type="number" step="any" register={register} errors={errors} readOnly={readOnly} required />
            <TextField name="gstRatePercent" label="GST Rate (%)" type="number" step="any" register={register} errors={errors} readOnly={readOnly} />
          </div>
          <p className="text-sm text-muted-foreground">
            Units per pack is how many stock units make one pack as bought and priced — 10 for a strip of ten tablets, 1 for a bottle. Stock is
            counted in units; MRP and rates are per pack.
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Pricing</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-4">
          <TextField name="mrp" label="MRP" type="number" step="any" register={register} errors={errors} readOnly={readOnly} required />
          <TextField name="costPrice" label="Cost Price" type="number" step="any" register={register} errors={errors} readOnly={readOnly} required />
          <TextField
            name="sellingPrice"
            label="Selling Price"
            type="number"
            step="any"
            register={register}
            errors={errors}
            readOnly={readOnly}
            required
          />
          <TextField name="hsnCode" label="HSN Code" register={register} errors={errors} readOnly={readOnly} />
          <TextField name="weight" label="Weight" type="number" step="any" register={register} errors={errors} readOnly={readOnly} />
          <TextField name="volume" label="Volume" type="number" step="any" register={register} errors={errors} readOnly={readOnly} />
        </CardContent>
      </Card>

      {mode !== 'view' && (
        <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : submitLabel}
          </Button>
        </div>
      )}
    </form>
  );
}
