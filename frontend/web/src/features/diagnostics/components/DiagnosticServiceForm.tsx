import { ApiError, createDiagnosticServiceSchema, DIAGNOSTIC_SAMPLE_TYPES, DIAGNOSTIC_SERVICE_TYPES, type DiagnosticServiceFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { Plus, X } from 'lucide-react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { cn } from '@/lib/utils';
import { useDiagnosticCategoriesQuery } from '../hooks/useDiagnosticCategoriesQuery';
import { useDiagnosticProvidersQuery } from '../hooks/useDiagnosticProvidersQuery';
import { computeProfit, formatRupees } from '../tariff';
import { ProfitBreakdownView } from './ProfitBreakdownView';

/** A lab package that includes this test, for the "Used in packages" card. */
export interface PackageUsingService {
  id: string;
  name: string;
  totalPrice: number;
  costPrice: number;
}

interface DiagnosticServiceFormProps {
  defaultValues?: Partial<DiagnosticServiceFormValues>;
  mode: 'create' | 'edit';
  onSubmit: (values: DiagnosticServiceFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
  /** Lab tests only: the packages this test is part of (edit mode). */
  packagesUsingThis?: PackageUsingService[];
}

const NO_SAMPLE = '__none__';

const DEFAULT_VALUES: DiagnosticServiceFormValues = {
  code: '',
  name: '',
  categoryId: '',
  serviceType: 'Laboratory',
  isOutsourced: false,
  providerId: '',
  price: 0,
  costPrice: 0,
  isActive: true,
  doctorCharge: 0,
  bedsideEnabled: false,
  bedsidePrice: 0,
  bedsideDoctorCharge: 0,
  rateOptions: [],
  sampleType: '',
  sortOrder: undefined,
};

/**
 * Central Laboratory's lab test / radiology service create-edit form (ADR-140). The fields
 * follow the kind of service, as the hospital's tariff sheet does:
 *   - Lab test: done in our lab or outsourced; Price and Running cost (for an outsourced test,
 *     what the outside lab charges us). No doctor's share.
 *   - Radiology: Price, Doctor's share and Running cost, plus named rate options (e.g. LH, MR)
 *     and a bedside add-on.
 * The right-hand card works out the profit and profit % as the numbers are typed.
 */
export function DiagnosticServiceForm({ defaultValues, mode, onSubmit, isSubmitting, submitLabel, apiError, packagesUsingThis }: DiagnosticServiceFormProps) {
  const codeReadOnly = mode === 'edit';
  const categoriesQuery = useDiagnosticCategoriesQuery({ pageSize: 200, isActive: true, sort: 'name' });
  const providersQuery = useDiagnosticProvidersQuery({ pageSize: 200, isActive: true, sort: 'name' });

  const {
    register,
    control,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<DiagnosticServiceFormValues>({
    resolver: zodResolver(createDiagnosticServiceSchema),
    defaultValues: { ...DEFAULT_VALUES, ...defaultValues },
  });
  const { fields: rateOptionFields, append: appendRateOption, remove: removeRateOption } = useFieldArray({ control, name: 'rateOptions' });

  const [serviceType, isOutsourced, price, costPrice, doctorCharge, bedsideEnabled, bedsidePrice, bedsideDoctorCharge, rateOptions] = useWatch({
    control,
    name: ['serviceType', 'isOutsourced', 'price', 'costPrice', 'doctorCharge', 'bedsideEnabled', 'bedsidePrice', 'bedsideDoctorCharge', 'rateOptions'],
  });
  const isRadiology = serviceType === 'Radiology';
  const outsourced = !isRadiology && isOutsourced;
  const runningCostLabel = isRadiology ? 'Running cost' : outsourced ? 'Lab charge' : 'Running cost';
  const breakdown = computeProfit(price, costPrice, isRadiology ? doctorCharge : 0);
  const bedsideBreakdown = computeProfit(Number(price) + Number(bedsidePrice || 0), costPrice, Number(doctorCharge) + Number(bedsideDoctorCharge || 0));

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="mx-auto flex w-full max-w-6xl flex-col gap-6">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_380px]">
        <div className="flex min-w-0 flex-col gap-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{isRadiology ? 'Service details' : 'Test details'}</CardTitle>
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="ds-name">{isRadiology ? 'Service name' : 'Test name'}</Label>
                <Input id="ds-name" {...register('name')} />
                {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="ds-code">Code</Label>
                <Input id="ds-code" disabled={codeReadOnly} {...register('code')} />
                {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="ds-serviceType">Kind</Label>
                <Controller
                  name="serviceType"
                  control={control}
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger id="ds-serviceType" aria-label="Kind">
                        <SelectValue placeholder="Select kind…" />
                      </SelectTrigger>
                      <SelectContent>
                        {DIAGNOSTIC_SERVICE_TYPES.map((type) => (
                          <SelectItem key={type} value={type}>
                            {type === 'Laboratory' ? 'Lab test' : 'Radiology'}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
                {errors.serviceType && <p className="text-sm text-destructive">{errors.serviceType.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="ds-category">Section</Label>
                <Controller
                  name="categoryId"
                  control={control}
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={field.onChange} disabled={categoriesQuery.isPending}>
                      <SelectTrigger id="ds-category" aria-label="Section">
                        <SelectValue placeholder="Select section…" />
                      </SelectTrigger>
                      <SelectContent>
                        {(categoriesQuery.data?.items ?? []).map((category) => (
                          <SelectItem key={category.id} value={category.id}>
                            {category.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
                {errors.categoryId && <p className="text-sm text-destructive">{errors.categoryId.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <span className="text-sm font-medium leading-none text-foreground">Status</span>
                <Controller
                  name="isActive"
                  control={control}
                  render={({ field }) => (
                    <div className="flex h-10 items-center gap-2">
                      <Switch checked={field.value} onCheckedChange={field.onChange} aria-label="Active" />
                      <span className="text-sm text-muted-foreground">{field.value ? 'Active — can be billed' : 'Inactive'}</span>
                    </div>
                  )}
                />
              </div>

              {!isRadiology && (
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="ds-sampleType">Sample</Label>
                  <Controller
                    name="sampleType"
                    control={control}
                    render={({ field }) => (
                      <Select value={field.value || NO_SAMPLE} onValueChange={(value) => field.onChange(value === NO_SAMPLE ? '' : value)}>
                        <SelectTrigger id="ds-sampleType" aria-label="Sample">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value={NO_SAMPLE}>Not set</SelectItem>
                          {DIAGNOSTIC_SAMPLE_TYPES.map((sample) => (
                            <SelectItem key={sample} value={sample}>
                              {sample}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                  <p className="text-xs text-muted-foreground">The Price List heading it sits under, as in the tariff sheet ("In-house tests – Blood").</p>
                </div>
              )}

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="ds-sortOrder">List order</Label>
                <Input id="ds-sortOrder" type="number" min={1} step={1} placeholder="e.g. 10" {...register('sortOrder')} />
                <p className="text-xs text-muted-foreground">Where it sits in the Price List — lower comes first. Blank lists it at the end of its section.</p>
                {errors.sortOrder && <p className="text-sm text-destructive">{errors.sortOrder.message}</p>}
              </div>
            </CardContent>
          </Card>

          {!isRadiology && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Where the test is done</CardTitle>
                <CardDescription>Decides what the running cost means: our reagents and consumables, or what the outside lab charges us.</CardDescription>
              </CardHeader>
              <CardContent className="flex flex-col gap-4">
                <div role="group" aria-label="Where the test is done" className="inline-flex self-start rounded-lg bg-muted p-1">
                  <SegmentButton pressed={!isOutsourced} onClick={() => setValue('isOutsourced', false, { shouldDirty: true })}>
                    In our lab
                  </SegmentButton>
                  <SegmentButton pressed={isOutsourced} onClick={() => setValue('isOutsourced', true, { shouldDirty: true })}>
                    Outsourced
                  </SegmentButton>
                </div>
                {isOutsourced && (
                  <div className="flex max-w-sm flex-col gap-1.5">
                    <Label htmlFor="ds-provider">Outside lab</Label>
                    <Controller
                      name="providerId"
                      control={control}
                      render={({ field }) => (
                        <Select value={field.value || undefined} onValueChange={field.onChange} disabled={providersQuery.isPending}>
                          <SelectTrigger id="ds-provider" aria-label="Outside lab">
                            <SelectValue placeholder="Select outside lab…" />
                          </SelectTrigger>
                          <SelectContent>
                            {(providersQuery.data?.items ?? []).map((provider) => (
                              <SelectItem key={provider.id} value={provider.id}>
                                {provider.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      )}
                    />
                    {errors.providerId && <p className="text-sm text-destructive">{errors.providerId.message}</p>}
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {isRadiology && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Rate options</CardTitle>
                <CardDescription>
                  Other rates for the same scan (for example LH or MR), each with its own price, doctor's share and running cost. Billing staff
                  pick the rate on the bill; without one, the standard rate on the right is billed.
                </CardDescription>
              </CardHeader>
              <CardContent className="flex flex-col gap-3">
                {rateOptionFields.length > 0 && (
                  <div className="overflow-x-auto">
                    <table className="w-full min-w-[520px] text-sm">
                      <thead>
                        <tr className="border-b border-border text-left text-xs text-muted-foreground">
                          <th scope="col" className="py-2 pr-2 font-medium">Name</th>
                          <th scope="col" className="px-2 py-2 font-medium">Price ₹</th>
                          <th scope="col" className="px-2 py-2 font-medium">Doctor's share ₹</th>
                          <th scope="col" className="px-2 py-2 font-medium">Running cost ₹</th>
                          <th scope="col" className="px-2 py-2 text-right font-medium">Profit</th>
                          <th scope="col" className="w-10 py-2">
                            <span className="sr-only">Remove</span>
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {rateOptionFields.map((field, index) => {
                          const option = rateOptions?.[index];
                          const optionProfit = computeProfit(option?.price, option?.costPrice, option?.doctorCharge);
                          const rowErrors = errors.rateOptions?.[index];
                          return (
                            <tr key={field.id} className="border-b border-border align-top last:border-0">
                              <td className="py-2 pr-2">
                                <Input aria-label={`Rate option ${index + 1} name`} placeholder="e.g. LH" {...register(`rateOptions.${index}.name`)} />
                                {rowErrors?.name && <p className="mt-1 text-xs text-destructive">{rowErrors.name.message}</p>}
                              </td>
                              <td className="px-2 py-2">
                                <Input type="number" min={0} step="any" aria-label={`Rate option ${index + 1} price`} {...register(`rateOptions.${index}.price`)} />
                              </td>
                              <td className="px-2 py-2">
                                <Input type="number" min={0} step="any" aria-label={`Rate option ${index + 1} doctor's share`} {...register(`rateOptions.${index}.doctorCharge`)} />
                              </td>
                              <td className="px-2 py-2">
                                <Input type="number" min={0} step="any" aria-label={`Rate option ${index + 1} running cost`} {...register(`rateOptions.${index}.costPrice`)} />
                              </td>
                              <td className="whitespace-nowrap px-2 py-2 pt-4 text-right tabular-nums">
                                {formatRupees(optionProfit.profit)}
                                {optionProfit.profitPercent !== null && <span className="text-muted-foreground"> ({optionProfit.profitPercent}%)</span>}
                              </td>
                              <td className="py-2">
                                <Button type="button" variant="ghost" size="icon" aria-label={`Remove rate option ${option?.name || index + 1}`} onClick={() => removeRateOption(index)}>
                                  <X className="h-4 w-4" />
                                </Button>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )}
                {typeof errors.rateOptions?.message === 'string' && <p className="text-sm text-destructive">{errors.rateOptions.message}</p>}
                <Button
                  type="button"
                  variant="outline"
                  className="self-start"
                  disabled={rateOptionFields.length >= 10}
                  onClick={() => appendRateOption({ name: '', price: Number(price) || 0, doctorCharge: 0, costPrice: Number(costPrice) || 0 })}
                >
                  <Plus className="mr-1 h-4 w-4" />
                  Add rate option
                </Button>
              </CardContent>
            </Card>
          )}

          {isRadiology && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Bedside add-on</CardTitle>
                <CardDescription>For a scan that can be done at the bedside: what it adds to the price and to the doctor's share.</CardDescription>
              </CardHeader>
              <CardContent className="flex flex-col gap-4">
                <Controller
                  name="bedsideEnabled"
                  control={control}
                  render={({ field }) => (
                    <div className="flex items-center gap-2">
                      <Switch id="ds-bedside" checked={field.value} onCheckedChange={field.onChange} />
                      <Label htmlFor="ds-bedside" className="cursor-pointer">
                        Can be done at the bedside
                      </Label>
                    </div>
                  )}
                />
                {bedsideEnabled && (
                  <>
                    <div className="grid gap-4 sm:grid-cols-2">
                      <div className="flex flex-col gap-1.5">
                        <Label htmlFor="ds-bedsidePrice">Adds to the price (₹)</Label>
                        <Input id="ds-bedsidePrice" type="number" min={0} step="any" {...register('bedsidePrice')} />
                        {errors.bedsidePrice && <p className="text-sm text-destructive">{errors.bedsidePrice.message}</p>}
                      </div>
                      <div className="flex flex-col gap-1.5">
                        <Label htmlFor="ds-bedsideDoctorCharge">Adds to the doctor's share (₹)</Label>
                        <Input id="ds-bedsideDoctorCharge" type="number" min={0} step="any" {...register('bedsideDoctorCharge')} />
                        {errors.bedsideDoctorCharge && <p className="text-sm text-destructive">{errors.bedsideDoctorCharge.message}</p>}
                      </div>
                    </div>
                    <p className="text-sm text-muted-foreground">
                      Standard rate at the bedside: price {formatRupees(bedsideBreakdown.price)}, doctor's share {formatRupees(bedsideBreakdown.doctorShare)}, profit{' '}
                      {formatRupees(bedsideBreakdown.profit)}
                      {bedsideBreakdown.profitPercent !== null ? ` (${bedsideBreakdown.profitPercent}%)` : ''}.
                    </p>
                  </>
                )}
              </CardContent>
            </Card>
          )}

          {!isRadiology && packagesUsingThis && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">
                  Used in {packagesUsingThis.length} {packagesUsingThis.length === 1 ? 'package' : 'packages'}
                </CardTitle>
                <CardDescription>Each package keeps its own price and profit, so changing this test doesn't change them.</CardDescription>
              </CardHeader>
              {packagesUsingThis.length > 0 && (
                <CardContent>
                  <ul className="flex flex-col">
                    {packagesUsingThis.map((pkg) => {
                      const pkgProfit = computeProfit(pkg.totalPrice, pkg.costPrice);
                      return (
                        <li key={pkg.id} className="flex flex-wrap items-center justify-between gap-2 border-t border-border py-2.5 text-sm first:border-t-0">
                          <Link to={`/diagnostics/lab/packages/${pkg.id}`} className="font-medium text-primary hover:underline">
                            {pkg.name}
                          </Link>
                          <span className="tabular-nums text-muted-foreground">
                            {formatRupees(pkg.totalPrice)} ·{' '}
                            {pkgProfit.isCosted ? `profit ${formatRupees(pkgProfit.profit)} (${pkgProfit.profitPercent ?? 0}%)` : "profit from its tests' total"}
                          </span>
                        </li>
                      );
                    })}
                  </ul>
                </CardContent>
              )}
            </Card>
          )}
        </div>

        <Card className="border-primary/30 lg:sticky lg:top-4">
          <CardHeader>
            <CardTitle className="text-base">{isRadiology ? 'Standard rate and profit' : 'Price and profit'}</CardTitle>
            <CardDescription>
              {isRadiology
                ? "Enter the price, the doctor's share and our running cost. Income and profit are worked out for you."
                : 'Enter what the patient pays and what the test costs us. Profit is worked out for you.'}
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="ds-price">Price — patient pays (₹)</Label>
              <Input id="ds-price" type="number" min={0} step="any" className="text-lg font-semibold" {...register('price')} />
              {errors.price && <p className="text-sm text-destructive">{errors.price.message}</p>}
            </div>

            <div className={cn('grid gap-4', isRadiology && 'sm:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2')}>
              {isRadiology && (
                <div className="flex flex-col gap-1.5">
                  <Label htmlFor="ds-doctorCharge">Doctor's share (₹)</Label>
                  <Input id="ds-doctorCharge" type="number" min={0} step="any" {...register('doctorCharge')} />
                  {errors.doctorCharge && <p className="text-sm text-destructive">{errors.doctorCharge.message}</p>}
                </div>
              )}
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="ds-costPrice">
                  {outsourced ? 'Lab charges us (₹)' : isRadiology ? 'Running cost (₹)' : 'Running cost — reagents, consumables (₹)'}
                </Label>
                <Input id="ds-costPrice" type="number" min={0} step="any" {...register('costPrice')} />
                {errors.costPrice && <p className="text-sm text-destructive">{errors.costPrice.message}</p>}
              </div>
            </div>
            <p className="text-xs text-muted-foreground">
              {outsourced
                ? 'What the outside lab bills the hospital for one test.'
                : isRadiology
                  ? 'Films, gel and other consumables for one scan. Leave both at 0 if not yet costed.'
                  : 'What it costs our lab to run the test once. Leave 0 if not yet costed.'}
            </p>

            <ProfitBreakdownView breakdown={breakdown} runningCostLabel={runningCostLabel} showDoctorShare={isRadiology} />
          </CardContent>
        </Card>
      </div>

      <div className="sticky bottom-0 z-10 -mx-4 flex justify-end gap-3 border-t border-border bg-background/95 px-4 py-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : submitLabel}
        </Button>
      </div>
    </form>
  );
}

function SegmentButton({ pressed, onClick, children }: { pressed: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      aria-pressed={pressed}
      onClick={onClick}
      className={cn(
        'min-h-10 rounded-md px-4 text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
        pressed ? 'bg-background font-semibold text-foreground shadow-sm' : 'font-medium text-muted-foreground hover:text-foreground',
      )}
    >
      {children}
    </button>
  );
}
