import { Plus, X } from 'lucide-react';
import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { Controller, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Field } from '@/features/patients/components/FormSection';
import { isServiceEntryActive } from '../billingActivity';
import { formatCurrency } from '../billingCalculations';
import { getServicePrice, type BillingService, type ServiceConsultant } from '../billingCatalog';
import { emptyServiceRow, type BillingFormValues, type ServiceBillingCategory } from '../billingValidation';
import { ChargeDisplay } from './ChargeDisplay';
import { CollapsibleCard } from './CollapsibleCard';
import { DiscountApprovalControl } from './DiscountApprovalControl';

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
}: ServiceBillingCardProps) {
  const { control } = useFormContext<BillingFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: category });
  const rows = useWatch({ control, name: category });

  const activeRows = (rows ?? []).filter(isServiceEntryActive);
  const isActive = activeRows.length > 0;
  const categoryTotal = activeRows.reduce((sum, row) => sum + Math.max(row.charge - row.discount, 0), 0);

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
        />
      ))}
      <Button type="button" variant="outline" size="sm" className="w-fit gap-1.5" onClick={() => append({ ...emptyServiceRow })}>
        <Plus className="h-4 w-4" />
        Add another {title.replace(' Billing', '').toLowerCase()}
      </Button>
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
}

function ServiceBillingRow({ category, index, services, consultants, showRemove, onRemove, isLast, isLoadingServices }: ServiceBillingRowProps) {
  const {
    control,
    setValue,
    watch,
    formState: { errors },
  } = useFormContext<BillingFormValues>();
  const basePath = `${category}.${index}` as const;

  const serviceId = watch(`${basePath}.serviceId`);
  const discount = watch(`${basePath}.discount`);
  const charge = watch(`${basePath}.charge`);
  const discountApproved = watch(`${basePath}.discountApproved`);
  const discountApprovedBy = watch(`${basePath}.discountApprovedBy`);

  useEffect(() => {
    setValue(`${basePath}.charge`, getServicePrice(services, serviceId), { shouldValidate: true });
    // `services`/`basePath` are stable per row instance — only the selected service should recompute the price.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [serviceId, services, setValue]);

  const serviceOptions = services.map((s) => ({ value: s.id, label: `${s.name} — ${formatCurrency(s.price)}`, keywords: s.name }));
  const rowErrors = errors[category]?.[index];

  return (
    <div className={showRemove || !isLast ? 'flex flex-col gap-4 border-b border-dashed border-border pb-4' : 'flex flex-col gap-4'}>
      <div className="flex flex-wrap items-start gap-3">
        <Field
          label="Service"
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
                ariaLabel="Service"
                value={field.value}
                onValueChange={field.onChange}
                options={serviceOptions}
                placeholder={isLoadingServices ? 'Loading services…' : 'Select service'}
                searchPlaceholder="Search services…"
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
              <Select value={field.value || undefined} onValueChange={field.onChange}>
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

      <div className="flex flex-wrap items-end gap-3">
        <ChargeDisplay id={`${basePath}-charge`} amount={charge} />
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
