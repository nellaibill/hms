import { todayIso } from '@/features/pharmacy/common/format';

export interface ReportPeriod {
  fromDate: string;
  toDate: string;
}

export type PeriodPreset = 'today' | 'yesterday' | 'last7' | 'month' | 'lastMonth' | 'quarter' | 'financialYear' | 'custom';

export const PERIOD_PRESETS: { key: Exclude<PeriodPreset, 'custom'>; label: string }[] = [
  { key: 'today', label: 'Today' },
  { key: 'yesterday', label: 'Yesterday' },
  { key: 'last7', label: 'Last 7 days' },
  { key: 'month', label: 'This month' },
  { key: 'lastMonth', label: 'Last month' },
  { key: 'quarter', label: 'This quarter' },
  { key: 'financialYear', label: 'Financial year' },
];

function iso(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function parts(isoDate: string): { year: number; month: number; day: number } {
  const [year, month, day] = isoDate.split('-').map(Number);
  return { year, month, day };
}

/** An ISO date moved by a number of days. */
export function addDays(isoDate: string, days: number): string {
  const { year, month, day } = parts(isoDate);
  return iso(new Date(Date.UTC(year, month - 1, day + days)));
}

/**
 * The dates a preset stands for, as of `today`. Periods end today rather than at the end of
 * the month or year — a report on "this month" is of the month so far. The financial year
 * runs April to March, as GST returns and the hospital's books do.
 */
export function presetPeriod(preset: Exclude<PeriodPreset, 'custom'>, today: string = todayIso()): ReportPeriod {
  const { year, month } = parts(today);
  switch (preset) {
    case 'today':
      return { fromDate: today, toDate: today };
    case 'yesterday':
      return { fromDate: addDays(today, -1), toDate: addDays(today, -1) };
    case 'last7':
      return { fromDate: addDays(today, -6), toDate: today };
    case 'month':
      return { fromDate: iso(new Date(Date.UTC(year, month - 1, 1))), toDate: today };
    case 'lastMonth':
      return { fromDate: iso(new Date(Date.UTC(year, month - 2, 1))), toDate: iso(new Date(Date.UTC(year, month - 1, 0))) };
    case 'quarter': {
      // Quarters of the April–March year: Apr–Jun, Jul–Sep, Oct–Dec, Jan–Mar.
      const firstMonth = month - ((month + 8) % 3);
      return { fromDate: iso(new Date(Date.UTC(year, firstMonth - 1, 1))), toDate: today };
    }
    case 'financialYear':
      return { fromDate: `${month >= 4 ? year : year - 1}-04-01`, toDate: today };
  }
}

/** Which preset, if any, a pair of dates is. */
export function matchPreset(period: ReportPeriod, today: string = todayIso()): PeriodPreset {
  const match = PERIOD_PRESETS.find((preset) => {
    const candidate = presetPeriod(preset.key, today);
    return candidate.fromDate === period.fromDate && candidate.toDate === period.toDate;
  });
  return match?.key ?? 'custom';
}

/** "01 Oct 2026 – 14 Oct 2026", or one date when the period is a single day. */
export function periodLabel(period: ReportPeriod): string {
  const format = (isoDate: string) => new Date(`${isoDate}T00:00:00Z`).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC' });
  return period.fromDate === period.toDate ? format(period.fromDate) : `${format(period.fromDate)} – ${format(period.toDate)}`;
}
