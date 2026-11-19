import { ApiError, createDiagnosticPackageSchema, type DiagnosticPackageFormValues } from '@hms/shared';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState, type ReactNode } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { packageBreakdown, packageCostPrice, packageProfitMode, validatePackageProfit, type PackageProfitMode } from '../tariff';
import { PackageProfitFields } from './PackageProfitFields';
import { ProfitBreakdownView } from './ProfitBreakdownView';

interface DiagnosticPackageFormProps {
  defaultValues?: Partial<DiagnosticPackageFormValues>;
  mode: 'create' | 'edit';
  onSubmit: (values: DiagnosticPackageFormValues) => void;
  isSubmitting: boolean;
  submitLabel: string;
  apiError: ApiError | null;
  /** Running-cost total of the tests chosen so far (null when one isn't costed). */
  testsRunningCost?: number | null;
  /** Extra cards shown above Package details (the create page's "First test" picker). */
  children?: ReactNode;
}

/**
 * Central Laboratory's package create form (ADR-140). The package price is what the patient
 * pays; the profit is entered the way the tariff sheet writes it ("CBC – 300, Profit – 120") or
 * taken from the tests' running costs, and the running cost saved follows from it. Tests are
 * added afterward on LabPackageDetailPage.
 */
export function DiagnosticPackageForm({ defaultValues, mode, onSubmit, isSubmitting, submitLabel, apiError, testsRunningCost, children }: DiagnosticPackageFormProps) {
  const codeReadOnly = mode === 'edit';
  const initialCost = defaultValues?.costPrice ?? 0;
  const initialPrice = defaultValues?.totalPrice ?? 0;
  const [profitMode, setProfitMode] = useState<PackageProfitMode>(packageProfitMode({ costPrice: initialCost }));
  const [profit, setProfit] = useState(initialCost > 0 ? String(initialPrice - initialCost) : '');
  const [profitError, setProfitError] = useState<string | null>(null);

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<DiagnosticPackageFormValues>({
    resolver: zodResolver(createDiagnosticPackageSchema),
    defaultValues: {
      code: '',
      name: '',
      description: '',
      totalPrice: 0,
      costPrice: 0,
      isActive: true,
      ...defaultValues,
    },
  });
  const price = useWatch({ control, name: 'totalPrice' });
  const breakdown = packageBreakdown(price, profitMode, profit, testsRunningCost ?? null);

  function submit(values: DiagnosticPackageFormValues) {
    if (profitMode === 'profit') {
      const error = validatePackageProfit(values.totalPrice, profit);
      setProfitError(error);
      if (error) return;
    }
    onSubmit({ ...values, costPrice: packageCostPrice(values.totalPrice, profitMode, profit) });
  }

  const generalError = apiError && !apiError.validationErrors ? apiError.message : null;

  return (
    <form onSubmit={handleSubmit(submit)} noValidate className="mx-auto flex w-full max-w-6xl flex-col gap-6">
      {generalError && (
        <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {generalError}
        </p>
      )}

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_380px]">
        <div className="flex min-w-0 flex-col gap-6">
          {children}

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Package details</CardTitle>
            </CardHeader>
            <CardContent className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="dpk-name">Package name</Label>
                <Input id="dpk-name" {...register('name')} />
                {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="dpk-code">Code</Label>
                <Input id="dpk-code" disabled={codeReadOnly} {...register('code')} />
                {errors.code && <p className="text-sm text-destructive">{errors.code.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5 sm:col-span-2">
                <Label htmlFor="dpk-description">Shown on the bill and report as</Label>
                <Input id="dpk-description" placeholder="e.g. TC, HB, PCV/HCT, RBC, MCV…" {...register('description')} />
                {errors.description && <p className="text-sm text-destructive">{errors.description.message}</p>}
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
            </CardContent>
          </Card>

          {mode === 'create' && (
            <p className="text-sm text-muted-foreground">More tests are added on the package's page — you'll be taken there once it's created.</p>
          )}
        </div>

        <Card className="border-primary/30 lg:sticky lg:top-4">
          <CardHeader>
            <CardTitle className="text-base">Package price and profit</CardTitle>
            <CardDescription>The package price is what the patient sees and pays. The profit is worked out in the background.</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="dpk-totalPrice">Package price — patient pays (₹)</Label>
              <Input id="dpk-totalPrice" type="number" min={0} step="any" className="text-lg font-semibold" {...register('totalPrice')} />
              {errors.totalPrice && <p className="text-sm text-destructive">{errors.totalPrice.message}</p>}
            </div>

            <PackageProfitFields
              idPrefix="dpk"
              mode={profitMode}
              onModeChange={(next) => {
                setProfitMode(next);
                setProfitError(null);
              }}
              profit={profit}
              onProfitChange={(next) => {
                setProfit(next);
                setProfitError(null);
              }}
              testsRunningCost={testsRunningCost}
              error={profitError}
            />

            <ProfitBreakdownView breakdown={breakdown} />
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
