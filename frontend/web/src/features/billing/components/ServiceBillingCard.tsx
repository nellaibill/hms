import { X } from 'lucide-react';
import { useEffect, useRef } from 'react';
import type { ReactNode } from 'react';
import { Controller, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Field } from '@/features/patients/components/FormSection';
import { isServiceEntryActive } from '../billingActivity';
import { formatCurrency } from '../billingCalculations';
import { getBedsideHospitalShare, getServiceRowPrice, type BillingService, type ServiceConsultant } from '../billingCatalog';
import { bedsideAmountTooLowMessage, emptyServiceRow, type BillingFormValues, type ServiceBillingCategory } from '../billingValidation';
import { ChargeDisplay } from './ChargeDisplay';
import { CollapsibleCard } from './CollapsibleCard';

/** The Rate dropdown's value for the service's standard rate (a Select item can't be ''). */
const STANDARD_RATE = '__standard__';

interface ServiceBillingCardProps {
  category: ServiceBillingCategory;
  title: string;
  description: string;
  icon: ReactNode;
  services: BillingService[];
  consultants: ServiceConsultant[];
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
  /** True while `services` is still loading from its source (e.g. an API-backed catalog) — disables the Service dropdown so it doesn't briefly render empty. */
  isLoadingServices?: boolean;
  /** Overrides the row's item-picker label (and its placeholder/search text) — defaults to
   * "Service", but e.g. Procedure Billing calls it "Procedure Name" since that's what a
   * receptionist is actually picking from that category's catalog. */
  serviceFieldLabel?: string;
  /** The last consultant picked across Radiology/Laboratory/Procedure (see BillingStep's own
   * comment) — seeds a row's Consultant field the moment it's created, if it doesn't already
   * have one. */
  defaultConsultantId?: string;
  /** Reports a row's consultant the moment it's picked, so BillingStep can remember it for the
   * next row added in any of these three categories. */
  onConsultantSelected?: (consultantId: string) => void;
}

/**
 * Shared implementation behind RadiologyBillingCard / LaboratoryBillingCard /
 * ProcedureBillingCard — the three categories are identical in shape (a list of Service →
 * Consultant → auto-priced charge → discount → payment status rows), so only the catalog
 * and labels differ. A category can hold several rows (multiple lab tests, multiple
 * procedures in one visit) via `useFieldArray`, starting with one empty row.
 */
export function ServiceBillingCard({
  category,
  title,
  description,
  icon,
  services,
  consultants,
  expanded,
  onToggle,
  hasError,
  isLoadingServices = false,
  serviceFieldLabel = 'Service',
  defaultConsultantId,
  onConsultantSelected,
}: ServiceBillingCardProps) {
  const { control } = useFormContext<BillingFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: category });
  const rows = useWatch({ control, name: category });

  const activeRows = (rows ?? []).filter(isServiceEntryActive);
  const isActive = activeRows.length > 0;
  const categoryTotal = activeRows.reduce((sum, row) => sum + Math.max(row.quantity * row.charge - row.discount, 0), 0);

  return (
    <CollapsibleCard
      id={`billing-${category}`}
      title={title}
      description={description}
      icon={icon}
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
      onAdd={() => append({ ...emptyServiceRow })}
      addLabel={`Add another ${title.replace(' Billing', '').toLowerCase()}`}
    >
      {fields.map((field, index) => (
        <ServiceBillingRow
          key={field.id}
          category={category}
          index={index}
          services={services}
          consultants={consultants}
          showRemove={fields.length > 1}
          onRemove={() => remove(index)}
          isLast={index === fields.length - 1}
          isLoadingServices={isLoadingServices}
          serviceFieldLabel={serviceFieldLabel}
          defaultConsultantId={defaultConsultantId}
          onConsultantSelected={onConsultantSelected}
        />
      ))}
    </CollapsibleCard>
  );
}

interface ServiceBillingRowProps {
  category: ServiceBillingCategory;
  index: number;
  services: BillingService[];
  consultants: ServiceConsultant[];
  showRemove: boolean;
  onRemove: () => void;
  isLast: boolean;
  isLoadingServices: boolean;
  serviceFieldLabel: string;
  defaultConsultantId?: string;
  onConsultantSelected?: (consultantId: string) => void;
}

