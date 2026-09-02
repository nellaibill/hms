import { useQuery } from '@tanstack/react-query';
import { Stethoscope, X } from 'lucide-react';
import { useEffect } from 'react';
import { Controller, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { ConsultationTypeSelect } from '@/components/ConsultationTypeSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { Input } from '@/components/ui/input';
import { Field } from '@/features/patients/components/FormSection';
import { consultationTypesApi } from '@/services/apiClient';
import { isConsultationEntryActive } from '../billingActivity';
import { formatCurrency } from '../billingCalculations';
import { emptyConsultation, type BillingFormValues } from '../billingValidation';
import { CollapsibleCard } from './CollapsibleCard';

interface ConsultationBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

/**
 * One or more Department → Consultant → Consultation Type rows. Charge defaults to the
 * selected Consultation Type's real standard fee but is directly editable — real-world cases
 * (a waived fee, a negotiated corporate/insurance rate) need a genuinely different charge, not
 * a Discount against the standard one, which would otherwise drag in the discount-approval
 * flow for something that isn't actually a discount. Department/Consultant stay pure
 * attribution (who saw the patient, where). Mirrors ServiceBillingCard's row/array pattern
 * used by Radiology/Laboratory/Procedure, so a visit seen by more than one specialist can bill
 * more than one consultation (previously a "demo affordance" row that silently never reached
 * the saved invoice).
 */
export function ConsultationBillingCard({ expanded, onToggle, hasError }: ConsultationBillingCardProps) {
  const { control } = useFormContext<BillingFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: 'consultation' });
  const rows = useWatch({ control, name: 'consultation' });

  const activeRows = (rows ?? []).filter(isConsultationEntryActive);
  const isActive = activeRows.length > 0;
  const categoryTotal = activeRows.reduce((sum, row) => sum + Math.max(row.quantity * row.charge - row.discount, 0), 0);

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
      onAdd={() => append({ ...emptyConsultation })}
      addLabel="Add another Consultation"
    >
      {fields.map((field, index) => (
        <ConsultationBillingRow
          key={field.id}
          index={index}
          showRemove={fields.length > 1}
          onRemove={() => remove(index)}
          isLast={index === fields.length - 1}
        />
      ))}
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
    setValue,
    watch,
    formState: { errors },
  } = useFormContext<BillingFormValues>();
  const basePath = `consultation.${index}` as const;

  const departmentId = watch(`${basePath}.departmentId`);
  const consultationTypeId = watch(`${basePath}.consultationTypeId`);
  const fromVisit = watch(`${basePath}.fromVisit`);

  // Same query key ConsultationTypeSelect uses below, so this is a cache read, not an extra
  // request — just here to look up the selected type's real fee for the charge effect.
  const { data: consultationTypes } = useQuery({
    queryKey: ['consultationTypes', 'select-list'],
    queryFn: () => consultationTypesApi.getConsultationTypes({ pageSize: 100, isActive: true }),
  });

  useEffect(() => {
    const selectedType = consultationTypes?.items.find((t) => t.id === consultationTypeId);
    // Types like "Doctor's Consultation - Others/On-call" have no fixed master amount
    // (amount: null, "Amount to be filled" — see ConsultationTypeSelect) — leave the charge
    // alone for those so staff can type the actual amount, instead of forcing it to 0. Also
    // deliberately depends only on consultationTypeId, not `consultationTypes` itself: the
    // query object gets a new reference on every refetch (focus, cache invalidation from an
    // unrelated mutation, etc.), and re-running this on that alone used to stomp whatever the
    // user had just typed back to the master default on every such refetch.
    if (selectedType?.amount != null) {
      setValue(`${basePath}.charge`, selectedType.amount, { shouldValidate: true });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [consultationTypeId, setValue]);

  const rowErrors = errors.consultation?.[index];

  return (
    <div className={showRemove || !isLast ? 'flex flex-col gap-4 border-b border-dashed border-border pb-4' : 'flex flex-col gap-4'}>
      <div className="flex flex-wrap items-start gap-3">
        <Field
          label={fromVisit ? 'Department (from registration)' : 'Department'}
          htmlFor={`${basePath}-department`}
          error={rowErrors?.departmentId?.message}
          className="flex w-full flex-col gap-1 sm:w-56"
        >
          <Controller
            name={`${basePath}.departmentId`}
            control={control}
            render={({ field }) => (
              <DepartmentSelect
                id={`${basePath}-department`}
                value={field.value}
                onValueChange={(value) => {
                  field.onChange(value);
                  // Clearing the consultant when its department changes matches
                  // PatientRegistrationForm's identical Department→Consultant behavior — a
                  // consultant scoped to the old department isn't valid for the new one.
                  setValue(`${basePath}.consultantId`, '');
                }}
                // Deliberately NOT locked even when fromVisit — OPD Billing Entry bills any
                // existing visit, not just the one right after registration, so the patient's
                // last recorded department/consultant can legitimately differ from who they're
                // actually seeing today. The "(from registration)" label above is enough of a
                // hint; forcing staff to bill the wrong doctor because the field was locked was
                // a real problem, not a safeguard. Same reasoning already applied to
                // Consultation Type below — this just extends it consistently.
              />
            )}
          />
        </Field>
        <Field
          label={fromVisit ? 'Consultant (from registration)' : 'Consultant'}
          htmlFor={`${basePath}-consultant`}
          error={rowErrors?.consultantId?.message}
          className="flex min-w-[200px] flex-1 flex-col gap-1"
        >
          <Controller
            name={`${basePath}.consultantId`}
            control={control}
            render={({ field }) => (
              <ConsultantSelect
                id={`${basePath}-consultant`}
                value={field.value}
                onValueChange={field.onChange}
                departmentId={departmentId || undefined}
              />
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
            render={({ field }) => <ConsultationTypeSelect id={`${basePath}-type`} value={field.value} onValueChange={field.onChange} />}
          />
        </Field>
        <Field
          label="Consultation charge (₹)"
          htmlFor={`${basePath}-charge`}
          error={rowErrors?.charge?.message}
          className="flex w-full flex-col gap-1 sm:w-40"
        >
          <Controller
            name={`${basePath}.charge`}
            control={control}
            render={({ field }) => (
              <Input
                id={`${basePath}-charge`}
                type="number"
                min={0}
                inputMode="decimal"
                value={field.value}
                onChange={(e) => field.onChange(e.target.value === '' ? 0 : Number(e.target.value))}
              />
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
    </div>
  );
}
