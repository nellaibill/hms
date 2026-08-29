import type { DiagnosticPackage } from '@hms/shared';
import { FlaskConical, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Controller, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { SearchableSelect, type SearchableSelectOption } from '@/components/ui/searchable-select';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { primeDiagnosticPackageCache, useDiagnosticPackagesQuery, useDiagnosticServices, type BillingServiceOption } from '@/features/diagnostics';
import { Field } from '@/features/patients/components/FormSection';
import { isLaboratoryEntryActive } from '../billingActivity';
import { formatCurrency } from '../billingCalculations';
import type { ServiceConsultant } from '../billingCatalog';
import { emptyLaboratoryRow, type BillingFormValues } from '../billingValidation';
import { useAllActiveConsultants } from '../hooks/useAllActiveConsultants';
import { ChargeDisplay } from './ChargeDisplay';
import { CollapsibleCard } from './CollapsibleCard';

interface LaboratoryBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

/** `svc:<id>` / `pkg:<id>` — disambiguates a single flat SearchableSelect's value into
 * itemType/itemId (parsed back out in LaboratoryBillingRow's onValueChange). */
function toOptionValue(itemType: 'service' | 'package', itemId: string): string {
  return `${itemType === 'package' ? 'pkg' : 'svc'}:${itemId}`;
}

function parseOptionValue(value: string): { itemType: 'service' | 'package'; itemId: string } | null {
  if (value.startsWith('svc:')) return { itemType: 'service', itemId: value.slice(4) };
  if (value.startsWith('pkg:')) return { itemType: 'package', itemId: value.slice(4) };
  return null;
}

function findPrice(services: BillingServiceOption[], packages: DiagnosticPackage[], itemType: 'service' | 'package', itemId: string): number {
  if (itemType === 'package') return packages.find((p) => p.id === itemId)?.totalPrice ?? 0;
  return services.find((s) => s.id === itemId)?.price ?? 0;
}

/**
 * Laboratory Billing's own card — no longer a thin wrapper around the shared ServiceBillingCard
 * (see ServiceBillingCard.tsx, still used by Radiology/Procedure): a row here can pick either a
 * DiagnosticService or a DiagnosticPackage, mixed into one flat SearchableSelect
 * (useDiagnosticServices('Laboratory') + useDiagnosticPackagesQuery({isActive:true})), values
 * prefixed svc:/pkg: to disambiguate on selection. Judgment call: the mockup's "Selected Items"
 * table (Item Type/Item Name/Details/Unit Price/Qty/Amount/Actions) is rendered here as this
 * card's own row list (matching every other billing category's row-based editing UI, e.g.
 * ServiceBillingRow) rather than a separate read-only summary table — BillingSummaryCard
 * already provides the cross-category summary, and a second table would just duplicate these
 * same rows with its own remove/edit affordances.
 */
export function LaboratoryBillingCard({ expanded, onToggle, hasError }: LaboratoryBillingCardProps) {
  const { control } = useFormContext<BillingFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: 'laboratory' });
  const rows = useWatch({ control, name: 'laboratory' });
  const { services, isLoading: isLoadingServices } = useDiagnosticServices('Laboratory');
  const packagesQuery = useDiagnosticPackagesQuery({ isActive: true, pageSize: 200, sort: 'name' });
  const packages = packagesQuery.data?.items ?? [];
  const { consultants } = useAllActiveConsultants();

  // Primes the diagnostics reference cache describeBillingItem reads from (see
  // referenceCache.ts) — useDiagnosticServices already primes the service half itself.
  useEffect(() => {
    if (packagesQuery.data?.items) primeDiagnosticPackageCache(packagesQuery.data.items);
  }, [packagesQuery.data]);

  const activeRows = (rows ?? []).filter(isLaboratoryEntryActive);
  const isActive = activeRows.length > 0;
  const categoryTotal = activeRows.reduce((sum, row) => sum + Math.max(row.quantity * row.charge - row.discount, 0), 0);

  // Split rather than one combined list — each row's Services/Packages toggle picks which of
  // these two the SearchableSelect actually shows (see LaboratoryBillingRow below).
  const serviceOptions: SearchableSelectOption[] = services.map((service) => ({
    value: toOptionValue('service', service.id),
    label: `${service.name} — ${formatCurrency(service.price)}`,
    keywords: service.name,
  }));
  const packageOptions: SearchableSelectOption[] = packages.map((pkg) => ({
    value: toOptionValue('package', pkg.id),
    label: `${pkg.name} — ${formatCurrency(pkg.totalPrice)}`,
    keywords: pkg.name,
  }));

  const isLoadingItems = isLoadingServices || packagesQuery.isPending;

  return (
    <CollapsibleCard
      id="billing-laboratory"
      title="Laboratory Billing"
      description="Pathology and diagnostic lab tests (individually or as packages) for this visit."
      icon={<FlaskConical className="h-5 w-5" />}
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
      onAdd={() => append({ ...emptyLaboratoryRow })}
      addLabel="Add another laboratory item"
    >
      {fields.map((field, index) => (
        <LaboratoryBillingRow
          key={field.id}
          index={index}
          serviceOptions={serviceOptions}
          packageOptions={packageOptions}
          services={services}
          packages={packages}
          consultants={consultants}
          showRemove={fields.length > 1}
          onRemove={() => remove(index)}
          isLast={index === fields.length - 1}
          isLoadingItems={isLoadingItems}
        />
      ))}
    </CollapsibleCard>
  );
}

