/**
 * The hospital's clock and calendar (ADR-147). Every "now", every "today" and every time the app
 * shows is India Standard Time by the *server's* clock — never the desk PC's own clock or its
 * time-zone setting. Either can be wrong on a counter PC: a fresh Windows install defaults to
 * Pacific Time, and a clock can drift or be set by hand. Bills, payments and visits are already
 * stamped by the server; this file makes the screens read those stamps, and pick "today", the
 * same way the server does (HMS.Shared.Kernel.HospitalTimeZone). It lives in @hms/shared so the
 * web app, the shared form validation and the mobile app hold one copy of the server's clock.
 */

export const HOSPITAL_TIME_ZONE = 'Asia/Kolkata';

/** India is UTC+05:30 all year (no daylight saving since 1945), so a fixed offset is exact. */
const HOSPITAL_UTC_OFFSET = '+05:30';
const HOSPITAL_OFFSET_MS = 330 * 60_000;
const DATE_ONLY = /^\d{4}-\d{2}-\d{2}$/;

/** Server clock minus this PC's clock, learnt from API responses. 0 until the first one. */
let serverOffsetMs = 0;

/** Takes the server's clock from an API response's Date header (an HTTP-date). The header is
 * cut to the whole second, so the server's real time is up to a second later than it says;
 * half a second is added to centre that. A missing or unreadable header changes nothing. */
export function syncServerClock(dateHeader: string | null | undefined, receivedAt: number = Date.now()): void {
  if (!dateHeader) return;
  const serverTime = Date.parse(dateHeader);
  if (Number.isNaN(serverTime)) return;
  serverOffsetMs = serverTime + 500 - receivedAt;
}

/** How far this PC's clock is behind the server's, in milliseconds (negative when ahead). */
export function serverClockOffsetMs(): number {
  return serverOffsetMs;
}

/** The current moment by the server's clock. Use this, never `new Date()`, for "now". */
export function serverNow(): Date {
  return new Date(Date.now() + serverOffsetMs);
}

function toDate(instant: Date | string | number): Date {
  return instant instanceof Date ? instant : new Date(instant);
}

/** The calendar day in India (YYYY-MM-DD, the `<input type="date">` format) at an instant, e.g.
 * a bill's createdAt. Not `toISOString().slice(0, 10)`, which is the UTC day and still says
 * yesterday until 5:30 AM. Empty for an invalid instant. */
export function hospitalDateOf(instant: Date | string | number): string {
  const time = toDate(instant).getTime();
  if (Number.isNaN(time)) return '';
  return new Date(time + HOSPITAL_OFFSET_MS).toISOString().slice(0, 10);
}

/** Today's date in India by the server's clock (YYYY-MM-DD). */
export function hospitalToday(): string {
  return hospitalDateOf(serverNow());
}

/** The instant a day (YYYY-MM-DD) starts in India: 00:00 IST, 18:30 UTC the day before. */
export function hospitalDayStart(date: string | undefined): Date | undefined {
  if (!date || !DATE_ONLY.test(date)) return undefined;
  const start = new Date(`${date}T00:00:00.000${HOSPITAL_UTC_OFFSET}`);
  return Number.isNaN(start.getTime()) ? undefined : start;
}

/** The last millisecond of a day (YYYY-MM-DD) in India: 23:59:59.999 IST. */
export function hospitalDayEnd(date: string | undefined): Date | undefined {
  if (!date || !DATE_ONLY.test(date)) return undefined;
  const end = new Date(`${date}T23:59:59.999${HOSPITAL_UTC_OFFSET}`);
  return Number.isNaN(end.getTime()) ? undefined : end;
}

/** A YYYY-MM-DD date moved by whole days (negative for earlier), with no clock or zone involved. */
export function addDaysToDate(date: string, days: number): string {
  if (!DATE_ONLY.test(date)) return '';
  const [year, month, day] = date.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day + days)).toISOString().slice(0, 10);
}

/** The year, month (1–12), day, hour and minute in India at an instant — for the few places that
 * build a calendar or count age from parts, instead of the PC-zone getters (getDate(), …). */
export function hospitalDateParts(instant: Date | string | number = serverNow()): {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
  weekday: number;
} {
  const shifted = new Date(toDate(instant).getTime() + HOSPITAL_OFFSET_MS);
  return {
    year: shifted.getUTCFullYear(),
    month: shifted.getUTCMonth() + 1,
    day: shifted.getUTCDate(),
    hour: shifted.getUTCHours(),
    minute: shifted.getUTCMinutes(),
    weekday: shifted.getUTCDay(),
  };
}

const DATE_TIME_INPUT = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d{1,3})?)?$/;

/** An instant as an `<input type="datetime-local">` value ("YYYY-MM-DDTHH:mm"), in India's time
 * (ADR-147) — not the PC's zone, and not the UTC time `iso.slice(0, 16)` gives. Empty for an
 * invalid instant. */
export function toHospitalDateTimeInput(instant: Date | string | number): string {
  const time = toDate(instant).getTime();
  if (Number.isNaN(time)) return '';
  return new Date(time + HOSPITAL_OFFSET_MS).toISOString().slice(0, 16);
}

/** An `<input type="datetime-local">` value, read as India's time (ADR-147), as the ISO instant
 * the API takes. Throws a RangeError for a value that isn't a date and time, as
 * `new Date(value).toISOString()` did. */
export function fromHospitalDateTimeInput(value: string): string {
  if (!DATE_TIME_INPUT.test(value)) return new Date(Number.NaN).toISOString();
  return new Date(`${value.length === 16 ? `${value}:00` : value}${HOSPITAL_UTC_OFFSET}`).toISOString();
}

const INSTALLED = Symbol.for('hms.hospitalTimeDisplay');

/** The YYYY-MM-DD date `years` whole years before another (29 Feb stays 29 Feb: it compares
 * correctly as text, which is all the age limits need). */
export function yearsBeforeDate(date: string, years: number): string {
  if (!DATE_ONLY.test(date)) return '';
  return `${String(Number(date.slice(0, 4)) - years).padStart(4, '0')}${date.slice(4)}`;
}

/**
 * Makes every date and time the app shows India Standard Time, whatever the PC's time-zone
 * setting: `toLocaleString`, `toLocaleDateString` and `toLocaleTimeString` get
 * `timeZone: 'Asia/Kolkata'` unless a call names its own zone. One switch in main.tsx instead
 * of a `timeZone` option on each of the app's ~180 calls (and the ones inside PDF and chart
 * libraries). The output format is unchanged: a time zone alone doesn't pick any fields.
 */
export function installHospitalTimeDisplay(): void {
  const proto = Date.prototype as Date & { [INSTALLED]?: boolean };
  if (proto[INSTALLED]) return;
  for (const method of ['toLocaleString', 'toLocaleDateString', 'toLocaleTimeString'] as const) {
    const original = proto[method] as (this: Date, locales?: Intl.LocalesArgument, options?: Intl.DateTimeFormatOptions) => string;
    Object.defineProperty(proto, method, {
      configurable: true,
      writable: true,
      value: function (this: Date, locales?: Intl.LocalesArgument, options?: Intl.DateTimeFormatOptions) {
        return original.call(this, locales, { timeZone: HOSPITAL_TIME_ZONE, ...options });
      },
    });
  }
  proto[INSTALLED] = true;
}
