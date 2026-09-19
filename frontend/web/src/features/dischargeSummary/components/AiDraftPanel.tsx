import { ApiError, type DischargeSummaryDraftSuggestion, type DischargeSummaryFormValues } from '@hms/shared';
import { Loader2, Sparkles } from 'lucide-react';
import type { UseFormGetValues, UseFormSetValue } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useToast } from '@/components/ui/toast-context';
import { useAiDraftDischargeSummaryMutation } from '../hooks/useDischargeSummaryMutations';

const AI_NOT_CONFIGURED_ERROR_CODE = 'DISCHARGE_SUMMARY.AI_NOT_CONFIGURED';

const TEXT_KEYS = [
  'chiefComplaints',
  'historyOfPresentingIllness',
  'courseInHospital',
  'conditionAtDischarge',
  'diet',
  'woundCare',
  'activity',
  'physiotherapy',
  'reviewInstructions',
  'emergencyInstructions',
  'bloodPressure',
] as const satisfies readonly (keyof DischargeSummaryFormValues & keyof DischargeSummaryDraftSuggestion)[];

const NUMBER_KEYS = [
  'heightCm',
  'weightKg',
  'pulseRate',
  'respiratoryRate',
  'temperatureF',
  'spO2Percent',
] as const satisfies readonly (keyof DischargeSummaryFormValues & keyof DischargeSummaryDraftSuggestion)[];

function isBlank(value: unknown): boolean {
  return value === undefined || value === null || (typeof value === 'string' && value.trim() === '') || (typeof value === 'number' && Number.isNaN(value));
}

interface AiDraftPanelProps {
  summaryId: string;
  getValues: UseFormGetValues<DischargeSummaryFormValues>;
  setValue: UseFormSetValue<DischargeSummaryFormValues>;
}

/**
 * "Draft with AI" for a Draft discharge summary: the backend gathers this admission's IPD data
 * (notes, vitals, orders) and returns a suggestion, which is merged into the form here. Only
 * fields that are currently empty are filled — text the clinician already wrote is never
 * overwritten — and nothing is saved until they use Save Draft as usual.
 */
export function AiDraftPanel({ summaryId, getValues, setValue }: AiDraftPanelProps) {
  const { toast } = useToast();
  const mutation = useAiDraftDischargeSummaryMutation();

  function apply(suggestion: DischargeSummaryDraftSuggestion) {
    let filled = 0;
    let kept = 0;

    for (const key of TEXT_KEYS) {
      const suggested = suggestion[key];
      if (isBlank(suggested)) continue;
      if (isBlank(getValues(key))) {
        setValue(key, suggested as string, { shouldDirty: true });
        filled += 1;
      } else {
        kept += 1;
      }
    }

    for (const key of NUMBER_KEYS) {
      const suggested = suggestion[key];
      if (isBlank(suggested)) continue;
      if (isBlank(getValues(key))) {
        setValue(key, suggested as number, { shouldDirty: true });
        filled += 1;
      } else {
        kept += 1;
      }
    }

    toast({
      title: filled > 0 ? 'Draft ready for review' : 'Nothing to fill',
      description:
        filled > 0
          ? `${filled} empty field(s) drafted${kept > 0 ? `; ${kept} field(s) you already filled were left as-is` : ''}. Review before saving.`
          : 'Every field the AI could draft already has content.',
      variant: filled > 0 ? 'success' : 'default',
    });
  }

  function handleClick() {
    mutation.mutate(summaryId, {
      onSuccess: apply,
      onError: (error) => {
        const notConfigured = error instanceof ApiError && error.errorCode === AI_NOT_CONFIGURED_ERROR_CODE;
        toast({
          title: 'Could not draft summary',
          description: notConfigured
            ? "AI drafting isn't configured for this environment."
            : error instanceof ApiError
              ? error.message
              : 'Something went wrong. Please try again.',
          variant: 'error',
        });
      },
    });
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <Sparkles className="h-4 w-4 text-primary" />
          AI Draft
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-wrap items-center justify-between gap-3">
        <p className="max-w-2xl text-sm text-muted-foreground">
          Drafts the clinical narrative and advice from this admission&apos;s progress notes, vitals and orders. Only empty fields are filled;
          examination, procedures and discharge medications are left for you. Always review before saving.
        </p>
        <Button type="button" variant="outline" disabled={mutation.isPending} onClick={handleClick} className="gap-1.5">
          {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
          {mutation.isPending ? 'Drafting…' : 'Draft with AI'}
        </Button>
      </CardContent>
    </Card>
  );
}
