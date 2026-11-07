import { DateInput } from '@/components/ui/date-input';
import { todayIso } from '@/features/pharmacy/common/format';
import { cn } from '@/lib/utils';
import { matchPreset, PERIOD_PRESETS, presetPeriod, type ReportPeriod } from './period';


interface PeriodPickerProps {
  value: ReportPeriod;
  onChange: (period: ReportPeriod) => void;
}

/** Quick periods a pharmacy asks for most (today, this month, the financial year…) and a from/to pair for anything else. */
export function PeriodPicker({ value, onChange }: PeriodPickerProps) {
  const active = matchPreset(value);
  const today = todayIso();

  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className="flex flex-wrap overflow-hidden rounded-md border border-border text-xs font-medium">
        {PERIOD_PRESETS.map((preset) => (
          <button
            key={preset.key}
            type="button"
            onClick={() => onChange(presetPeriod(preset.key))}
            className={cn('px-2.5 py-1.5', active === preset.key ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-accent')}
          >
            {preset.label}
          </button>
        ))}
      </div>
      <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
        From
        <DateInput value={value.fromDate} max={value.toDate} onChange={(event) => event.target.value && onChange({ ...value, fromDate: event.target.value })} className="h-8 w-36 px-2 text-xs" />
      </label>
      <label className="flex items-center gap-1.5 text-xs text-muted-foreground">
        To
        <DateInput value={value.toDate} min={value.fromDate} max={today} onChange={(event) => event.target.value && onChange({ ...value, toDate: event.target.value })} className="h-8 w-36 px-2 text-xs" />
      </label>
    </div>
  );
}
