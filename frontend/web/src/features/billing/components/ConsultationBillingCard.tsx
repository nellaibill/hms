import { Plus, Stethoscope, X } from 'lucide-react';
import { useEffect, useRef } from 'react';
import { Controller, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Field } from '@/features/patients/components/FormSection';
import { isConsultationEntryActive } from '../billingActivity';
import { formatCurrency } from '../billingCalculations';
import { CONSULTATION_DEPARTMENTS, CONSULTATION_TYPES, getConsultantsForDepartment, getConsultationCharge } from '../billingCatalog';
import { emptyConsultation, type BillingFormValues } from '../billingValidation';
import { ChargeDisplay } from './ChargeDisplay';
import { CollapsibleCard } from './CollapsibleCard';
import { DiscountApprovalControl } from './DiscountApprovalControl';

interface ConsultationBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
  /** Registration Details' selected Department/Consultant, by name — see BillingStep's prop
   * doc for why this is a name (not an id) hint. */
  initialDepartmentName?: string;
  initialConsultantName?: string;
}

/**
 * Department → Consultant → Consultation Type → auto-calculated charge, one row per
 * consultation. Mirrors ServiceBillingCard's `useFieldArray` "Add another" pattern — a
 * visit can need more than one consultation (a multi-specialty referral, or a follow-up
 * plus a new consult in the same visit), the same way Radiology/Laboratory/Procedure
 * already support multiple entries.
 */
export function ConsultationBillingCard({
  expanded,
  onToggle,
  hasError,
  initialDepartmentName,
  initialConsultantName,
}: ConsultationBillingCardProps) {
  const { control, setValue } = useFormContext<BillingFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: 'consultation' });
  const rows = useWatch({ control, name: 'consultation' });

  const activeRows = (rows ?? []).filter(isConsultationEntryActive);
  const isActive = activeRows.length > 0;
  const categoryTotal = activeRows.reduce((sum, row) => sum + Math.max(row.charge - row.discount, 0), 0);

  // Best-effort carry-forward from Registration Details, applied at most once. Billing's
  // Consultation catalog (CONSULTATION_DEPARTMENTS/CONSULTATION_CONSULTANTS) is a separate
  // mock dataset with its own id space — not the real Masters ids Registration Details uses
  // (see billingCatalog.ts's top comment) — so there's no shared id to carry forward
  // directly. This matches by name instead: if a same-named department/consultant exists in
  // the mock catalog, prefill row 0; if not (a real possibility, since the mock catalog is a
  // small hardcoded list), leave the row blank for the user to fill in themselves. Only fires
  // while the consultation array is still the untouched single empty default, so it can never
  // overwrite a restored draft or something the user already started editing.
  const appliedPrefillRef = useRef(false);
  useEffect(() => {
    if (appliedPrefillRef.current) return;
    if (!initialDepartmentName && !initialConsultantName) return;
    const isPristine = rows.length === 1 && !isConsultationEntryActive(rows[0]);
    if (!isPristine) return;

    appliedPrefillRef.current = true;
    const matchedDepartment = initialDepartmentName
      ? CONSULTATION_DEPARTMENTS.find((d) => d.name.toLowerCase() === initialDepartmentName.toLowerCase())
      : undefined;
    if (!matchedDepartment) return;

    setValue('consultation.0.departmentId', matchedDepartment.id);
    if (initialConsultantName) {
      const matchedConsultant = getConsultantsForDepartment(matchedDepartment.id).find(
        (c) => c.name.toLowerCase() === initialConsultantName.toLowerCase(),
      );
      if (matchedConsultant) {
        setValue('consultation.0.consultantId', matchedConsultant.id);
      }
    }
  }, [initialDepartmentName, initialConsultantName, rows, setValue]);

  return (
    <CollapsibleCard
      id="billing-consultation"
      title="Consultation Billing"
      description="OP / IP consultation charge for this visit."
      icon={<Stethoscope className="h-5 w-5" />}
      expanded={expanded}
      onToggle={onToggle}
      hasError={hasError}
      summary={
        !expanded && isActive ? (
          <span className="text-sm font-semibold text-foreground">
            {formatCurrency(categoryTotal)}
            {activeRows.length > 1 ? ` · ${activeRows.length} items` : ''}
          </span>
        ) : undefined
      }
    >
      {fields.map((field, index) => (
        <ConsultationBillingRow key={field.id} index={index} showRemove={fields.length > 1} onRemove={() => remove(index)} isLast={index === fields.length - 1} />
      ))}
      <Button type="button" variant="outline" size="sm" className="w-fit gap-1.5" onClick={() => append({ ...emptyConsultation })}>
        <Plus className="h-4 w-4" />
        Add another consultation
      </Button>
    </CollapsibleCard>
  );
}

interface ConsultationBillingRowProps {
  index: number;
  showRemove: boolean;
  onRemove: () => void;
  isLast: boolean;
}

