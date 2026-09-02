import { X } from 'lucide-react';
import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { Controller, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Field } from '@/features/patients/components/FormSection';
import { isSimpleServiceEntryActive } from '../billingActivity';
import { formatCurrency } from '../billingCalculations';
import { getServicePrice, type BillingService } from '../billingCatalog';
import { emptySimpleServiceRow, type BillingFormValues, type SimpleServiceBillingCategory } from '../billingValidation';
import { ChargeDisplay } from './ChargeDisplay';
import { CollapsibleCard } from './CollapsibleCard';

interface SimpleServiceBillingCardProps {
  category: SimpleServiceBillingCategory;
  title: string;
  description: string;
  icon: ReactNode;
  services: BillingService[];
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
  /** True while `services` is still loading from its source (e.g. an API-backed catalog) — disables the Service dropdown so it doesn't briefly render empty. */
  isLoadingServices?: boolean;
}

/**
 * Shared implementation behind InjectionBillingCard / FileBillingCard — a stripped-down sibling
 * of ServiceBillingCard for the two categories with no consultant/doctor involved: a list of
 * Service -> auto-priced charge -> discount rows, no Consultant field at all (per the user's
 * requirement — Injection/File billing never asks who administered/issued it).
 */
export function SimpleServiceBillingCard({
  category,
  title,
  description,
  icon,
  services,
  expanded,
  onToggle,
  hasError,
  isLoadingServices = false,
}: SimpleServiceBillingCardProps) {
  const { control } = useFormContext<BillingFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: category });
  const rows = useWatch({ control, name: category });

  const activeRows = (rows ?? []).filter(isSimpleServiceEntryActive);
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
      onAdd={() => append({ ...emptySimpleServiceRow })}
      addLabel={`Add another ${title.replace(' Billing', '').toLowerCase()}`}
    >
      {fields.map((field, index) => (
        <SimpleServiceBillingRow
          key={field.id}
          category={category}
          index={index}
          services={services}
          showRemove={fields.length > 1}
          onRemove={() => remove(index)}
          isLast={index === fields.length - 1}
          isLoadingServices={isLoadingServices}
        />
      ))}
    </CollapsibleCard>
  );
}

interface SimpleServiceBillingRowProps {
  category: SimpleServiceBillingCategory;
  index: number;
  services: BillingService[];
  showRemove: boolean;
  onRemove: () => void;
  isLast: boolean;
  isLoadingServices: boolean;
}

function SimpleServiceBillingRow({ category, index, services, showRemove, onRemove, isLast, isLoadingServices }: SimpleServiceBillingRowProps) {
  const {
    control,
    setValue,
    watch,
    formState: { errors },
  } = useFormContext<BillingFormValues>();
  const basePath = `${category}.${index}` as const;

  const serviceId = watch(`${basePath}.serviceId`);
  const charge = watch(`${basePath}.charge`);

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
