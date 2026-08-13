import { ApiError, createWardSchema, WARD_TYPES, type WardFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { DepartmentSelect } from '@/components/DepartmentSelect';

const wardTypeLabels: Record<(typeof WARD_TYPES)[number], string> = {
  General: 'General',
  SemiPrivate: 'Semi-Private',
  Private: 'Private',
  ICU: 'ICU',
};

interface WardFormProps {
  mode: 'create' | 'edit';
  defaultValues?: Partial<WardFormValues>;
  onSubmit: (values: WardFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function WardForm({ mode, defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: WardFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<WardFormValues>({
    resolver: zodResolver(createWardSchema),
    defaultValues: {
      code: '',
      name: '',
      departmentId: '',
      wardType: 'General',
      isActive: true,
      ...defaultValues,
    },
  });

  // Server-side validation failures (docs/ApiStandards.md §5) are mapped onto the same
  // field-level display client validation uses, per docs/FrontendArchitecture.md §9.
  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }

    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof WardFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex max-w-lg flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="code">Code</Label>
        <Input id="code" disabled={mode === 'edit'} {...register('code')} />
        {mode === 'edit' && <p className="text-xs text-muted-foreground">Code can't be changed after a ward is created.</p>}
        {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="name">Name</Label>
        <Input id="name" {...register('name')} />
        {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="departmentId">Department</Label>
        <Controller
          control={control}
          name="departmentId"
          render={({ field }) => (
            <DepartmentSelect id="departmentId" value={field.value} onValueChange={field.onChange} ariaLabel="Department" />
          )}
        />
        {errors.departmentId && <p className="text-sm text-destructive">{errors.departmentId.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="wardType">Ward type</Label>
        <Controller
          control={control}
          name="wardType"
          render={({ field }) => (
            <Select value={field.value} onValueChange={field.onChange}>
              <SelectTrigger id="wardType" aria-label="Ward type">
                <SelectValue placeholder="Select ward type…" />
              </SelectTrigger>
              <SelectContent>
                {WARD_TYPES.map((type) => (
                  <SelectItem key={type} value={type}>
                    {wardTypeLabels[type]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        />
        {errors.wardType && <p className="text-sm text-destructive">{errors.wardType.message}</p>}
      </div>

      <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
        <Label htmlFor="isActive" className="cursor-pointer">
          Active
        </Label>
        <Controller
          control={control}
          name="isActive"
          render={({ field }) => <Switch id="isActive" checked={field.value} onCheckedChange={field.onChange} />}
        />
      </div>

      <Button type="submit" disabled={isSubmitting} className="mt-2 self-start">
        {isSubmitting ? 'Saving…' : submitLabel}
      </Button>
    </form>
  );
}
