import { ApiError, createNursingAssessmentSchema, type NursingAssessmentFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Plus } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useNursingAssessmentsQuery, usePostNursingAssessmentMutation } from '../hooks/useNursingAssessments';

interface NursingAssessmentPanelProps {
  admissionId: string;
}

const defaultValues: NursingAssessmentFormValues = {
  assessedAt: '',
  generalCondition: '',
  consciousnessLevel: '',
  mobility: '',
  nutritionStatus: '',
  fallRisk: '',
  pressureSoreRisk: '',
  skinCondition: '',
  painScore: undefined,
  notes: '',
};

export function NursingAssessmentPanel({ admissionId }: NursingAssessmentPanelProps) {
  const { data: assessments, isPending, isError } = useNursingAssessmentsQuery(admissionId);
  const mutation = usePostNursingAssessmentMutation(admissionId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<NursingAssessmentFormValues>({
    resolver: zodResolver(createNursingAssessmentSchema),
    defaultValues,
  });

  function onSubmit(values: NursingAssessmentFormValues) {
    mutation.mutate(
      {
        assessedAt: new Date(values.assessedAt).toISOString(),
        generalCondition: values.generalCondition || null,
        consciousnessLevel: values.consciousnessLevel || null,
        mobility: values.mobility || null,
        nutritionStatus: values.nutritionStatus || null,
        fallRisk: values.fallRisk || null,
        pressureSoreRisk: values.pressureSoreRisk || null,
        skinCondition: values.skinCondition || null,
        painScore: values.painScore,
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
          Loading nursing assessments…
        </div>
      )}

      {isError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          Failed to load nursing assessments.
        </p>
      )}

      {!isPending && !isError && (
        <div className="overflow-x-auto rounded-lg border border-border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50 text-left text-xs font-medium uppercase tracking-wide text-muted-foreground">
              <tr>
                <th className="px-3 py-2.5">Assessed</th>
                <th className="px-3 py-2.5">Condition</th>
                <th className="px-3 py-2.5">Consciousness</th>
                <th className="px-3 py-2.5">Mobility</th>
                <th className="px-3 py-2.5">Fall Risk</th>
                <th className="px-3 py-2.5">Pressure Sore Risk</th>
                <th className="px-3 py-2.5">Pain</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {(assessments ?? []).length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-6 text-center text-sm text-muted-foreground">
                    No nursing assessments recorded yet.
                  </td>
                </tr>
              )}
              {assessments?.map((assessment) => (
                <tr key={assessment.id}>
                  <td className="px-3 py-3 text-sm text-muted-foreground">{new Date(assessment.assessedAt).toLocaleString('en-IN')}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{assessment.generalCondition || '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{assessment.consciousnessLevel || '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{assessment.mobility || '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{assessment.fallRisk || '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{assessment.pressureSoreRisk || '—'}</td>
                  <td className="px-3 py-3 text-sm text-foreground">{assessment.painScore ?? '—'}</td>
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

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="assessedAt">Assessed at</Label>
          <Input id="assessedAt" type="datetime-local" className="w-64" {...register('assessedAt')} />
          {errors.assessedAt && <p className="text-xs text-destructive">{errors.assessedAt.message}</p>}
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="generalCondition">General condition</Label>
            <Input id="generalCondition" {...register('generalCondition')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="consciousnessLevel">Consciousness</Label>
            <Input id="consciousnessLevel" {...register('consciousnessLevel')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="mobility">Mobility</Label>
            <Input id="mobility" {...register('mobility')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="nutritionStatus">Nutrition status</Label>
            <Input id="nutritionStatus" {...register('nutritionStatus')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="fallRisk">Fall risk</Label>
            <Input id="fallRisk" {...register('fallRisk')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="pressureSoreRisk">Pressure sore risk</Label>
            <Input id="pressureSoreRisk" {...register('pressureSoreRisk')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="skinCondition">Skin condition</Label>
            <Input id="skinCondition" {...register('skinCondition')} />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="painScore">Pain (0-10)</Label>
            <Input id="painScore" type="number" {...register('painScore')} />
            {errors.painScore && <p className="text-xs text-destructive">{errors.painScore.message}</p>}
          </div>
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="notes">Notes (optional)</Label>
          <Input id="notes" {...register('notes')} />
        </div>

        <Button type="submit" disabled={mutation.isPending} className="w-fit gap-1.5">
          <Plus className="h-4 w-4" />
          {mutation.isPending ? 'Saving…' : 'Record Assessment'}
        </Button>
      </form>
    </div>
  );
}
