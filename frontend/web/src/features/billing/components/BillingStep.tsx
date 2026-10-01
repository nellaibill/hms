import { zodResolver } from '@hookform/resolvers/zod';
import { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { useLiveErrorRefresh } from '@/hooks/useLiveErrorRefresh';
import { isConsultationEntryActive } from '../billingActivity';
import { billingFormSchema, defaultBillingFormValues, type BillingFormValues } from '../billingValidation';
import type { BillingType } from '../types';
import { BillingSummaryCard } from './BillingSummaryCard';
import { ConsultationBillingCard } from './ConsultationBillingCard';
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
  // PatientRegistrationForm.tsx for this exact symptom. Errors still don't start appearing
  // before the user has attempted to proceed (toggleCategory's per-category trigger() on
  // collapse, or Save's full validate()), which matches how every other tab in this app
  // behaves; clearing them once fixed is useLiveErrorRefresh's job below, since RHF's
  // reValidateMode only applies after a real submit.
  const methods = useForm<BillingFormValues>({
    resolver: zodResolver(billingFormSchema),
    defaultValues: defaultValues ?? defaultBillingFormValues,
  });
  const {
    trigger,
    watch,
    getValues,
    setError,
    clearErrors,
    formState: { errors, isDirty },
  } = methods;
  useLiveErrorRefresh({ schema: billingFormSchema, watch, errors, setError, clearErrors });

  // A category starts expanded when its rows were prefilled (see InvoiceCreatePage's
  // billingDefaultValues) — Consultation from the patient's visit, Laboratory/Radiology from the
  // doctor's catalog investigations on that visit's consultation (OPD-01) — so reception sees
  // those charges right away rather than having to click to reveal them.
  const [expanded, setExpanded] = useState<Record<BillingType, boolean>>(() => ({
    Consultation: (defaultValues?.consultation ?? []).some(isConsultationEntryActive),
    Radiology: (defaultValues?.radiology ?? []).some((row) => Boolean(row.serviceId)),
    Laboratory: (defaultValues?.laboratory ?? []).some((row) => Boolean(row.itemId)),
    Procedure: false,
    Injection: false,
    File: false,
  }));
  const [attempted, setAttempted] = useState<ReadonlySet<BillingType>>(new Set());

  // Remembers the last consultant picked in Radiology/Laboratory/Procedure Billing (the three
  // categories with a per-row Consultant field — Injection has none, Consultation's is locked
  // to the visit's own department/consultant), so the next row added in any of those three
  // starts pre-filled with the same consultant instead of asking reception to reselect them
  // for every line item on the same visit. Plain component state (not part of the RHF form —
  // it's a UI convenience, not billing data) that lives only as long as this BillingStep
  // instance does; InvoiceCreatePage remounts a fresh one per patient (key={patient.id}) and
  // "Bill Another Patient" resets to no patient at all, so this always starts blank for a new
  // billing transaction.
  const [lastConsultantId, setLastConsultantId] = useState('');

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
          Mode/Reference) per payment row plus the split-payment controls. Two columns only
          from xl, not lg: with the 280px sidebar open, a 1024px viewport leaves ~680px of
          content width, which would crush the billing cards to ~280px beside this column. */}
      <div className="grid grid-cols-1 items-start gap-4 xl:grid-cols-[minmax(0,1fr)_380px]">
        <div className="flex flex-col gap-4">
          <ConsultationBillingCard
            expanded={expanded.Consultation}
            onToggle={() => toggleCategory('Consultation')}
            hasError={hasFieldError('Consultation')}
          />
          <RadiologyBillingCard
            expanded={expanded.Radiology}
            onToggle={() => toggleCategory('Radiology')}
            hasError={hasFieldError('Radiology')}
            defaultConsultantId={lastConsultantId}
            onConsultantSelected={setLastConsultantId}
          />
          <LaboratoryBillingCard
            expanded={expanded.Laboratory}
            onToggle={() => toggleCategory('Laboratory')}
            hasError={hasFieldError('Laboratory')}
            defaultConsultantId={lastConsultantId}
            onConsultantSelected={setLastConsultantId}
          />
          <ProcedureBillingCard
            expanded={expanded.Procedure}
            onToggle={() => toggleCategory('Procedure')}
            hasError={hasFieldError('Procedure')}
            defaultConsultantId={lastConsultantId}
            onConsultantSelected={setLastConsultantId}
          />
          <InjectionBillingCard expanded={expanded.Injection} onToggle={() => toggleCategory('Injection')} hasError={hasFieldError('Injection')} />
        </div>
        <BillingSummaryCard onSave={onSave} isSaving={isSaving} saveError={saveError} saveErrorDetails={saveErrorDetails} />
      </div>
    </FormProvider>
  );
});
