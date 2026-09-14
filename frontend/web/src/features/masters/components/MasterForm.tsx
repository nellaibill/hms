import type { ApiError } from '@hms/shared';
import { Info } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Controller, useForm, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { cn } from '@/lib/utils';
import { useMasterOptionsQuery } from '../hooks/useMasterQuery';
import { getDisplayLabel, getMasterConfig } from '../engine/registry';
import { MasterPhotoUpload } from './MasterPhotoUpload';
import type { MasterEntityConfig, MasterFieldDef, MasterFieldGroup, MasterInfoBox, MasterRecord } from '../engine/types';

const WEEKDAYS = [
  { value: 'Monday', label: 'Mon' },
  { value: 'Tuesday', label: 'Tue' },
  { value: 'Wednesday', label: 'Wed' },
  { value: 'Thursday', label: 'Thu' },
  { value: 'Friday', label: 'Fri' },
  { value: 'Saturday', label: 'Sat' },
  { value: 'Sunday', label: 'Sun' },
];

/** Small colored callout used both beside a 'radio-card' field's options and at the bottom of a
 * MasterFieldGroup's card — see MasterInfoBox. */
function InfoCallout({ box }: { box: MasterInfoBox }) {
  return (
    <div className="flex gap-2 rounded-md border border-blue-200 bg-blue-50 px-3 py-2.5 text-sm dark:border-blue-900 dark:bg-blue-950">
      <Info className="mt-0.5 h-4 w-4 shrink-0 text-blue-600 dark:text-blue-400" />
      <p className="text-blue-900 dark:text-blue-200">
        {box.title && <span className="font-semibold">{box.title}: </span>}
        {box.text}
      </p>
    </div>
  );
}

/** Radix Select can't represent "no selection" with an empty string, so optional reference/select fields use this sentinel internally. */
const NONE_VALUE = '__none__';

/** Renders two adjacent 'time' MasterFieldDefs (the first with `rangeLabel` set) as one
 * connected "label + start + to + end" control — see MasterFieldDef.rangeLabel's own doc
 * comment. Each field still registers/submits independently; this is presentation-only. */
function TimeRangeControl({
  startField,
  endField,
  register,
  errors,
  readOnly,
}: {
  startField: MasterFieldDef;
  endField: MasterFieldDef;
  register: UseFormRegister<Record<string, unknown>>;
  errors: FieldErrors<Record<string, unknown>>;
  readOnly: boolean;
}) {
  const startError = errors[startField.key];
  const endError = errors[endField.key];
  const required = startField.required || endField.required;

  return (
    <div className="flex min-w-[280px] flex-1 flex-col gap-1">
      <label className="text-sm font-medium leading-none text-foreground">
        {startField.rangeLabel}
        {required && <span className="text-destructive"> *</span>}
      </label>
      <div className="flex items-center gap-2">
        <Input
          type="time"
          step="1"
          disabled={readOnly}
          aria-label={startField.label}
          {...register(startField.key, { required: startField.required ? `${startField.label} is required.` : false })}
        />
        <span className="text-sm text-muted-foreground">to</span>
        <Input
          type="time"
          step="1"
          disabled={readOnly}
          aria-label={endField.label}
          {...register(endField.key, { required: endField.required ? `${endField.label} is required.` : false })}
        />
      </div>
      {startField.helpText && <p className="text-xs text-muted-foreground">{startField.helpText}</p>}
      {startError && <p className="text-sm text-destructive">{String(startError.message)}</p>}
      {endError && <p className="text-sm text-destructive">{String(endError.message)}</p>}
    </div>
  );
}

interface MasterFormProps {
  config: MasterEntityConfig;
  mode: 'create' | 'edit' | 'view';
  recordId?: string;
  defaultValues: Record<string, unknown>;
  isSubmitting?: boolean;
  apiError?: ApiError | null;
  /** photoFile is only ever passed in create mode, when config.photo is set and a file was
   * staged (see MasterPhotoUpload's own doc comment) — the caller uploads it once the create
   * call this came from actually succeeds and a real record id exists. */
  onSubmit: (values: Record<string, unknown>, photoFile?: File) => void;
  onCancel: () => void;
}

