import { ApiError, createShiftSchema, type ShiftFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';

interface ShiftFormProps {
  mode: 'create' | 'edit';
  defaultValues?: Partial<ShiftFormValues>;
  onSubmit: (values: ShiftFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function ShiftForm({ mode, defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: ShiftFormProps) {
  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ShiftFormValues>({
    resolver: zodResolver(createShiftSchema),
    defaultValues: {
      code: '',
      name: '',
      startTime: '',
      endTime: '',
      breakMinutes: 0,
      graceMinutes: 0,
      isNightShift: false,
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
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof ShiftFormValues;
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
        {mode === 'edit' && <p className="text-xs text-muted-foreground">Code can't be changed after a shift is created.</p>}
        {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="name">Name</Label>
        <Input id="name" {...register('name')} />
        {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="startTime">Start time</Label>
          <Input id="startTime" type="time" step="1" {...register('startTime')} />
          {errors.startTime && <p className="text-sm text-destructive">{errors.startTime.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="endTime">End time</Label>
          <Input id="endTime" type="time" step="1" {...register('endTime')} />
          {errors.endTime && <p className="text-sm text-destructive">{errors.endTime.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="breakMinutes">Break (minutes)</Label>
          <Input id="breakMinutes" type="number" {...register('breakMinutes')} />
          {errors.breakMinutes && <p className="text-sm text-destructive">{errors.breakMinutes.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="graceMinutes">Grace (minutes)</Label>
          <Input id="graceMinutes" type="number" {...register('graceMinutes')} />
          {errors.graceMinutes && <p className="text-sm text-destructive">{errors.graceMinutes.message}</p>}
        </div>
      </div>

      <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
        <Label htmlFor="isNightShift" className="cursor-pointer">Night shift</Label>
        <Controller
          control={control}
          name="isNightShift"
          render={({ field }) => <Switch id="isNightShift" checked={field.value} onCheckedChange={field.onChange} />}
        />
      </div>

      <div className="flex items-center justify-between rounded-md border border-border px-3 py-2.5">
        <Label htmlFor="isActive" className="cursor-pointer">Active</Label>
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
