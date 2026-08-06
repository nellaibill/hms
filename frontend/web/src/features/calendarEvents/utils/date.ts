// Mirrors features/weeklyRosters/utils/week.ts's convention: all arithmetic uses UTC-anchored
// Date objects purely as a calendar calculator, so a user east of UTC never sees a date shift
// by a day. Event dates are plain calendar dates ("YYYY-MM-DD"), no time component.

export function parseIsoDate(iso: string): Date {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}

export function formatIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function todayIso(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

export function addDays(iso: string, days: number): string {
  const date = parseIsoDate(iso);
  date.setUTCDate(date.getUTCDate() + days);
  return formatIsoDate(date);
}

export function addMonths(year: number, month: number, delta: number): { year: number; month: number } {
  const total = year * 12 + (month - 1) + delta;
  return { year: Math.floor(total / 12), month: (((total % 12) + 12) % 12) + 1 };
}

export function isoDateRangesOverlap(aStart: string, aEnd: string, bStart: string, bEnd: string): boolean {
  return aStart <= bEnd && bStart <= aEnd;
}

export const WEEKDAY_LABELS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
export const MONTH_LABELS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];

export interface MonthGridDay {
  iso: string;
  dayOfMonth: number;
  isCurrentMonth: boolean;
  isWeekend: boolean;
  isToday: boolean;
}

/** The 6x7 day grid (Sun-start) for a given month, including the leading/trailing days of the adjacent months needed to fill whole weeks. */
export function buildMonthGrid(year: number, month: number): MonthGridDay[] {
  const firstOfMonth = new Date(Date.UTC(year, month - 1, 1));
  const startWeekday = firstOfMonth.getUTCDay();
  const gridStart = new Date(firstOfMonth);
  gridStart.setUTCDate(gridStart.getUTCDate() - startWeekday);

  const today = todayIso();

  return Array.from({ length: 42 }, (_, i) => {
    const date = new Date(gridStart);
    date.setUTCDate(date.getUTCDate() + i);
    const iso = formatIsoDate(date);
    const weekday = date.getUTCDay();
    return {
      iso,
      dayOfMonth: date.getUTCDate(),
      isCurrentMonth: date.getUTCMonth() === month - 1,
      isWeekend: weekday === 0 || weekday === 6,
      isToday: iso === today,
    };
  });
}

export function formatDisplayDate(iso: string): string {
  return parseIsoDate(iso).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', timeZone: 'UTC' });
}
