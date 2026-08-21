import { Plus, Stethoscope, X } from 'lucide-react';
import { useEffect } from 'react';
import { Controller, useController, useFieldArray, useFormContext, type Control } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Field } from '@/features/patients/components/FormSection';
import { isConsultationEntryActive } from '../billingActivity';
import { formatCurrency } from '../billingCalculations';
import { CONSULTATION_DEPARTMENTS, CONSULTATION_TYPES, getConsultantsForDepartment, getConsultationCharge } from '../billingCatalog';
import type { BillingFormValues } from '../billingValidation';
import { ChargeDisplay } from './ChargeDisplay';
import { CollapsibleCard } from './CollapsibleCard';
import { DiscountApprovalControl } from './DiscountApprovalControl';

interface ConsultationBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

/** Department → Consultant → Consultation Type → auto-calculated charge. The consultant list narrows to the selected department; changing department clears a now-invalid consultant. */
export function ConsultationBillingCard({ expanded, onToggle, hasError }: ConsultationBillingCardProps) {
  const {
    control,
    watch,
    setValue,
    formState: { errors },
  } = useFormContext<BillingFormValues>();

  // UI-only demo affordance — see additionalConsultations' own doc comment in
  // billingValidation.ts for why these rows never reach an actual invoice.
  const additionalConsultations = useFieldArray({ control, name: 'additionalConsultations' });

  const departmentId = watch('consultation.departmentId');
  const consultantId = watch('consultation.consultantId');
  const consultationTypeId = watch('consultation.consultationTypeId');
  const discount = watch('consultation.discount');
  const charge = watch('consultation.charge');
  const discountApproved = watch('consultation.discountApproved');
  const discountApprovedBy = watch('consultation.discountApprovedBy');

  const consultants = getConsultantsForDepartment(departmentId);

  useEffect(() => {
    const computed = getConsultationCharge(departmentId, consultantId, consultationTypeId);
    setValue('consultation.charge', computed, { shouldValidate: true });
  }, [departmentId, consultantId, consultationTypeId, setValue]);

  useEffect(() => {
    if (consultantId && !consultants.some((c) => c.id === consultantId)) {
      setValue('consultation.consultantId', '');
    }
    // Only re-check when the department (and thus the eligible consultant list) changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [departmentId]);

  const err = errors.consultation;
  const isActive = isConsultationEntryActive({ departmentId, consultantId, consultationTypeId, discount });

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
          <span className="text-sm font-semibold text-foreground">{formatCurrency(Math.max(charge - discount, 0))}</span>
        ) : undefined
      }
    >
      <div className="flex flex-wrap gap-3">
        <Field label="Department" htmlFor="consultation-department" error={err?.departmentId?.message} className="flex w-full flex-col gap-1 sm:w-56">
          <Controller
            name="consultation.departmentId"
            control={control}
            render={({ field }) => (
              <Select value={field.value || undefined} onValueChange={field.onChange}>
                <SelectTrigger id="consultation-department" aria-label="Department">
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
          htmlFor="consultation-consultant"
          error={err?.consultantId?.message}
          className="flex min-w-[200px] flex-1 flex-col gap-1"
        >
          <Controller
            name="consultation.consultantId"
            control={control}
            render={({ field }) => (
              <Select value={field.value || undefined} onValueChange={field.onChange} disabled={!departmentId}>
                <SelectTrigger id="consultation-consultant" aria-label="Consultant">
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
          htmlFor="consultation-type"
          error={err?.consultationTypeId?.message}
          className="flex w-full flex-col gap-1 sm:w-48"
        >
          <Controller
            name="consultation.consultationTypeId"
            control={control}
            render={({ field }) => (
              <Select value={field.value || undefined} onValueChange={field.onChange}>
                <SelectTrigger id="consultation-type" aria-label="Consultation type">
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
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <ChargeDisplay id="consultation-charge" amount={charge} label="Consultation charge" />
        <Field label="Discount (₹)" htmlFor="consultation-discount" error={err?.discount?.message} className="flex w-full flex-col gap-1 sm:w-36">
          <Controller
            name="consultation.discount"
            control={control}
            render={({ field }) => (
              <Input
                id="consultation-discount"
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
        id="consultation-discount-approved"
        approved={discountApproved}
        approvedBy={discountApprovedBy}
        discount={discount}
        onChange={(approved, approvedBy) => {
          setValue('consultation.discountApproved', approved);
          setValue('consultation.discountApprovedBy', approvedBy);
        }}
      />

      {additionalConsultations.fields.map((field, index) => (
        <AdditionalConsultationRow
          key={field.id}
          control={control}
          index={index}
          onRemove={() => additionalConsultations.remove(index)}
        />
      ))}

      <Button
        type="button"
        variant="outline"
        size="sm"
        className="w-fit gap-1.5"
        onClick={() =>
          additionalConsultations.append({
            departmentId: '',
            consultantId: '',
            consultationTypeId: '',
            charge: 0,
            discount: 0,
            discountApproved: false,
            discountApprovedBy: '',
          })
        }
      >
        <Plus className="h-4 w-4" />
        Add another Consultation
      </Button>
    </CollapsibleCard>
  );
}

interface AdditionalConsultationRowProps {
  control: Control<BillingFormValues>;
  index: number;
  onRemove: () => void;
}

/** One "Add another Consultation" row — its own Department/Consultant/Type/Charge/Discount,
 * independent of the primary consultation above and every other additional row. Mirrors the
 * primary card's own department-clears-consultant and auto-computed-charge behavior via
 * useController + a local effect, since this row has no form-level watch()/setValue() of
 * its own the way the primary fields do through useFormContext above. */
function AdditionalConsultationRow({ control, index, onRemove }: AdditionalConsultationRowProps) {
  const departmentField = useController({ control, name: `additionalConsultations.${index}.departmentId` as const });
  const consultantField = useController({ control, name: `additionalConsultations.${index}.consultantId` as const });
  const typeField = useController({ control, name: `additionalConsultations.${index}.consultationTypeId` as const });
  const chargeField = useController({ control, name: `additionalConsultations.${index}.charge` as const });
  const discountField = useController({ control, name: `additionalConsultations.${index}.discount` as const });

  const rowDepartmentId = departmentField.field.value;
  const rowConsultantId = consultantField.field.value;
  const rowTypeId = typeField.field.value;
  const rowConsultants = getConsultantsForDepartment(rowDepartmentId);

  useEffect(() => {
    chargeField.field.onChange(getConsultationCharge(rowDepartmentId, rowConsultantId, rowTypeId));
    // Only recompute when the inputs to the charge lookup change, not on every render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rowDepartmentId, rowConsultantId, rowTypeId]);

  useEffect(() => {
    if (rowConsultantId && !rowConsultants.some((c) => c.id === rowConsultantId)) {
      consultantField.field.onChange('');
    }
    // Only re-check when the department (and thus the eligible consultant list) changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rowDepartmentId]);

  return (
    <div className="flex flex-col gap-3 rounded-md border border-dashed border-border p-3">
      <div className="flex flex-wrap gap-3">
        <Field label="Department" htmlFor={`additional-consultation-department-${index}`} className="flex w-full flex-col gap-1 sm:w-56">
          <Select value={rowDepartmentId || undefined} onValueChange={departmentField.field.onChange}>
            <SelectTrigger id={`additional-consultation-department-${index}`} aria-label="Department">
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
        </Field>
        <Field
          label="Consultant"
          htmlFor={`additional-consultation-consultant-${index}`}
          className="flex min-w-[200px] flex-1 flex-col gap-1"
        >
          <Select value={rowConsultantId || undefined} onValueChange={consultantField.field.onChange} disabled={!rowDepartmentId}>
            <SelectTrigger id={`additional-consultation-consultant-${index}`} aria-label="Consultant">
              <SelectValue placeholder={rowDepartmentId ? 'Select consultant' : 'Select a department first'} />
            </SelectTrigger>
            <SelectContent>
              {rowConsultants.map((c) => (
                <SelectItem key={c.id} value={c.id}>
                  {c.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Field
          label="Consultation type"
          htmlFor={`additional-consultation-type-${index}`}
          className="flex w-full flex-col gap-1 sm:w-48"
        >
          <Select value={rowTypeId || undefined} onValueChange={typeField.field.onChange}>
            <SelectTrigger id={`additional-consultation-type-${index}`} aria-label="Consultation type">
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
        </Field>
      </div>

      <div className="flex flex-wrap items-end gap-3">
        <ChargeDisplay id={`additional-consultation-charge-${index}`} amount={chargeField.field.value} label="Consultation charge" />
        <Field label="Discount (₹)" htmlFor={`additional-consultation-discount-${index}`} className="flex w-full flex-col gap-1 sm:w-36">
          <Input
            id={`additional-consultation-discount-${index}`}
            type="number"
            min={0}
            max={chargeField.field.value}
            inputMode="decimal"
            value={discountField.field.value}
            onChange={(e) => discountField.field.onChange(e.target.value === '' ? 0 : Number(e.target.value))}
          />
        </Field>
        <Button type="button" variant="ghost" size="icon" aria-label="Remove consultation" onClick={onRemove}>
          <X className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
