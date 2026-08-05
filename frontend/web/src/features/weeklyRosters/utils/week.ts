// Dates throughout HR (WeeklyRoster.weekStartDate, ShiftAssignment.rosterDate) are plain
// calendar dates ("YYYY-MM-DD", .NET DateOnly) with no time component — all arithmetic here
// uses UTC-anchored Date objects purely as a calendar calculator, never converting to/from
// local time, so a user east of UTC (e.g. IST) never sees a date shifted by a day.

function parseIsoDate(iso: string): Date {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}

function formatIsoDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function addDays(iso: string, days: number): string {
  const date = parseIsoDate(iso);
  date.setUTCDate(date.getUTCDate() + days);
  return formatIsoDate(date);
}

export const DAY_LABELS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

/** The 7 calendar dates (Monday–Sunday) making up the week starting at `weekStartDate`. */
export function getWeekDates(weekStartDate: string): string[] {
  return Array.from({ length: 7 }, (_, i) => addDays(weekStartDate, i));
}

export function formatDisplayDate(iso: string): string {
  return parseIsoDate(iso).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', timeZone: 'UTC' });
}

/** This week's Monday, as of the caller's local wall-clock date. */
export function getCurrentWeekStartDate(): string {
  const now = new Date();
  const todayIso = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
  const dayOfWeek = parseIsoDate(todayIso).getUTCDay();
  const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
  return addDays(todayIso, mondayOffset);
}