function toFormDefaults(config: MasterEntityConfig, values: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = { ...values };
  for (const field of config.fields) {
    const isEmpty = result[field.key] === undefined || result[field.key] === null || result[field.key] === '';
    if (isEmpty && field.defaultValue !== undefined) {
      result[field.key] = field.defaultValue;
    }
    if ((field.type === 'reference' || field.type === 'select') && (result[field.key] === undefined || result[field.key] === null || result[field.key] === '')) {
      result[field.key] = NONE_VALUE;
    }
    if (
      (field.type === 'day-checkboxes' || field.type === 'reference-checkboxes' || field.type === 'reference-checkboxes-amount') &&
      !Array.isArray(result[field.key])
    ) {
      result[field.key] = [];
    }
  }
  if (result.isActive === undefined) result.isActive = true;
  return result;
}

function fromFormValues(config: MasterEntityConfig, values: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = { ...values };
  for (const field of config.fields) {
    if ((field.type === 'reference' || field.type === 'select') && result[field.key] === NONE_VALUE) {
      result[field.key] = undefined;
    }
    if ((field.type === 'number' || field.type === 'decimal') && typeof result[field.key] === 'string') {
      result[field.key] = result[field.key] === '' ? undefined : Number(result[field.key]);
    }
  }
  return result;
}

interface FieldControlProps {
  field: MasterFieldDef;
  register: UseFormRegister<Record<string, unknown>>;
  control: Control<Record<string, unknown>>;
  errors: FieldErrors<Record<string, unknown>>;
  readOnly: boolean;
  recordId?: string;
  scopeValue?: unknown;
  /** Extra field-level validate rule — used for the unique-code field. */
  validate?: (value: unknown) => string | true;
}

