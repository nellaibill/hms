import { useQuery } from '@tanstack/react-query';
import { FileText, PlusCircle, Stethoscope, Trash2, X, XCircle } from 'lucide-react';
import { useEffect } from 'react';
import { Controller, useFieldArray, useFormContext, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { ConsultantSelect } from '@/components/ConsultantSelect';
import { ConsultationTypeSelect } from '@/components/ConsultationTypeSelect';
import { DepartmentSelect } from '@/components/DepartmentSelect';
import { Input } from '@/components/ui/input';
import { SearchableSelect } from '@/components/ui/searchable-select';
import { Field } from '@/features/patients/components/FormSection';
import { consultationTypesApi } from '@/services/apiClient';
import { isConsultationEntryActive } from '../billingActivity';
import { getServicePrice, type BillingService } from '../billingCatalog';
import { formatCurrency } from '../billingCalculations';
import { emptyConsultation, emptySimpleServiceRow, type BillingFormValues } from '../billingValidation';
import { useDiagnosticTestServices } from '../hooks/useDiagnosticTestServices';
import { CollapsibleCard } from './CollapsibleCard';

interface ConsultationBillingCardProps {
  expanded: boolean;
  onToggle: () => void;
  hasError: boolean;
}

/**
 * One or more Department → Consultant → Consultation Type rows. Charge auto-fills from the
 * selected Consultation Type's master rate and is locked once that rate exists — staff pick the
 * category, they don't set the price. Types with no fixed master rate (e.g. "Others/On-call")
 * leave the field open for manual entry, since there's nothing to lock it to. Department/
 * Consultant stay pure attribution (who saw the patient, where). Mirrors ServiceBillingCard's
 * row/array pattern used by Radiology/Laboratory/Procedure, so a visit seen by more than one
 * specialist can bill more than one consultation (previously a "demo affordance" row that
 * silently never reached the saved invoice).
 */
export function ConsultationBillingCard({ expanded, onToggle, hasError }: ConsultationBillingCardProps) {
  const { control } = useFormContext<BillingFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: 'consultation' });
  const rows = useWatch({ control, name: 'consultation' });
  const fileFields = useFieldArray({ control, name: 'file' });

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

      <ConsultationFileCharges fileFields={fileFields} />
    </CollapsibleCard>
  );
}

/**
 * File Charges used to be its own top-level billing category (a fifth CollapsibleCard sitting
 * below Radiology/Laboratory/Procedure/Injection) — the customer's actual workflow only ever
 * needs it alongside a Consultation, so it now lives nested inside this card instead, hidden
 * behind an explicit "Add File Billing" action rather than showing an empty row by default.
 * Still backed by the same top-level `file` field array/schema as before (billingValidation.ts,
 * describeBillingItem's 'File' branch, the invoice's File line items) — only where this array
 * is edited moved, not what it saves as.
 */
