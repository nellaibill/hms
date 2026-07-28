/** Calendar-aware Years/Months/Days breakdown for display next to the Date of Birth field — distinct from the whole-number `age` stored on the Patient record. */
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
