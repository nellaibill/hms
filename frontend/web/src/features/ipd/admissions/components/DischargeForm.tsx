import { ApiError, DISCHARGE_TYPES, dischargeAdmissionSchema, type DischargeAdmissionFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

const dischargeTypeLabels: Record<(typeof DISCHARGE_TYPES)[number], string> = {
  Normal: 'Normal',
  AgainstMedicalAdvice: 'Against Medical Advice',
  Referred: 'Referred',
};

interface DischargeFormProps {
  isSubmitting: boolean;
  apiError: ApiError | null;
  onSubmit: (values: DischargeAdmissionFormValues) => void;
}

export function DischargeForm({ isSubmitting, apiError, onSubmit }: DischargeFormProps) {
  const {
    control,
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<DischargeAdmissionFormValues>({
    resolver: zodResolver(dischargeAdmissionSchema),
    defaultValues: {
      dischargeDateTime: '',
      dischargeType: 'Normal',
      finalDiagnosis: '',
      dischargeNotes: '',
      followUpAdvice: '',
    },
  });

  useEffect(() => {
    if (!apiError?.validationErrors) {
      return;
    }
    for (const issue of apiError.validationErrors) {
      const fieldName = (issue.field.charAt(0).toLowerCase() + issue.field.slice(1)) as keyof DischargeAdmissionFormValues;
      setError(fieldName, { type: 'server', message: issue.message });
    }
  }, [apiError, setError]);

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex max-w-xl flex-col gap-4">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="dischargeDateTime">Discharge date/time</Label>
          <Input id="dischargeDateTime" type="datetime-local" {...register('dischargeDateTime')} />
          {errors.dischargeDateTime && <p className="text-sm text-destructive">{errors.dischargeDateTime.message}</p>}
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="dischargeType">Discharge type</Label>
          <Controller
            control={control}
            name="dischargeType"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id="dischargeType" aria-label="Discharge type">
                  <SelectValue placeholder="Select discharge type…" />
                </SelectTrigger>
                <SelectContent>
                  {DISCHARGE_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {dischargeTypeLabels[type]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          {errors.dischargeType && <p className="text-sm text-destructive">{errors.dischargeType.message}</p>}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="finalDiagnosis">Final diagnosis</Label>
        <textarea
          id="finalDiagnosis"
          rows={2}
          {...register('finalDiagnosis')}
          className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        />
        {errors.finalDiagnosis && <p className="text-sm text-destructive">{errors.finalDiagnosis.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="dischargeNotes">Discharge notes</Label>
        <textarea
          id="dischargeNotes"
          rows={3}
          {...register('dischargeNotes')}
          className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        />
        {errors.dischargeNotes && <p className="text-sm text-destructive">{errors.dischargeNotes.message}</p>}
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="followUpAdvice">Follow-up advice</Label>
        <textarea
          id="followUpAdvice"
          rows={2}
          {...register('followUpAdvice')}
          className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
        />
        {errors.followUpAdvice && <p className="text-sm text-destructive">{errors.followUpAdvice.message}</p>}
      </div>

      <Button type="submit" variant="destructive" disabled={isSubmitting} className="mt-2 self-start">
        {isSubmitting ? 'Discharging…' : 'Confirm Discharge'}
      </Button>
    </form>
  );
}