interface LaboratoryBillingRowProps {
  index: number;
  serviceOptions: SearchableSelectOption[];
  packageOptions: SearchableSelectOption[];
  services: BillingServiceOption[];
  packages: DiagnosticPackage[];
  consultants: ServiceConsultant[];
  showRemove: boolean;
  onRemove: () => void;
  isLast: boolean;
  isLoadingItems: boolean;
}

function LaboratoryBillingRow({
  index,
  serviceOptions,
  packageOptions,
  services,
  packages,
  consultants,
  showRemove,
  onRemove,
  isLast,
  isLoadingItems,
}: LaboratoryBillingRowProps) {
  const {
    control,
    setValue,
    watch,
    formState: { errors },
  } = useFormContext<BillingFormValues>();
  const basePath = `laboratory.${index}` as const;

  const itemType = watch(`${basePath}.itemType`);
  const itemId = watch(`${basePath}.itemId`);
  // Which of the two lists this row's item picker currently shows — defaults to whatever the
  // row's real selection already is (so an existing package row reopens showing Packages, not
  // Services), 'service' for a brand-new blank row. Switching it clears the current selection
  // if that selection no longer matches the newly-picked type (a stale package id sitting under
  // "Services" would otherwise look selected but be invisible in the filtered list).
  const [filterType, setFilterType] = useState<'service' | 'package'>(itemType === 'package' ? 'package' : 'service');
  const filteredOptions = filterType === 'package' ? packageOptions : serviceOptions;

  function handleFilterTypeChange(next: 'service' | 'package') {
    if (next === filterType) return;
    setFilterType(next);
    if (itemId && itemType !== next) {
      setValue(`${basePath}.itemType`, next);
      setValue(`${basePath}.itemId`, '');
    }
  }
  const discount = watch(`${basePath}.discount`);
  const charge = watch(`${basePath}.charge`);
  const quantity = watch(`${basePath}.quantity`);

  useEffect(() => {
    setValue(`${basePath}.charge`, findPrice(services, packages, itemType, itemId), { shouldValidate: true });
    // `services`/`packages`/`basePath` are stable per row instance — only the selected item should recompute the price.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [itemType, itemId, services, packages, setValue]);

  const rowErrors = errors.laboratory?.[index];
  const selectedPackage = itemType === 'package' ? packages.find((p) => p.id === itemId) : undefined;
  const amount = Math.max(quantity * charge - discount, 0);

  return (
    <div className={showRemove || !isLast ? 'flex flex-col gap-4 border-b border-dashed border-border pb-4' : 'flex flex-col gap-4'}>
      {/* Its own line, above the field row — nesting it inside "Item"'s Field (between the
          label and the select) was what forced every sibling field to carry a matching
          invisible spacer just to keep their inputs lined up with Item's. Hoisting it out
          means every field below has the same plain label+control shape, so plain
          items-start alignment lines them all up with no extra bookkeeping. */}
      <div>
        <span className="mb-1 block text-sm font-medium leading-none text-foreground">Item type</span>
        <div className="inline-flex w-fit overflow-hidden rounded-md border border-input">
          <button
            type="button"
            onClick={() => handleFilterTypeChange('service')}
            className={`px-2.5 py-1 text-xs font-medium transition-colors ${
              filterType === 'service' ? 'bg-primary text-primary-foreground' : 'bg-background text-muted-foreground hover:bg-accent'
            }`}
          >
            Services
          </button>
          <button
            type="button"
            onClick={() => handleFilterTypeChange('package')}
            className={`px-2.5 py-1 text-xs font-medium transition-colors ${
              filterType === 'package' ? 'bg-primary text-primary-foreground' : 'bg-background text-muted-foreground hover:bg-accent'
            }`}
          >
            Packages
          </button>
        </div>
      </div>

      <div className="flex flex-wrap items-start gap-3">
        <Field label="Item" htmlFor={`${basePath}-item`} error={rowErrors?.itemId?.message} className="flex min-w-[240px] flex-1 flex-col gap-1">
          <Controller
            name={`${basePath}.itemId`}
            control={control}
            render={({ field }) => (
              <SearchableSelect
                id={`${basePath}-item`}
                ariaLabel="Laboratory item"
                value={itemType === filterType && field.value ? toOptionValue(itemType, field.value) : ''}
                onValueChange={(value) => {
                  const parsed = parseOptionValue(value);
                  if (!parsed) return;
                  setValue(`${basePath}.itemType`, parsed.itemType);
                  field.onChange(parsed.itemId);
                }}
                options={filteredOptions}
                placeholder={isLoadingItems ? 'Loading items…' : filterType === 'package' ? 'Select a package' : 'Select a test'}
                searchPlaceholder={filterType === 'package' ? 'Search packages…' : 'Search tests…'}
                disabled={isLoadingItems}
              />
            )}
          />
          <span className="text-xs text-muted-foreground">
            {selectedPackage ? `${selectedPackage.items.length} test${selectedPackage.items.length === 1 ? '' : 's'} included` : '—'}
          </span>
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

        <ChargeDisplay id={`${basePath}-amount`} amount={amount} label="Amount" />

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

