import { hospitalDateOf, hospitalDateParts, serverNow } from '@/lib/hospitalTime';

/**
 * Whole-number age in years, matching the backend's Patient.Age computed property
 * (HMS.Modules.Patients.Domain.Patient — "Age is always derived from DateOfBirth, never
 * stored"). The mock store mirrors that: age is recalculated from dateOfBirth on every
 * read rather than trusted from whatever was last persisted, so it can never go stale.
 */
export function calculateAge(dateOfBirth: string, today: Date = serverNow()): number {
  const dob = birthDateParts(dateOfBirth);
  if (!dob) {
    return 0;
  }
  const now = hospitalDateParts(today);

  let age = now.year - dob.year;
  const monthDiff = now.month - dob.month;
  if (monthDiff < 0 || (monthDiff === 0 && now.day < dob.day)) {
    age -= 1;
  }
  return Math.max(age, 0);
}

/** A date of birth's year/month/day as written (it's a calendar date, not an instant), or, for a
 * full timestamp, its day in India. Null when it isn't a date. */
function birthDateParts(dateOfBirth: string): { year: number; month: number; day: number } | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(dateOfBirth);
  if (match) return { year: Number(match[1]), month: Number(match[2]), day: Number(match[3]) };
  const parsed = new Date(dateOfBirth);
  return Number.isNaN(parsed.getTime()) ? null : hospitalDateParts(parsed);
}

/** Calendar-aware Years/Months/Days breakdown for display next to the Date of Birth field — distinct from the whole-number `age` above. */
export function calculateDetailedAge(dateOfBirth: string, today: Date = serverNow()): string | null {
  const dob = birthDateParts(dateOfBirth);
  if (!dob) {
    return null;
  }
  // Ages count in India's calendar days by the server's clock (ADR-147), not the PC's.
  const now = hospitalDateParts(today);
  const dobKey = dob.year * 10_000 + dob.month * 100 + dob.day;
  if (dobKey > now.year * 10_000 + now.month * 100 + now.day) {
    return null;
  }

  let years = now.year - dob.year;
  let months = now.month - dob.month;
  let days = now.day - dob.day;

  if (days < 0) {
    months -= 1;
    const daysInPrevMonth = new Date(Date.UTC(now.year, now.month - 1, 0)).getUTCDate();
    days += daysInPrevMonth;
  }
  if (months < 0) {
    years -= 1;
    months += 12;
  }

  const parts: string[] = [];
  if (years > 0) parts.push(`${years} Year${years === 1 ? '' : 's'}`);
  if (months > 0) parts.push(`${months} Month${months === 1 ? '' : 's'}`);
  if (days > 0 || parts.length === 0) parts.push(`${days} Day${days === 1 ? '' : 's'}`);

  return parts.join(', ');
}

/**
 * `min`/`max` bounds for a Date of Birth `<input type="date">` — a native date input has no
 * built-in limit on how many digits can be typed into its year segment (Chromium allows up
 * to 6), so without these a stray extra digit (e.g. "202506") can slip through. `max` blocks
 * a future date at the picker level; `min` mirrors the 100-year age cap already enforced by
 * patientRegistrationUiValidation.ts's Zod schema and the backend's
 * CreatePatientRequestValidator.MaxAgeYears, so all three stay in sync.
 */
export function dateOfBirthInputBounds(today: Date = serverNow()): { min: string; max: string } {
  // Today in India, not the UTC day (`toISOString()`), which until 5:30 AM is yesterday and kept
  // a baby born after midnight out of the picker (ADR-147).
  const max = hospitalDateOf(today);
  const [year, rest] = [Number(max.slice(0, 4)), max.slice(4)];
  return { min: `${year - 100}${rest === '-02-29' ? '-02-28' : rest}`, max };
}