function ConsultationFileCharges({ fileFields }: { fileFields: ReturnType<typeof useFieldArray<BillingFormValues, 'file'>> }) {
  const { fields, append, remove, replace } = fileFields;
  const { services, isLoading } = useDiagnosticTestServices('File');

  if (fields.length === 0) {
    return (
      <div className="flex flex-col gap-2 border-t border-dashed border-border pt-4">
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="w-fit gap-1.5 text-primary"
          onClick={() => append({ ...emptySimpleServiceRow })}
        >
          <PlusCircle className="h-4 w-4" />
          Add File Billing
        </Button>
        <p className="flex items-start gap-2 rounded-md bg-accent/60 px-3 py-2 text-sm text-accent-foreground">
          Click &quot;Add File Billing&quot; if you want to add file/document charges to this consultation.
        </p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-border bg-accent/30 p-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <FileText className="mt-0.5 h-4 w-4 shrink-0 text-primary" />
          <div className="flex flex-col">
            <span className="text-sm font-semibold text-foreground">
              File Charges <span className="font-normal text-primary">(Optional)</span>
            </span>
            <span className="text-xs text-muted-foreground">Add file charges for documents (case sheet, ANC file, medical record file, etc.)</span>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="shrink-0 gap-1.5 border-destructive/40 text-destructive hover:bg-destructive/10"
          onClick={() => replace([])}
        >
          <XCircle className="h-4 w-4" />
          Remove File Billing
        </Button>
      </div>

      <div className="overflow-hidden rounded-md border border-border">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-sidebar-active text-left text-xs font-medium uppercase tracking-wide text-sidebar-active-foreground">
              <tr>
                <th className="px-3 py-2">#</th>
                <th className="px-3 py-2">File Type</th>
                <th className="px-3 py-2">Charge (₹)</th>
                <th className="px-3 py-2 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border bg-card">
              {fields.map((field, index) => (
                <FileChargeRow key={field.id} index={index} services={services} isLoadingServices={isLoading} onRemove={() => remove(index)} />
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <Button type="button" variant="outline" size="sm" className="w-fit gap-1.5" onClick={() => append({ ...emptySimpleServiceRow })}>
        <PlusCircle className="h-4 w-4" />
        Add Another File
      </Button>
    </div>
  );
}

interface FileChargeRowProps {
  index: number;
  services: BillingService[];
  isLoadingServices: boolean;
  onRemove: () => void;
}

function FileChargeRow({ index, services, isLoadingServices, onRemove }: FileChargeRowProps) {
  const {
    control,
    setValue,
    watch,
    formState: { errors },
  } = useFormContext<BillingFormValues>();
  const basePath = `file.${index}` as const;

  const serviceId = watch(`${basePath}.serviceId`);
  const charge = watch(`${basePath}.charge`);

  useEffect(() => {
    setValue(`${basePath}.charge`, getServicePrice(services, serviceId), { shouldValidate: true });
    // `services`/`basePath` are stable per row instance — only the selected service should recompute the price.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [serviceId, services, setValue]);

  const serviceOptions = services.map((s) => ({ value: s.id, label: `${s.name} — ${formatCurrency(s.price)}`, keywords: s.name }));
  const rowError = errors.file?.[index]?.serviceId?.message;

  return (
    <tr>
      <td className="px-3 py-2 align-top text-muted-foreground">{index + 1}</td>
      <td className="px-3 py-2 align-top">
        <Controller
          name={`${basePath}.serviceId`}
          control={control}
          render={({ field }) => (
            <SearchableSelect
              id={`${basePath}-service`}
              ariaLabel="File type"
              value={field.value}
              onValueChange={field.onChange}
              options={serviceOptions}
              placeholder={isLoadingServices ? 'Loading…' : 'Select file type'}
              searchPlaceholder="Search file types…"
              disabled={isLoadingServices}
            />
          )}
        />
        {rowError && <p className="mt-1 text-xs text-destructive">{rowError}</p>}
      </td>
      <td className="px-3 py-2 align-top">
        <div className="flex h-10 w-32 items-center rounded-md border border-dashed border-input bg-muted px-3 text-sm font-semibold text-foreground">
          {formatCurrency(charge)}
        </div>
      </td>
      <td className="px-3 py-2 text-right align-top">
        <Button type="button" variant="ghost" size="icon" aria-label="Remove this file charge" onClick={onRemove}>
          <Trash2 className="h-4 w-4 text-destructive" />
        </Button>
      </td>
    </tr>
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
  const consultantId = watch(`${basePath}.consultantId`);
  const consultationTypeId = watch(`${basePath}.consultationTypeId`);
  const fromVisit = watch(`${basePath}.fromVisit`);

  // Same query key ConsultationTypeSelect uses below, so this is a cache read, not an extra
  // request — just here to look up the selected type's real fee for the charge effect.
  const { data: consultationTypes } = useQuery({
    queryKey: ['consultationTypes', 'select-list'],
    queryFn: () => consultationTypesApi.getConsultationTypes({ pageSize: 100, isActive: true }),
  });

  const selectedType = consultationTypes?.items.find((t) => t.id === consultationTypeId);
  // Types like "Doctor's Consultation - Others/On-call" have no fixed master amount (amount:
  // null, "Amount to be filled" — see ConsultationTypeSelect) — the charge field stays editable
  // for those since there's no master rate to lock it to; every other type locks it, so staff
  // pick the category rather than set the price.
  const hasFixedCharge = selectedType?.amount != null;

  const fixedAmount = selectedType?.amount;

  useEffect(() => {
    // Keyed on the type id and its master amount (a primitive), not `consultationTypes`/
    // `selectedType` themselves: those get a new object reference on every refetch (focus,
    // cache invalidation from an unrelated mutation, etc.), and re-running on that alone used
    // to stomp whatever the user had just typed back to the master default.
    //
    // The amount has to be a dependency too. When the row is prefilled from a visit,
    // consultationTypeId is set before the consultation-types query has loaded, so on that
    // first run selectedType is still undefined. Without re-running once the amount arrives,
    // the charge stayed at ₹0 — and hasFixedCharge then locked the field, so the consultation
    // was billed free (regression report BIL-01, INV-2026-000019).
    if (fixedAmount != null) {
      setValue(`${basePath}.charge`, fixedAmount, { shouldValidate: true });
    }
  }, [basePath, consultationTypeId, fixedAmount, setValue]);

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
          className="flex w-full min-w-[220px] flex-1 flex-col gap-1 sm:w-auto"
        >
          <Controller
            name={`${basePath}.consultationTypeId`}
            control={control}
            render={({ field }) => (
              <ConsultationTypeSelect
                id={`${basePath}-type`}
                value={field.value}
                onValueChange={field.onChange}
                // Scoped to this consultant's own Consultation Types, and cleared when the
                // consultant changes — see ConsultationTypeSelect.
                departmentId={departmentId || undefined}
                consultantId={consultantId || undefined}
              />
            )}
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
                disabled={hasFixedCharge}
                value={field.value}
                // The field starts at (or gets cleared back to) 0 — e.g. "Doctor's Consultation
                // - Others/On-call" has no master fee (see the effect above), so staff type the
                // real amount in, and clearing the field to retype resets it to 0 too. Selecting
                // on focus covers typing right after landing on a pre-filled "0". That alone
                // isn't enough for clear-then-retype though: clearing re-renders the field back
                // to "0" without reselecting it (focus doesn't refire), so the next keystroke
                // would insert after that "0" — hence also stripping any leading zero directly
                // off what was just typed, and writing the corrected text back into the input
                // right away rather than waiting on the next render to fix the display.
                onFocus={(e) => e.target.select()}
                onChange={(e) => {
                  let raw = e.target.value;
                  if (/^0+(?=\d)/.test(raw)) {
                    raw = raw.replace(/^0+/, '');
                    e.target.value = raw;
                  }
                  field.onChange(raw === '' ? 0 : Number(raw));
                }}
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
