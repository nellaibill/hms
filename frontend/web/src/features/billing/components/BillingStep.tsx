import { zodResolver } from '@hookform/resolvers/zod';
import { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { billingFormSchema, defaultBillingFormValues, type BillingFormValues } from '../billingValidation';
import type { BillingType } from '../types';
import { BillingSummaryCard } from './BillingSummaryCard';
import { ConsultationBillingCard } from './ConsultationBillingCard';
import { FileBillingCard } from './FileBillingCard';
import { InjectionBillingCard } from './InjectionBillingCard';
import { LaboratoryBillingCard } from './LaboratoryBillingCard';
import { ProcedureBillingCard } from './ProcedureBillingCard';
import { RadiologyBillingCard } from './RadiologyBillingCard';

export interface BillingStepHandle {
  /** Validates every category (marking all as "attempted" so error dots appear) — call before letting the wizard proceed past Billing. */
  validate: () => Promise<boolean>;
  getValues: () => BillingFormValues;
}

interface BillingStepProps {
  defaultValues?: BillingFormValues;
  onChange?: (values: BillingFormValues) => void;
  /** Reports whether the Billing tab should show its error dot — true once at least one category has been attempted and currently has an error. */
  onErrorStateChange?: (hasError: boolean) => void;
  /** Reports whether the form has any unsaved edits — true the moment a field first differs from its default, so the caller can warn before navigating away. */
  onDirtyChange?: (isDirty: boolean) => void;
  /** Passed straight through to BillingSummaryCard, which renders the actual save button/error
   * inside its own sticky sidebar — see that component's props for why it lives there instead
   * of below this step. Omit entirely to render the summary with no save action. */
  onSave?: () => void;
  isSaving?: boolean;
  saveError?: string | null;
  saveErrorDetails?: string[];
}

const CATEGORY_FIELD = {
  Consultation: 'consultation',
  Radiology: 'radiology',
  Laboratory: 'laboratory',
  Procedure: 'procedure',
  Injection: 'injection',
  File: 'file',
} as const satisfies Record<BillingType, keyof BillingFormValues>;

const CATEGORY_ORDER: BillingType[] = ['Consultation', 'Radiology', 'Laboratory', 'Procedure', 'Injection', 'File'];

/**
 * Step 5 of the registration wizard. Owns its own useForm/zod resolver rather than being
 * folded into the patient form's schema — Billing is its own bounded context (see the
 * normalized Billing/BillingItem model), so PatientRegistrationForm just drives it via the
 * exposed `validate`/`getValues` handle the same way it drives per-tab validation elsewhere.
 */
export const BillingStep = forwardRef<BillingStepHandle, BillingStepProps>(function BillingStep(
  { defaultValues, onChange, onErrorStateChange, onDirtyChange, onSave, isSaving, saveError, saveErrorDetails },
  ref,
) {
  // No `mode: 'onChange'` — with a whole-schema Zod resolver (not per-field), that mode fires
  // one full-form async validation pass on every single field change, and several of those
  // firing in quick succession (e.g. filling in a second Collect Payment row: amount, mode
  // select, then another row) can resolve out of order, letting an earlier pass's now-stale
  // result overwrite a later, correct one — surfacing a phantom error on a field that's
  // actually valid. Same root cause and same fix already applied to
  // PatientRegistrationForm.tsx for this exact symptom. RHF's default `reValidateMode:
  // 'onChange'` still live-clears an error once a field has been validated at least once (via
  // toggleCategory's per-category trigger() on collapse, or Save's full validate()), so nothing
  // about the tab-dot/error-summary/inline-message UX regresses — errors just don't start
  // appearing before the user has attempted to proceed, which matches how every other tab in
  // this app already behaves.
  const methods = useForm<BillingFormValues>({
    resolver: zodResolver(billingFormSchema),
    defaultValues: defaultValues ?? defaultBillingFormValues,
  });
  const {
    trigger,
    watch,
    getValues,
    formState: { errors, isDirty },
  } = methods;

  const [expanded, setExpanded] = useState<Record<BillingType, boolean>>({
    Consultation: false,
    Radiology: false,
    Laboratory: false,
    Procedure: false,
    Injection: false,
    File: false,
  });
  const [attempted, setAttempted] = useState<ReadonlySet<BillingType>>(new Set());

  useImperativeHandle(
    ref,
    () => ({
      validate: async () => {
        const valid = await trigger();
        setAttempted(new Set(CATEGORY_ORDER));
        return valid;
      },
      getValues: () => getValues(),
    }),
    [trigger, getValues],
  );

  useEffect(() => {
    const subscription = watch((values) => onChange?.(values as BillingFormValues));
    return () => subscription.unsubscribe();
  }, [watch, onChange]);

  useEffect(() => {
    const hasAnyError = CATEGORY_ORDER.some((category) => attempted.has(category) && Boolean(errors[CATEGORY_FIELD[category]]));
    onErrorStateChange?.(hasAnyError);
  }, [errors, attempted, onErrorStateChange]);

  useEffect(() => {
    onDirtyChange?.(isDirty);
  }, [isDirty, onDirtyChange]);

  async function toggleCategory(category: BillingType) {
    const willExpand = !expanded[category];
    if (!willExpand) {
      // Collapsing — validate this card's own fields now, so an error surfaces immediately
      // rather than only on final submit (each card validates independently).
      await trigger(CATEGORY_FIELD[category]);
      setAttempted((prev) => new Set(prev).add(category));
    }
    setExpanded((prev) => ({ ...prev, [category]: willExpand }));
  }

  const hasFieldError = (category: BillingType) => attempted.has(category) && Boolean(errors[CATEGORY_FIELD[category]]);

  return (
    <FormProvider {...methods}>
      {/* 380px matches BrandingForm's own two-column sidebar (frontend/web/src/features/
          branding/components/BrandingForm.tsx) — the widest sidebar width already established
          in this codebase, needed here since Collect Payment now packs three fields (Amount/
          Mode/Reference) per payment row plus the split-payment controls. */}
      <div className="grid grid-cols-1 items-start gap-4 lg:grid-cols-[minmax(0,1fr)_380px]">
        <div className="flex flex-col gap-4">
          <ConsultationBillingCard
            expanded={expanded.Consultation}
            onToggle={() => toggleCategory('Consultation')}
            hasError={hasFieldError('Consultation')}
          />
          <RadiologyBillingCard expanded={expanded.Radiology} onToggle={() => toggleCategory('Radiology')} hasError={hasFieldError('Radiology')} />
          <LaboratoryBillingCard
            expanded={expanded.Laboratory}
            onToggle={() => toggleCategory('Laboratory')}
            hasError={hasFieldError('Laboratory')}
          />
          <ProcedureBillingCard expanded={expanded.Procedure} onToggle={() => toggleCategory('Procedure')} hasError={hasFieldError('Procedure')} />
          <InjectionBillingCard expanded={expanded.Injection} onToggle={() => toggleCategory('Injection')} hasError={hasFieldError('Injection')} />
          <FileBillingCard expanded={expanded.File} onToggle={() => toggleCategory('File')} hasError={hasFieldError('File')} />
        </div>
        <BillingSummaryCard onSave={onSave} isSaving={isSaving} saveError={saveError} saveErrorDetails={saveErrorDetails} />
      </div>
    </FormProvider>
  );
});