function ServiceBillingRow({
  category,
  index,
  services,
  consultants,
  showRemove,
  onRemove,
  isLast,
  isLoadingServices,
  serviceFieldLabel,
  defaultConsultantId,
  onConsultantSelected,
}: ServiceBillingRowProps) {
  const {
    control,
    setValue,
    watch,
    getValues,
    setError,
    clearErrors,
    formState: { errors },
  } = useFormContext<BillingFormValues>();
  const basePath = `${category}.${index}` as const;

  const serviceId = watch(`${basePath}.serviceId`);
  const rateOptionId = watch(`${basePath}.rateOptionId`);
  const isBedside = watch(`${basePath}.isBedside`);
  const bedsideAmount = watch(`${basePath}.bedsideAmount`);
  const charge = watch(`${basePath}.charge`);
  const selectedService = services.find((s) => s.id === serviceId);
  const rateOptions = selectedService?.rateOptions ?? [];
  const bedsidePrice = selectedService?.bedsidePrice;
  // ADR-144: the least the bedside add-on can be charged at — anything above it is the consultant's.
  const bedsideHospitalShare = getBedsideHospitalShare(selectedService);
  const bedsideConsultantShare = Math.max((Number(bedsideAmount) || 0) - (bedsideHospitalShare ?? 0), 0);

  // ADR-140: picking another service starts its rate and bedside choice afresh. Skipped on the
  // first render, so a row that already holds a choice keeps it.
  const previousServiceId = useRef(serviceId);
  useEffect(() => {
    if (previousServiceId.current === serviceId) return;
    previousServiceId.current = serviceId;
    setValue(`${basePath}.rateOptionId`, '');
    setValue(`${basePath}.isBedside`, false);
    setValue(`${basePath}.bedsideAmount`, 0);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [serviceId, setValue]);

  // ADR-144: the row keeps the service's hospital share of the bedside add-on, so the form's
  // schema can refuse an amount below it.
  useEffect(() => {
    setValue(`${basePath}.bedsideHospitalShare`, bedsideHospitalShare ?? 0);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [bedsideHospitalShare, setValue]);

  useEffect(() => {
    setValue(`${basePath}.charge`, getServiceRowPrice(services, serviceId, rateOptionId, isBedside, Number(bedsideAmount) || 0), { shouldValidate: true });
    // `services`/`basePath` are stable per row instance — only the selected service, rate and bedside choice/amount recompute the price.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [serviceId, rateOptionId, isBedside, bedsideAmount, services, setValue]);

  useEffect(() => {
    // Fills this row's Consultant with the remembered one (the visit's doctor, or the last
    // consultant picked in another row) the moment a service is chosen — never while the row
    // is still blank, because a blank row with a consultant counts as in use and would then
    // demand a service on a bill that doesn't need one. Only ever fills an empty field, so a
    // consultant the receptionist chose is never overwritten.
    if (serviceId && defaultConsultantId && !getValues(`${basePath}.consultantId`)) {
      setValue(`${basePath}.consultantId`, defaultConsultantId, { shouldValidate: true });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [serviceId]);

  const serviceOptions = services.map((s) => ({ value: s.id, label: `${s.name} — ${formatCurrency(s.price)}`, keywords: s.name }));
  const rowErrors = errors[category]?.[index];

  return (
    <div className={showRemove || !isLast ? 'flex flex-col gap-4 border-b border-dashed border-border pb-4' : 'flex flex-col gap-4'}>
      <div className="flex flex-wrap items-start gap-3">
        <Field
          label={serviceFieldLabel}
          htmlFor={`${basePath}-service`}
          error={rowErrors?.serviceId?.message}
          className="flex min-w-[220px] flex-1 flex-col gap-1"
        >
          <Controller
            name={`${basePath}.serviceId`}
            control={control}
            render={({ field }) => (
              <SearchableSelect
                id={`${basePath}-service`}
                ariaLabel={serviceFieldLabel}
                value={field.value}
                onValueChange={field.onChange}
                options={serviceOptions}
                placeholder={isLoadingServices ? 'Loading…' : `Select ${serviceFieldLabel.toLowerCase()}`}
                searchPlaceholder={`Search ${serviceFieldLabel.toLowerCase()}…`}
                disabled={isLoadingServices}
              />
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
              <Select
                value={field.value || undefined}
                onValueChange={(value) => {
                  field.onChange(value);
                  onConsultantSelected?.(value);
                }}
              >
                <SelectTrigger id={`${basePath}-consultant`} aria-label="Consultant">
                  <SelectValue placeholder="Select consultant" />
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
        {rateOptions.length > 0 && (
          <Field label="Rate" htmlFor={`${basePath}-rate`} className="flex min-w-[180px] flex-col gap-1">
            <Controller
              name={`${basePath}.rateOptionId`}
              control={control}
              render={({ field }) => (
                <Select value={field.value || STANDARD_RATE} onValueChange={(value) => field.onChange(value === STANDARD_RATE ? '' : value)}>
                  <SelectTrigger id={`${basePath}-rate`} aria-label="Rate">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={STANDARD_RATE}>Standard — {formatCurrency(selectedService?.price ?? 0)}</SelectItem>
                    {rateOptions.map((option) => (
                      <SelectItem key={option.id} value={option.id}>
                        {option.name} — {formatCurrency(option.price)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </Field>
        )}
        {bedsidePrice != null && (
          <div className="flex flex-col gap-1">
            <span className="text-sm font-medium leading-none text-foreground">Bedside</span>
            <Controller
              name={`${basePath}.isBedside`}
              control={control}
              render={({ field }) => (
                <label className="flex h-10 cursor-pointer items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    aria-label="Bedside"
                    className="h-5 w-5 accent-primary"
                    checked={field.value}
                    onChange={(event) => {
                      // ADR-144: a bedside scan starts at the service's bedside price; the
                      // counter can change it in the Bedside amount field that appears.
                      if (event.target.checked) setValue(`${basePath}.bedsideAmount`, bedsidePrice);
                      clearErrors(`${basePath}.bedsideAmount`);
                      field.onChange(event.target.checked);
                    }}
                  />
                  {!field.value && `+${formatCurrency(bedsidePrice)}`}
                </label>
              )}
            />
          </div>
        )}
        {bedsidePrice != null && isBedside && (
          <Field
            label="Bedside amount (₹)"
            htmlFor={`${basePath}-bedside-amount`}
            error={rowErrors?.bedsideAmount?.message}
            className="flex w-full flex-col gap-1 sm:w-48"
          >
            <Controller
              name={`${basePath}.bedsideAmount`}
              control={control}
              render={({ field }) => (
                <Input
                  id={`${basePath}-bedside-amount`}
                  type="number"
                  min={bedsideHospitalShare ?? 0}
                  inputMode="decimal"
                  aria-describedby={`${basePath}-bedside-split`}
                  value={field.value}
                  // Same select-on-focus and leading-zero handling as the consultation charge
                  // field, so clearing the amount and typing a new one doesn't leave "0600".
                  onFocus={(e) => e.target.select()}
                  onChange={(e) => {
                    let raw = e.target.value;
                    if (/^0+(?=\d)/.test(raw)) {
                      raw = raw.replace(/^0+/, '');
                      e.target.value = raw;
                    }
                    const amount = raw === '' ? 0 : Number(raw);
                    field.onChange(amount);
                    // Checked as it's typed, not only on Save: the rest of the form waits for the
                    // user to try to move on (see BillingStep), but an amount below the
                    // hospital's share is flagged straight away.
                    const hospitalShare = bedsideHospitalShare ?? 0;
                    if (amount < hospitalShare) {
                      setError(`${basePath}.bedsideAmount`, { type: 'manual', message: bedsideAmountTooLowMessage(hospitalShare) });
                    } else {
                      clearErrors(`${basePath}.bedsideAmount`);
                    }
                  }}
                  onBlur={field.onBlur}
                />
              )}
            />
            <p id={`${basePath}-bedside-split`} className="text-xs text-muted-foreground">
              Hospital {formatCurrency(bedsideHospitalShare ?? 0)} · Consultant {formatCurrency(bedsideConsultantShare)}
            </p>
          </Field>
        )}
        <ChargeDisplay id={`${basePath}-charge`} amount={charge} />
        {showRemove && (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Remove this item"
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
