import { ApiError, type DiagnosticPackage } from '@hms/shared';
import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useUpdateDiagnosticPackageMutation } from '../hooks/useDiagnosticPackageMutations';
import { formatRupees, packageBreakdown, packageCostPrice, packageProfitMode, validatePackageProfit, type PackageProfitMode } from '../tariff';
import { PackageProfitFields } from './PackageProfitFields';
import { ProfitBreakdownView } from './ProfitBreakdownView';

interface PackagePriceCardProps {
  pkg: DiagnosticPackage;
  /** What the package's tests come to when billed one by one. */
  testsTotalPrice: number;
  /** The tests' total running cost; null when a test isn't costed yet. */
  testsRunningCost: number | null;
  canEdit: boolean;
}

/**
 * A lab package's price and profit (ADR-140), on its own page: the price the patient pays, how
 * the profit is set (as in the tariff sheet, or from the tests' running costs), what the patient
 * saves against billing the tests one by one, and the worked-out profit. Saves with its own
 * button; the package's other details stay in the Edit dialog.
 */
export function PackagePriceCard({ pkg, testsTotalPrice, testsRunningCost, canEdit }: PackagePriceCardProps) {
  const mutation = useUpdateDiagnosticPackageMutation();
  const [price, setPrice] = useState(String(pkg.totalPrice));
  const [mode, setMode] = useState<PackageProfitMode>(packageProfitMode(pkg));
  const [profit, setProfit] = useState(pkg.costPrice > 0 ? String(pkg.totalPrice - pkg.costPrice) : '');
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  // A save (here or in the Edit dialog) refetches the package — start again from what's stored.
  useEffect(() => {
    setPrice(String(pkg.totalPrice));
    setMode(packageProfitMode(pkg));
    setProfit(pkg.costPrice > 0 ? String(pkg.totalPrice - pkg.costPrice) : '');
  }, [pkg]);

  const breakdown = packageBreakdown(price, mode, profit, testsRunningCost);
  const priceNumber = Number(price) || 0;
  const saving = Math.max(0, testsTotalPrice - priceNumber);
  const costToSave = packageCostPrice(price, mode, profit);
  const isDirty = priceNumber !== pkg.totalPrice || costToSave !== (pkg.costPrice ?? 0);

  function handleSave() {
    if (priceNumber < 0) {
      setError("Price can't be negative.");
      return;
    }
    if (mode === 'profit') {
      const profitError = validatePackageProfit(price, profit);
      setError(profitError);
      if (profitError) return;
    }
    setError(null);
    mutation.mutate(
      {
        id: pkg.id,
        request: {
          code: pkg.code,
          name: pkg.name,
          description: pkg.description || undefined,
          totalPrice: priceNumber,
          costPrice: costToSave,
          isActive: pkg.isActive,
          sortOrder: pkg.sortOrder ?? null,
        },
      },
      { onSuccess: () => setSaved(true) },
    );
  }

  const apiError = mutation.error instanceof ApiError ? mutation.error : null;

  return (
    <Card className="border-primary/30 lg:sticky lg:top-4">
      <CardHeader>
        <CardTitle className="text-base">Package price and profit</CardTitle>
        <CardDescription>The package price is what the patient sees and pays. The profit is worked out in the background.</CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {apiError && (
          <p role="alert" className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {apiError.message}
          </p>
        )}

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="pkp-price">Package price — patient pays (₹)</Label>
          <Input
            id="pkp-price"
            type="number"
            min={0}
            step="any"
            className="text-lg font-semibold"
            value={price}
            disabled={!canEdit}
            onChange={(event) => {
              setPrice(event.target.value);
              setSaved(false);
            }}
          />
          {testsTotalPrice > 0 && (
            <p className="text-xs text-muted-foreground">
              Billed one by one: {formatRupees(testsTotalPrice)}.{' '}
              {saving > 0 ? `The patient saves ${formatRupees(saving)} (${Math.round((saving / testsTotalPrice) * 100)}%).` : 'The package costs the patient no less.'}
            </p>
          )}
        </div>

        <PackageProfitFields
          idPrefix="pkp"
          mode={mode}
          onModeChange={(next) => {
            setMode(next);
            setError(null);
            setSaved(false);
          }}
          profit={profit}
          onProfitChange={(next) => {
            setProfit(next);
            setError(null);
            setSaved(false);
          }}
          testsRunningCost={testsRunningCost}
          error={error}
          disabled={!canEdit}
        />

        <ProfitBreakdownView breakdown={breakdown} />

        {canEdit && (
          <div className="flex items-center justify-end gap-3">
            {saved && !isDirty && (
              <span role="status" className="text-sm text-muted-foreground">
                Saved
              </span>
            )}
            <Button type="button" onClick={handleSave} disabled={!isDirty || mutation.isPending}>
              {mutation.isPending ? 'Saving…' : 'Save price and profit'}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
