/** Number and date formatting shared by every pharmacy screen, so a rate, a quantity and an
 * expiry read the same way on the counter, a purchase and a report. */

const moneyFormat = new Intl.NumberFormat('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const quantityFormat = new Intl.NumberFormat('en-IN', { maximumFractionDigits: 3 });

export function money(value: number | null | undefined): string {
  return moneyFormat.format(value ?? 0);
}

export function rupees(value: number | null | undefined): string {
  return `₹${money(value)}`;
}

export function quantity(value: number | null | undefined): string {
  return quantityFormat.format(value ?? 0);
}

export function round2(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}

/** "03/27" from an ISO date — how an expiry is printed on a strip. */
export function expiryLabel(isoDate: string | null | undefined): string {
  if (!isoDate) return '—';
  const [year, month] = isoDate.split('-');
  return `${month}/${year.slice(2)}`;
}

/**
 * Reads an expiry the way it is typed off a pack — "03/27", "0327", "3-2027", "2027-03" — and
 * returns the last day of that month as an ISO date, which is when such a batch expires. A
 * full ISO date is passed through. Returns null for anything it cannot read.
 */
export function parseExpiry(text: string): string | null {
  const value = text.trim();
  if (!value) return null;

  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) return value;

  let month: number;
  let year: number;
  const yearFirst = /^(\d{4})[-/.](\d{1,2})$/.exec(value);
  const monthFirst = /^(\d{1,2})[-/.\s](\d{2}|\d{4})$/.exec(value);
  const compact = /^(\d{2})(\d{2})$/.exec(value);

  if (yearFirst) {
    year = Number(yearFirst[1]);
    month = Number(yearFirst[2]);
  } else if (monthFirst) {
    month = Number(monthFirst[1]);
    year = Number(monthFirst[2]);
  } else if (compact) {
    month = Number(compact[1]);
    year = Number(compact[2]);
  } else {
    return null;
  }

  if (year < 100) year += 2000;
  if (month < 1 || month > 12 || year < 2000 || year > 2100) return null;

  const lastDay = new Date(Date.UTC(year, month, 0)).getUTCDate();
  return `${year}-${String(month).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`;
}

/** Today in the hospital's own time zone, as an ISO date. */
export function todayIso(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Kolkata' }).format(new Date());
}

export function dateLabel(isoDate: string | null | undefined): string {
  if (!isoDate) return '—';
  return new Date(isoDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', timeZone: isoDate.length > 10 ? 'Asia/Kolkata' : 'UTC' });
}

export function dateTimeLabel(isoDateTime: string | null | undefined): string {
  if (!isoDateTime) return '—';
  return new Date(isoDateTime).toLocaleString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit', timeZone: 'Asia/Kolkata' });
}

/** Whole days from today until an ISO date; negative once it has passed. */
export function daysUntil(isoDate: string): number {
  const today = new Date(`${todayIso()}T00:00:00Z`).getTime();
  return Math.round((new Date(`${isoDate.slice(0, 10)}T00:00:00Z`).getTime() - today) / 86_400_000);
}

/** A number typed into a grid cell; blank or unreadable is 0. */
export function toNumber(text: string | number | null | undefined): number {
  if (typeof text === 'number') return Number.isFinite(text) ? text : 0;
  const parsed = Number.parseFloat((text ?? '').toString().replace(/,/g, ''));
  return Number.isFinite(parsed) ? parsed : 0;
}
