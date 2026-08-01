import type { ApiError } from '@hms/shared';
import { useEffect, useState } from 'react';
import { Controller, useForm, type Control, type FieldErrors, type UseFormRegister } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { useMasterOptionsQuery } from '../hooks/useMasterQuery';
import { getDisplayLabel, getMasterConfig } from '../engine/registry';
import type { MasterEntityConfig, MasterFieldDef, MasterRecord } from '../engine/types';

/** Radix Select can't represent "no selection" with an empty string, so optional reference/select fields use this sentinel internally. */
const NONE_VALUE = '__none__';

interface MasterFormProps {
  config: MasterEntityConfig;
  mode: 'create' | 'edit' | 'view';
  recordId?: string;
  defaultValues: Record<string, unknown>;
  isSubmitting?: boolean;
  apiError?: ApiError | null;
  onSubmit: (values: Record<string, unknown>) => void;
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
  const referenceOptions = useMasterOptionsQuery(field.type === 'reference' ? field.referenceEntityKey : undefined);
  const referenceConfig = field.type === 'reference' ? getMasterConfig(field.referenceEntityKey) : undefined;
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
    onSubmit(normalized);
  }

  const nonTextareaFields = config.fields.filter((field) => field.type !== 'textarea');
  const textareaFields = config.fields.filter((field) => field.type === 'textarea');

  return (
    <form onSubmit={handleSubmit(submitHandler)} noValidate className="mx-auto flex w-full max-w-4xl flex-col gap-5">
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{config.label} Information</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-4">
            {nonTextareaFields.map((field) => (
              <FieldControl
                key={field.key}
                field={field}
                register={register}
                control={control}
                errors={errors}
                // The code field is immutable after creation server-side (Update*Request DTOs
                // never include it) — disable it in edit mode too, not just view, so the UI
                // doesn't look editable for a change that would silently no-op.
                readOnly={readOnly || (mode === 'edit' && field.key === config.codeField)}
                recordId={recordId}
                scopeValue={field.referenceScopeField ? watch(field.referenceScopeField) : undefined}
                validate={field.key === (config.codeField ?? config.nameField) ? makeCodeUniquenessValidator(field) : undefined}
              />
            ))}

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

          {textareaFields.map((field) => (
            <FieldControl
              key={field.key}
              field={field}
              register={register}
              control={control}
              errors={errors}
              readOnly={readOnly}
              recordId={recordId}
            />
          ))}
        </CardContent>
      </Card>

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
    </form>
  );
}
