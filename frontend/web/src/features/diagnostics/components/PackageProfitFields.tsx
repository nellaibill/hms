import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { cn } from '@/lib/utils';
import { formatRupees, type PackageProfitMode } from '../tariff';

interface PackageProfitFieldsProps {
  idPrefix: string;
  mode: PackageProfitMode;
  onModeChange: (mode: PackageProfitMode) => void;
  profit: string;
  onProfitChange: (profit: string) => void;
  /** The tests' total running cost; null when a test isn't costed yet, undefined while the
   * package has no tests (the create form). */
  testsRunningCost: number | null | undefined;
  error?: string | null;
  disabled?: boolean;
}

/**
 * "How is the profit set?" for a lab package (ADR-140): enter the profit as the tariff sheet
 * writes it ("CBC – 300, Profit – 120"), or use the package's tests' running costs.
 */
export function PackageProfitFields({ idPrefix, mode, onModeChange, profit, onProfitChange, testsRunningCost, error, disabled = false }: PackageProfitFieldsProps) {
  const testsHint =
    testsRunningCost === undefined
      ? 'Running cost = the total of the tests you add.'
      : testsRunningCost === null
        ? 'Not every test has a running cost yet, so the profit can’t be worked out.'
        : `Running cost = ${formatRupees(testsRunningCost)}, the total of its tests.`;

  return (
    <fieldset className="flex flex-col gap-2.5" disabled={disabled}>
      <legend className="mb-1 text-sm font-medium text-foreground">How is the profit set?</legend>

      <label
        className={cn(
          'flex cursor-pointer items-start gap-3 rounded-lg border p-3',
          mode === 'profit' ? 'border-primary bg-primary/5' : 'border-border',
        )}
      >
        <input
          type="radio"
          name={`${idPrefix}-profit-mode`}
          className="mt-0.5 h-5 w-5 accent-primary"
          checked={mode === 'profit'}
          onChange={() => onModeChange('profit')}
        />
        <span className="flex flex-col gap-0.5">
          <span className="text-sm font-semibold">Enter the profit, as in the tariff sheet</span>
          <span className="text-xs text-muted-foreground">e.g. CBC – ₹300, profit ₹120 (40%). The running cost is worked out from it.</span>
        </span>
      </label>

      {mode === 'profit' && (
        <div className="flex flex-col gap-1.5 pl-8">
          <Label htmlFor={`${idPrefix}-profit`}>Profit (₹)</Label>
          <Input id={`${idPrefix}-profit`} type="number" min={0} step="any" value={profit} onChange={(event) => onProfitChange(event.target.value)} />
          {error && <p className="text-sm text-destructive">{error}</p>}
        </div>
      )}

      <label
        className={cn(
          'flex cursor-pointer items-start gap-3 rounded-lg border p-3',
          mode === 'tests' ? 'border-primary bg-primary/5' : 'border-border',
        )}
      >
        <input
          type="radio"
          name={`${idPrefix}-profit-mode`}
          className="mt-0.5 h-5 w-5 accent-primary"
          checked={mode === 'tests'}
          onChange={() => onModeChange('tests')}
        />
        <span className="flex flex-col gap-0.5">
          <span className="text-sm font-semibold">Use the tests' running costs</span>
          <span className="text-xs text-muted-foreground">{testsHint}</span>
        </span>
      </label>
    </fieldset>
  );
}
