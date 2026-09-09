import { ApiError, createVitalsReadingSchema, type VitalsReadingFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Plus } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { usePostVitalsReadingMutation, useVitalsReadingsQuery } from '../hooks/useVitalsReadings';

interface VitalsPanelProps {
  admissionId: string;
}

const defaultValues: VitalsReadingFormValues = {
  recordedAt: '',
  temperatureF: undefined,
  pulseRate: undefined,
  respiratoryRate: undefined,
  bloodPressureSystolic: undefined,
  bloodPressureDiastolic: undefined,
  spO2Percent: undefined,
  weightKg: undefined,
  heightCm: undefined,
  painScore: undefined,
  bloodGlucoseMgDl: undefined,
  notes: '',
};

export function VitalsPanel({ admissionId }: VitalsPanelProps) {
  const { data: readings, isPending, isError } = useVitalsReadingsQuery(admissionId);
  const mutation = usePostVitalsReadingMutation(admissionId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<VitalsReadingFormValues>({
    resolver: zodResolver(createVitalsReadingSchema),
    defaultValues,
  });

  function onSubmit(values: VitalsReadingFormValues) {
    mutation.mutate(
      {
        recordedAt: new Date(values.recordedAt).toISOString(),
        temperatureF: values.temperatureF,
        pulseRate: values.pulseRate,
        respiratoryRate: values.respiratoryRate,
        bloodPressureSystolic: values.bloodPressureSystolic,
        bloodPressureDiastolic: values.bloodPressureDiastolic,
        spO2Percent: values.spO2Percent,
        weightKg: values.weightKg,
        heightCm: values.heightCm,
        painScore: values.painScore,
        bloodGlucoseMgDl: values.bloodGlucoseMgDl,
        notes: values.notes || null,
      },
      { onSuccess: () => reset(defaultValues) },
    );
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <div className="flex flex-col gap-4">
      {isPending && (
        <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading vitals…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load vitals.
        </p>
      )}

      {!isPending && !isError && (
        <div className="overflow-hidden rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2.5">Recorded</th>
                <th className="px-3 py-2.5">Temp (°F)</th>
                <th className="px-3 py-2.5">Pulse</th>
                <th className="px-3 py-2.5">RR</th>
                <th className="px-3 py-2.5">BP</th>
                <th className="px-3 py-2.5">SpO2</th>
                <th className="px-3 py-2.5">Weight</th>
                <th className="px-3 py-2.5">Pain</th>
                <th className="px-3 py-2.5">Glucose</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {(readings ?? []).length === 0 && (
                <tr>
                  <td colSpan={9} className="px-4 py-6 text-center text-sm text-muted-foreground">
                    No vitals recorded yet.
                  </td>
                </tr>
              )}
              {readings?.map((reading) => (
                <tr key={reading.id}>
                  <td className="px-3 py-3 text-sm text-muted-foreground">{new Date(reading.recordedAt).toLocaleString('en-IN')}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{reading.temperatureF ?? '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{reading.pulseRate ?? '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{reading.respiratoryRate ?? '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">
                    {reading.bloodPressureSystolic && reading.bloodPressureDiastolic
                      ? `${reading.bloodPressureSystolic}/${reading.bloodPressureDiastolic}`
                      : '—'}
                  </td>
                  <td className="px-3 py-3 text-sm text-foreground">{reading.spO2Percent ?? '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{reading.weightKg ?? '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{reading.painScore ?? '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{reading.bloodGlucoseMgDl ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-3 rounded-lg border border-dashed border-border p-4">
        {apiError && !apiError.validationErrors && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.message}
          </p>
        )}

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="recordedAt">Recorded at</Label>
            <Input id="recordedAt" type="datetime-local" {...register('recordedAt')} />
            {errors.recordedAt && <p className="text-xs text-destructive">{errors.recordedAt.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="temperatureF">Temp (°F)</Label>
            <Input id="temperatureF" type="number" step="0.1" {...register('temperatureF')} />
            {errors.temperatureF && <p className="text-xs text-destructive">{errors.temperatureF.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="pulseRate">Pulse</Label>
            <Input id="pulseRate" type="number" {...register('pulseRate')} />
            {errors.pulseRate && <p className="text-xs text-destructive">{errors.pulseRate.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="respiratoryRate">Resp. rate</Label>
            <Input id="respiratoryRate" type="number" {...register('respiratoryRate')} />
            {errors.respiratoryRate && <p className="text-xs text-destructive">{errors.respiratoryRate.message}</p>}
          </div>
          <div className="flex gap-2">
            <div className="flex flex-1 flex-col gap-1.5">
              <Label htmlFor="bloodPressureSystolic">BP systolic</Label>
              <Input id="bloodPressureSystolic" type="number" {...register('bloodPressureSystolic')} />
              {errors.bloodPressureSystolic && <p className="text-xs text-destructive">{errors.bloodPressureSystolic.message}</p>}
            </div>
            <div className="flex flex-1 flex-col gap-1.5">
              <Label htmlFor="bloodPressureDiastolic">BP diastolic</Label>
              <Input id="bloodPressureDiastolic" type="number" {...register('bloodPressureDiastolic')} />
              {errors.bloodPressureDiastolic && <p className="text-xs text-destructive">{errors.bloodPressureDiastolic.message}</p>}
            </div>
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="spO2Percent">SpO2 (%)</Label>
            <Input id="spO2Percent" type="number" {...register('spO2Percent')} />
            {errors.spO2Percent && <p className="text-xs text-destructive">{errors.spO2Percent.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="weightKg">Weight (kg)</Label>
            <Input id="weightKg" type="number" step="0.1" {...register('weightKg')} />
            {errors.weightKg && <p className="text-xs text-destructive">{errors.weightKg.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="heightCm">Height (cm)</Label>
            <Input id="heightCm" type="number" step="0.1" {...register('heightCm')} />
            {errors.heightCm && <p className="text-xs text-destructive">{errors.heightCm.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="painScore">Pain (0-10)</Label>
            <Input id="painScore" type="number" {...register('painScore')} />
            {errors.painScore && <p className="text-xs text-destructive">{errors.painScore.message}</p>}
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="bloodGlucoseMgDl">Glucose (mg/dL)</Label>
            <Input id="bloodGlucoseMgDl" type="number" step="0.1" {...register('bloodGlucoseMgDl')} />
            {errors.bloodGlucoseMgDl && <p className="text-xs text-destructive">{errors.bloodGlucoseMgDl.message}</p>}
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="notes">Notes (optional)</Label>
          <Input id="notes" {...register('notes')} />
        </div>

        <Button type="submit" disabled={mutation.isPending} className="w-fit gap-1.5">
          <Plus className="h-4 w-4" />
          {mutation.isPending ? 'Recording…' : 'Record Vitals'}
        </Button>
      </form>
    </div>
  );
}