function FieldControl({ field, register, control, errors, readOnly, recordId, scopeValue, validate }: FieldControlProps) {
  const isReferenceLike = field.type === 'reference' || field.type === 'reference-checkboxes' || field.type === 'reference-checkboxes-amount';
  const referenceOptions = useMasterOptionsQuery(isReferenceLike ? field.referenceEntityKey : undefined);
  const referenceConfig = isReferenceLike ? getMasterConfig(field.referenceEntityKey) : undefined;
  const error = errors[field.key];
  const inputId = `master-field-${field.key}`;

  if (field.type === 'textarea') {
    return (
      <div className="flex w-full flex-col gap-1">
        <label htmlFor={inputId} className="text-sm font-medium leading-none text-foreground">
          {field.label}
        </label>
        <textarea
          id={inputId}
          disabled={readOnly}
          rows={2}
          placeholder={field.placeholder}
          className="flex w-full resize-none rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:cursor-not-allowed disabled:opacity-50"
          {...register(field.key)}
        />
        {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
      </div>
    );
  }

  if (field.type === 'boolean') {
    return (
      <div className="flex flex-col gap-1">
        <span className="text-sm font-medium leading-none text-foreground">{field.label}</span>
        <Controller
          name={field.key}
          control={control}
          render={({ field: controllerField }) => (
            <div className="flex h-10 items-center">
              <Switch
                checked={Boolean(controllerField.value)}
                onCheckedChange={controllerField.onChange}
                disabled={readOnly}
                aria-label={field.label}
              />
            </div>
          )}
        />
        {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
      </div>
    );
  }

  if (field.type === 'select' || field.type === 'reference') {
    const items =
      field.type === 'select'
        ? (field.options ?? []).map((option) => ({ value: option.value, label: option.label }))
        : (referenceOptions.data ?? [])
            .filter((option) => !field.excludeSelf || option.id !== recordId)
            .filter(
              (option) =>
                !field.referenceScopeField ||
                scopeValue === undefined ||
                scopeValue === NONE_VALUE ||
                option[field.referenceScopeField] === scopeValue,
            )
            .map((option) => ({ value: option.id, label: referenceConfig ? getDisplayLabel(referenceConfig, option) : option.id }));

    return (
      <div className="flex min-w-[200px] flex-1 flex-col gap-1">
        <label htmlFor={inputId} className="text-sm font-medium leading-none text-foreground">
          {field.label}
          {field.required && <span className="text-destructive"> *</span>}
        </label>
        <Controller
          name={field.key}
          control={control}
          rules={field.required ? { validate: (value) => value !== NONE_VALUE || `${field.label} is required.` } : undefined}
          render={({ field: controllerField }) => (
            <Select value={String(controllerField.value ?? NONE_VALUE)} onValueChange={controllerField.onChange} disabled={readOnly}>
              <SelectTrigger id={inputId} aria-label={field.label}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {!field.required && <SelectItem value={NONE_VALUE}>— None —</SelectItem>}
                {items.map((item) => (
                  <SelectItem key={item.value} value={item.value}>
                    {item.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
        {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
        {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
      </div>
    );
  }

  if (field.type === 'time') {
    return (
      <div className="flex min-w-[160px] flex-1 flex-col gap-1">
        <label htmlFor={inputId} className="text-sm font-medium leading-none text-foreground">
          {field.label}
          {field.required && <span className="text-destructive"> *</span>}
        </label>
        <Input
          id={inputId}
          type="time"
          step="1"
          disabled={readOnly}
          {...register(field.key, { required: field.required ? `${field.label} is required.` : false })}
        />
        {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
        {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
      </div>
    );
  }

  if (field.type === 'radio-card') {
    return (
      <div className="flex w-full flex-col gap-3 sm:flex-row">
        <Controller
          name={field.key}
          control={control}
          rules={field.required ? { required: `${field.label} is required.` } : undefined}
          render={({ field: controllerField }) => (
            <div className="grid flex-1 grid-cols-1 gap-3 sm:grid-cols-2">
              {(field.options ?? []).map((option) => {
                const isSelected = controllerField.value === option.value;
                const Icon = option.icon;
                return (
                  <button
                    key={option.value}
                    type="button"
                    disabled={readOnly}
                    onClick={() => controllerField.onChange(option.value)}
                    className={cn(
                      'flex items-start gap-3 rounded-lg border p-4 text-left transition-colors disabled:cursor-not-allowed disabled:opacity-50',
                      isSelected ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'border-input hover:bg-accent/50',
                    )}
                  >
                    <span
                      className={cn(
                        'mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center rounded-full border',
                        isSelected ? 'border-primary' : 'border-muted-foreground',
                      )}
                    >
                      {isSelected && <span className="h-2 w-2 rounded-full bg-primary" />}
                    </span>
                    {Icon && (
                      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-muted text-muted-foreground">
                        <Icon className="h-4 w-4" />
                      </span>
                    )}
                    <span className="flex flex-col gap-0.5">
                      <span className="text-sm font-semibold text-foreground">{option.label}</span>
                      {option.description && <span className="text-xs text-muted-foreground">{option.description}</span>}
                    </span>
                  </button>
                );
              })}
            </div>
          )}
        />
        {field.infoBox && (
          <div className="sm:w-64 sm:shrink-0">
            <InfoCallout box={field.infoBox} />
          </div>
        )}
        {error && <p className="w-full text-sm text-destructive">{String(error.message)}</p>}
      </div>
    );
  }

  if (field.type === 'day-checkboxes') {
    return (
      <div className="flex min-w-[260px] flex-1 flex-col gap-1">
        <label className="text-sm font-medium leading-none text-foreground">
          {field.label}
          {field.required && <span className="text-destructive"> *</span>}
        </label>
        <Controller
          name={field.key}
          control={control}
          rules={field.required ? { validate: (value) => (Array.isArray(value) && value.length > 0) || `Select at least one ${field.label.toLowerCase()}.` } : undefined}
          render={({ field: controllerField }) => {
            const selected: string[] = Array.isArray(controllerField.value) ? controllerField.value : [];
            function toggle(day: string) {
              controllerField.onChange(selected.includes(day) ? selected.filter((d) => d !== day) : [...selected, day]);
            }
            return (
              <div className="flex flex-wrap gap-4">
                {WEEKDAYS.map((day) => (
                  <label key={day.value} className="flex items-center gap-1.5 text-sm text-foreground">
                    <input
                      type="checkbox"
                      className="h-4 w-4 rounded border-input"
                      checked={selected.includes(day.value)}
                      disabled={readOnly}
                      onChange={() => toggle(day.value)}
                    />
                    {day.label}
                  </label>
                ))}
              </div>
            );
          }}
        />
        {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
        {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
      </div>
    );
  }

  if (field.type === 'reference-checkboxes') {
    // Only active options are offered going forward, per this field type's own doc comment —
    // but a record already mapped to one that's since been made inactive still shows it
    // (checked, disabled) rather than silently hiding a mapping that's still saved.
    const items = (referenceOptions.data ?? [])
      .filter((option) => !field.referenceActiveOnly || option.isActive)
      .map((option) => ({ id: option.id, label: referenceConfig ? getDisplayLabel(referenceConfig, option) : option.id }));

    return (
      <div className="flex w-full flex-col gap-2">
        <label className="text-sm font-medium leading-none text-foreground">
          {field.label}
          {field.required && <span className="text-destructive"> *</span>}
        </label>
        {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
        <Controller
          name={field.key}
          control={control}
          rules={field.required ? { validate: (value) => (Array.isArray(value) && value.length > 0) || `Select at least one ${field.label.toLowerCase()}.` } : undefined}
          render={({ field: controllerField }) => {
            const selected: string[] = Array.isArray(controllerField.value) ? controllerField.value : [];
            function toggle(id: string) {
              controllerField.onChange(selected.includes(id) ? selected.filter((existing) => existing !== id) : [...selected, id]);
            }
            return (
              <div className="grid grid-cols-1 gap-2 rounded-md border border-border p-3 sm:grid-cols-2">
                {items.map((item) => (
                  <label
                    key={item.id}
                    className="flex items-center gap-2 rounded-md border border-transparent px-2 py-1.5 text-sm text-foreground hover:border-input"
                  >
                    <input
                      type="checkbox"
                      className="h-4 w-4 rounded border-input"
                      checked={selected.includes(item.id)}
                      disabled={readOnly}
                      onChange={() => toggle(item.id)}
                    />
                    {item.label}
                  </label>
                ))}
              </div>
            );
          }}
        />
        {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
      </div>
    );
  }

  if (field.type === 'reference-checkboxes-amount') {
    const idKey = field.arrayItemKeys?.id ?? 'id';
    const amountKey = field.arrayItemKeys?.amount ?? 'amount';
    const items = (referenceOptions.data ?? [])
      .filter((option) => !field.referenceActiveOnly || option.isActive)
      .map((option) => ({
        id: option.id,
        label: referenceConfig ? getDisplayLabel(referenceConfig, option) : option.id,
        referenceAmount: field.referenceAmountField ? (option[field.referenceAmountField] as number | null | undefined) : undefined,
      }));

    return (
      <div className="flex w-full flex-col gap-2">
        <label className="text-sm font-medium leading-none text-foreground">
          {field.label}
          {field.required && <span className="text-destructive"> *</span>}
        </label>
        {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
        <Controller
          name={field.key}
          control={control}
          rules={field.required ? { validate: (value) => (Array.isArray(value) && value.length > 0) || `Select at least one ${field.label.toLowerCase()}.` } : undefined}
          render={({ field: controllerField }) => {
            const selected: Record<string, unknown>[] = Array.isArray(controllerField.value) ? controllerField.value : [];
            const findEntry = (id: string) => selected.find((entry) => entry[idKey] === id);
            function toggle(id: string) {
              controllerField.onChange(findEntry(id) ? selected.filter((entry) => entry[idKey] !== id) : [...selected, { [idKey]: id, [amountKey]: null }]);
            }
            function setAmount(id: string, raw: string) {
              const parsed = raw === '' ? null : Number(raw);
              controllerField.onChange(selected.map((entry) => (entry[idKey] === id ? { ...entry, [amountKey]: parsed } : entry)));
            }
            return (
              <div className="flex flex-col gap-2 rounded-md border border-border p-3">
                {items.map((item) => {
                  const entry = findEntry(item.id);
                  const checked = Boolean(entry);
                  const consultantCharge = entry ? (entry[amountKey] as number | null | undefined) : undefined;
                  const hasHospitalCharge = item.referenceAmount !== null && item.referenceAmount !== undefined;
                  const hospitalCharge = hasHospitalCharge ? Number(item.referenceAmount) : undefined;
                  const income = hospitalCharge !== undefined && consultantCharge != null ? hospitalCharge - Number(consultantCharge) : undefined;
                  const incomePercent = income !== undefined && hospitalCharge ? (income / hospitalCharge) * 100 : undefined;

                  return (
                    <div key={item.id} className="rounded-md border border-transparent px-2 py-1.5 has-[:checked]:border-input">
                      <label className="flex items-center gap-2 text-sm text-foreground">
                        <input
                          type="checkbox"
                          className="h-4 w-4 rounded border-input"
                          checked={checked}
                          disabled={readOnly}
                          onChange={() => toggle(item.id)}
                        />
                        {item.label}
                      </label>
                      {checked && (
                        <div className="mt-2 grid grid-cols-1 gap-3 pl-6 sm:grid-cols-3">
                          <div className="flex flex-col gap-1">
                            <span className="text-[11px] text-muted-foreground">Consultant charge (₹)</span>
                            <Input
                              type="number"
                              min={0}
                              step="any"
                              disabled={readOnly}
                              value={consultantCharge ?? ''}
                              onChange={(e) => setAmount(item.id, e.target.value)}
                              className="h-8 text-sm"
                            />
                          </div>
                          <div className="flex flex-col gap-1">
                            <span className="text-[11px] text-muted-foreground">Hospital charge (₹)</span>
                            <div className="flex h-8 items-center text-sm text-muted-foreground">
                              {hospitalCharge !== undefined ? `₹${hospitalCharge.toLocaleString('en-IN')}` : '—'}
                            </div>
                          </div>
                          <div className="flex flex-col gap-1">
                            <span className="text-[11px] text-muted-foreground">Hospital income</span>
                            <div className="flex h-8 items-center text-sm font-medium text-foreground">
                              {income !== undefined
                                ? `₹${income.toLocaleString('en-IN')}${incomePercent !== undefined ? ` (${incomePercent.toFixed(0)}%)` : ''}`
                                : '—'}
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            );
          }}
        />
        {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
      </div>
    );
  }

  // text | number | decimal
  const isNumeric = field.type === 'number' || field.type === 'decimal';
  return (
    <div className="flex min-w-[200px] flex-1 flex-col gap-1">
      <label htmlFor={inputId} className="text-sm font-medium leading-none text-foreground">
        {field.label}
        {field.required && <span className="text-destructive"> *</span>}
      </label>
      <Input
        id={inputId}
        type={isNumeric ? 'number' : 'text'}
        step={field.step ?? (field.type === 'decimal' ? 'any' : 1)}
        min={field.min}
        max={field.max}
        disabled={readOnly}
        placeholder={field.placeholder}
        {...register(field.key, {
          required: field.required ? `${field.label} is required.` : false,
          valueAsNumber: isNumeric,
          validate,
          min: field.min !== undefined ? { value: field.min, message: `${field.label} must be at least ${field.min}.` } : undefined,
          max: field.max !== undefined ? { value: field.max, message: `${field.label} must be at most ${field.max}.` } : undefined,
        })}
      />
      {field.helpText && <p className="text-xs text-muted-foreground">{field.helpText}</p>}
      {error && <p className="text-sm text-destructive">{String(error.message)}</p>}
    </div>
  );
}

export function MasterForm({ config, mode, recordId, defaultValues, isSubmitting, apiError, onSubmit, onCancel }: MasterFormProps) {
  const readOnly = mode === 'view';
  const existingRecordsQuery = useMasterOptionsQuery(config.key);
  const existingRecords: MasterRecord[] = existingRecordsQuery.data ?? [];
  const [crossFieldError, setCrossFieldError] = useState<string | undefined>(undefined);
  // Create mode only — see MasterPhotoUpload's own doc comment for why a file can't just
  // upload itself immediately when there's no record id yet to attach it to.
  const [stagedPhotoFile, setStagedPhotoFile] = useState<File | null>(null);

  const {
    register,
    control,
    handleSubmit,
    watch,
    setError,
    formState: { errors },
  } = useForm<Record<string, unknown>>({ defaultValues: toFormDefaults(config, defaultValues) });

  // Server-side validation failures (backend Update*Request DTOs are authoritative — client
  // validation here is convenience only) are mapped onto the same field-level display client
  // validation uses, mirroring UserForm.tsx's pattern.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }
    for (const issue of apiError.validationErrors) {
      const fieldName = issue.field.charAt(0).toLowerCase() + issue.field.slice(1);
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  function makeCodeUniquenessValidator(field: MasterFieldDef) {
    return (value: unknown): string | true => {
      if (!value) return true;
      const currentValues = watch();
      const conflict = existingRecords.find((record) => {
        if (record.id === recordId) return false;
        if (config.uniqueScopeField && record[config.uniqueScopeField] !== currentValues[config.uniqueScopeField]) return false;
        return String(record[field.key]).trim().toLowerCase() === String(value).trim().toLowerCase();
      });
      return conflict ? `This ${field.label.toLowerCase()} is already in use.` : true;
    };
  }

  function submitHandler(values: Record<string, unknown>) {
    const normalized = fromFormValues(config, values);
    if (config.validateForm) {
      const message = config.validateForm(normalized);
      if (message) {
        setCrossFieldError(message);
        return;
      }
    }
    setCrossFieldError(undefined);
    onSubmit(normalized, stagedPhotoFile ?? undefined);
  }

  const nonTextareaFields = config.fields.filter((field) => field.type !== 'textarea');
  const textareaFields = config.fields.filter((field) => field.type === 'textarea');

  function renderField(field: MasterFieldDef) {
    return (
      <FieldControl
        key={field.key}
        field={field}
        register={register}
        control={control}
        errors={errors}
        // The code field is immutable after creation server-side (Update*Request DTOs never
        // include it) — disable it in edit mode too, not just view, so the UI doesn't look
        // editable for a change that would silently no-op.
        readOnly={readOnly || (mode === 'edit' && field.key === config.codeField)}
        recordId={recordId}
        scopeValue={field.referenceScopeField ? watch(field.referenceScopeField) : undefined}
        validate={
          field.key === (config.codeField ?? config.nameField) && !field.skipUniquenessCheck
            ? makeCodeUniquenessValidator(field)
            : undefined
        }
      />
    );
  }

  /** Walks a group's fieldKeys, collapsing an adjacent `rangeLabel`-tagged 'time' pair into one
   * TimeRangeControl instead of two separate FieldControls — see MasterFieldDef.rangeLabel. */
  function renderGroupFields(group: MasterFieldGroup) {
    const nodes: React.ReactNode[] = [];
    for (let i = 0; i < group.fieldKeys.length; i += 1) {
      const field = config.fields.find((f) => f.key === group.fieldKeys[i])!;
      const nextField = i + 1 < group.fieldKeys.length ? config.fields.find((f) => f.key === group.fieldKeys[i + 1]) : undefined;
      if (field.type === 'time' && field.rangeLabel && nextField?.type === 'time') {
        nodes.push(<TimeRangeControl key={field.key} startField={field} endField={nextField} register={register} errors={errors} readOnly={readOnly} />);
        i += 1;
        continue;
      }
      nodes.push(renderField(field));
    }
    return nodes;
  }

  const statusCard = (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Status</CardTitle>
      </CardHeader>
      <CardContent>
        <Controller
          name="isActive"
          control={control}
          render={({ field: controllerField }) => (
            <div className="flex items-center gap-3">
              <Switch
                checked={Boolean(controllerField.value)}
                onCheckedChange={controllerField.onChange}
                disabled={readOnly}
                aria-label="Active"
              />
              <div>
                <p className="text-sm font-medium text-foreground">{controllerField.value ? 'Active' : 'Inactive'}</p>
                <p className="text-xs text-muted-foreground">Inactive {config.label.toLowerCase()}s will not be available for selection.</p>
              </div>
            </div>
          )}
        />
      </CardContent>
    </Card>
  );

  const footer = (
    <>
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      {crossFieldError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {crossFieldError}
        </p>
      )}

      {!readOnly && (
        <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : 'Save'}
          </Button>
        </div>
      )}
    </>
  );

  // Opt-in sectioned layout (MasterEntityConfig.fieldGroups) — every other entity leaves this
  // unset and falls through to the classic single flat-card layout below, unchanged.
  if (config.fieldGroups) {
    return (
      <form onSubmit={handleSubmit(submitHandler)} noValidate className="flex w-full flex-col gap-5">
        {config.fieldGroups.map((group, index) => (
          <Card key={group.key}>
            <CardHeader className="flex flex-row items-center gap-2 space-y-0 rounded-t-xl bg-muted/50 py-3">
              {group.icon && (
                <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
                  <group.icon className="h-4 w-4" />
                </span>
              )}
              <CardTitle className="text-base">{group.label}</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-4 pt-4">
              <div className={cn('flex flex-col gap-4', config.photo && index === 0 && 'sm:flex-row')}>
                {config.photo && index === 0 && (
                  <MasterPhotoUpload
                    config={config}
                    mode={mode}
                    recordId={recordId}
                    initialUrl={defaultValues[config.photo.urlField] as string | null | undefined}
                    onFileStaged={setStagedPhotoFile}
                  />
                )}
                <div
                  className={cn(
                    'flex flex-1 flex-wrap gap-4',
                    group.dividedColumns && 'gap-0 divide-x divide-border [&>*]:px-4 [&>*]:first:pl-0',
                  )}
                >
                  {renderGroupFields(group)}
                </div>
              </div>
              {group.infoText && <InfoCallout box={{ text: group.infoText }} />}
            </CardContent>
          </Card>
        ))}

        {textareaFields.map((field) => renderField(field))}

        {statusCard}

        {footer}
      </form>
    );
  }

  return (
    <form onSubmit={handleSubmit(submitHandler)} noValidate className="mx-auto flex w-full max-w-4xl flex-col gap-5">
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{config.label} Information</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-4">
            {nonTextareaFields.map((field) => renderField(field))}

            <div className="flex w-full flex-col gap-1 sm:w-40">
              <span className="text-sm font-medium leading-none text-foreground">Status</span>
              <Controller
                name="isActive"
                control={control}
                render={({ field: controllerField }) => (
                  <div className="flex h-10 items-center gap-2">
                    <Switch
                      checked={Boolean(controllerField.value)}
                      onCheckedChange={controllerField.onChange}
                      disabled={readOnly}
                      aria-label="Active"
                    />
                    <span className="text-sm text-muted-foreground">{controllerField.value ? 'Active' : 'Inactive'}</span>
                  </div>
                )}
              />
            </div>
          </div>

          {textareaFields.map((field) => renderField(field))}
        </CardContent>
      </Card>

      {footer}
    </form>
  );
}
