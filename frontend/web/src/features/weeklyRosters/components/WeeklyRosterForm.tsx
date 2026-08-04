import { ApiError, weeklyRosterSchema, type WeeklyRosterFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface WeeklyRosterFormProps {
  defaultValues?: Partial<WeeklyRosterFormValues>;
  onSubmit: (values: WeeklyRosterFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
}

export function WeeklyRosterForm({ defaultValues, onSubmit, isSubmitting, submitLabel, apiError }: WeeklyRosterFormProps) {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<WeeklyRosterFormValues>({
    resolver: zodResolver(weeklyRosterSchema),
    defaultValues: {
      weekStartDate: '',
      departmentId: '',
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
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof WeeklyRosterFormValues;
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
        <Label htmlFor="weekStartDate">Week start date</Label>
        <Input id="weekStartDate" type="date" {...register('weekStartDate')} />
        {errors.weekStartDate && <p className="text-sm text-destructive">{errors.weekStartDate.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="departmentId">Department ID</Label>
        <Input id="departmentId" placeholder="00000000-0000-0000-0000-000000000000" {...register('departmentId')} />
        <p className="text-xs text-muted-foreground">
          No department directory exists yet — enter the department's GUID directly.
        </p>
        {errors.departmentId && <p className="text-sm text-destructive">{errors.departmentId.message}</p>}
      </div>

      <Button type="submit" disabled={isSubmitting} className="mt-2 self-start">
        {isSubmitting ? 'Saving…' : submitLabel}
      </Button>
    </form>
  );
}
