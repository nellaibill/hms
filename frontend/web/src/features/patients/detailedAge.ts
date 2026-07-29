/**
 * Whole-number age in years, matching the backend's Patient.Age computed property
 * (HMS.Modules.Patients.Domain.Patient — "Age is always derived from DateOfBirth, never
 * stored"). The mock store mirrors that: age is recalculated from dateOfBirth on every
 * read rather than trusted from whatever was last persisted, so it can never go stale.
 */
export function calculateAge(dateOfBirth: string, today: Date = new Date()): number {
  const dob = new Date(dateOfBirth);
  if (Number.isNaN(dob.getTime())) {
    return 0;
  }

  let age = today.getFullYear() - dob.getFullYear();
  const monthDiff = today.getMonth() - dob.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
    age -= 1;
  }
  return Math.max(age, 0);
}

/** Calendar-aware Years/Months/Days breakdown for display next to the Date of Birth field — distinct from the whole-number `age` above. */
export function calculateDetailedAge(dateOfBirth: string, today: Date = new Date()): string | null {
  const dob = new Date(dateOfBirth);
  if (Number.isNaN(dob.getTime()) || dob.getTime() > today.getTime()) {
    return null;
  }

  let years = today.getFullYear() - dob.getFullYear();
  let months = today.getMonth() - dob.getMonth();
  let days = today.getDate() - dob.getDate();

  if (days < 0) {
    months -= 1;
    const daysInPrevMonth = new Date(today.getFullYear(), today.getMonth(), 0).getDate();
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
