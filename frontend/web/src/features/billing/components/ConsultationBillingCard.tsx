import { Stethoscope } from 'lucide-react';
import { useEffect } from 'react';
import { Controller, useFormContext } from 'react-hook-form';
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
import { PaymentStatusControl } from './PaymentStatusControl';

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

  const departmentId = watch('consultation.departmentId');
  const consultantId = watch('consultation.consultantId');
  const consultationTypeId = watch('consultation.consultationTypeId');
  const discount = watch('consultation.discount');
  const charge = watch('consultation.charge');
  const paymentStatus = watch('consultation.paymentStatus');
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
  const isActive = isConsultationEntryActive({ departmentId, consultantId, consultationTypeId, paymentStatus, discount });

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
        <PaymentStatusControl
          id="consultation-payment-status"
          value={paymentStatus}
          onChange={(status) => setValue('consultation.paymentStatus', status, { shouldValidate: true })}
          error={err?.paymentStatus?.message}
        />
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
    </CollapsibleCard>
  );
}