function ConsultationBillingRow({ index, showRemove, onRemove, isLast }: ConsultationBillingRowProps) {
  const {
    control,
    watch,
    setValue,
    formState: { errors },
  } = useFormContext<BillingFormValues>();
  const basePath = `consultation.${index}` as const;

  const departmentId = watch(`${basePath}.departmentId`);
  const consultantId = watch(`${basePath}.consultantId`);
  const consultationTypeId = watch(`${basePath}.consultationTypeId`);
  const discount = watch(`${basePath}.discount`);
  const charge = watch(`${basePath}.charge`);
  const discountApproved = watch(`${basePath}.discountApproved`);
  const discountApprovedBy = watch(`${basePath}.discountApprovedBy`);

  const consultants = getConsultantsForDepartment(departmentId);

  useEffect(() => {
    const computed = getConsultationCharge(departmentId, consultantId, consultationTypeId);
    setValue(`${basePath}.charge`, computed, { shouldValidate: true });
    // `basePath`/`setValue` are stable per row instance — only the selected department/consultant/type should recompute the charge.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [departmentId, consultantId, consultationTypeId, setValue]);

  useEffect(() => {
    if (consultantId && !consultants.some((c) => c.id === consultantId)) {
      setValue(`${basePath}.consultantId`, '');
    }
    // Only re-check when the department (and thus the eligible consultant list) changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [departmentId]);

  const rowErrors = errors.consultation?.[index];

  return (
    <div className={showRemove || !isLast ? 'flex flex-col gap-4 border-b border-dashed border-border pb-4' : 'flex flex-col gap-4'}>
      <div className="flex flex-wrap items-start gap-3">
        <Field
          label="Department"
          htmlFor={`${basePath}-department`}
          error={rowErrors?.departmentId?.message}
          className="flex w-full flex-col gap-1 sm:w-56"
        >
          <Controller
            name={`${basePath}.departmentId`}
            control={control}
            render={({ field }) => (
              <Select value={field.value || undefined} onValueChange={field.onChange}>
                <SelectTrigger id={`${basePath}-department`} aria-label="Department">
                  <SelectValue placeholder="Select department" />
                </SelectTrigger>
                <SelectContent>
                  {CONSULTATION_DEPARTMENTS.map((d) => (
                    <SelectItem key={d.id} value={d.id}>
                      {d.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </Field>
        <Field
          label="Consultant"
          htmlFor={`${basePath}-consultant`}
          error={rowErrors?.consultantId?.message}
          className="flex min-w-[200px] flex-1 flex-col gap-1"
        >
          <Controller
            name={`${basePath}.consultantId`}
            control={control}
            render={({ field }) => (
              <Select value={field.value || undefined} onValueChange={field.onChange} disabled={!departmentId}>
                <SelectTrigger id={`${basePath}-consultant`} aria-label="Consultant">
                  <SelectValue placeholder={departmentId ? 'Select consultant' : 'Select a department first'} />
                </SelectTrigger>
                <SelectContent>
                  {consultants.map((c) => (
                    <SelectItem key={c.id} value={c.id}>
                      {c.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </Field>
        <Field
          label="Consultation type"
          htmlFor={`${basePath}-type`}
          error={rowErrors?.consultationTypeId?.message}
          className="flex w-full flex-col gap-1 sm:w-48"
        >
          <Controller
            name={`${basePath}.consultationTypeId`}
            control={control}
            render={({ field }) => (
              <Select value={field.value || undefined} onValueChange={field.onChange}>
                <SelectTrigger id={`${basePath}-type`} aria-label="Consultation type">
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {CONSULTATION_TYPES.map((t) => (
                    <SelectItem key={t.id} value={t.id}>
                      {t.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
        </Field>
        {showRemove && (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Remove this consultation"
            className="mt-6 shrink-0"
            onClick={onRemove}
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <ChargeDisplay id={`${basePath}-charge`} amount={charge} label="Consultation charge" />
        <Field label="Discount (₹)" htmlFor={`${basePath}-discount`} error={rowErrors?.discount?.message} className="flex w-full flex-col gap-1 sm:w-36">
          <Controller
            name={`${basePath}.discount`}
            control={control}
            render={({ field }) => (
              <Input
                id={`${basePath}-discount`}
                type="number"
                min={0}
                max={charge}
                inputMode="decimal"
                value={field.value}
                onChange={(e) => field.onChange(e.target.value === '' ? 0 : Number(e.target.value))}
              />
            )}
          />
        </Field>
      </div>
      <DiscountApprovalControl
        id={`${basePath}-discount-approved`}
        approved={discountApproved}
        approvedBy={discountApprovedBy}
        discount={discount}
        onChange={(approved, approvedBy) => {
          setValue(`${basePath}.discountApproved`, approved);
          setValue(`${basePath}.discountApprovedBy`, approvedBy);
        }}
      />
    </div>
  );
}
