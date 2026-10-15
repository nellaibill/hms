import type { AppointmentStatus } from '@hms/shared';

/** Appointment dates are "yyyy-MM-dd" and times "HH:mm:ss" — hospital-local values from the
 * API's DateOnly/TimeOnly, never instants. These helpers keep them as plain local values. */

/** "09:30:00" -> "9:30 AM". */
export function formatSlotTime(time: string): string {
  const [hourText, minuteText] = time.split(':');
  const hour = Number(hourText);
  const minute = Number(minuteText);
  if (Number.isNaN(hour) || Number.isNaN(minute)) return time;
  const suffix = hour >= 12 ? 'PM' : 'AM';
  const displayHour = hour % 12 === 0 ? 12 : hour % 12;
  return `${displayHour}:${String(minute).padStart(2, '0')} ${suffix}`;
}

export function formatSlotRange(start: string, end: string): string {
  return `${formatSlotTime(start)} – ${formatSlotTime(end)}`;
}

/** "2026-10-15" -> "Thu, 15 Oct 2026". */
export function formatAppointmentDate(date: string): string {
  const parsed = new Date(`${date}T00:00:00`);
  if (Number.isNaN(parsed.getTime())) return date;
  return parsed.toLocaleDateString('en-GB', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' });
}

/** Adds whole days to a "yyyy-MM-dd" value, in the local calendar. */
export function addDays(date: string, days: number): string {
  const parsed = new Date(`${date}T00:00:00`);
  parsed.setDate(parsed.getDate() + days);
  const y = parsed.getFullYear();
  const m = String(parsed.getMonth() + 1).padStart(2, '0');
  const d = String(parsed.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** The slot's start as an ISO instant — what a visit's appointmentTime expects. The browser is
 * in the hospital's time zone, so local parsing gives the right instant. */
export function slotStartIso(date: string, time: string): string {
  return new Date(`${date}T${time.length === 5 ? `${time}:00` : time}`).toISOString();
}

/** Splits a caller's name the way registration wants it: first word, then the rest. */
export function splitPatientName(name: string): { firstName: string; lastName: string } {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length <= 1) return { firstName: parts[0] ?? '', lastName: '' };
  return { firstName: parts[0], lastName: parts.slice(1).join(' ') };
}

export const SOURCE_LABELS = { Phone: 'Phone', InPerson: 'At the desk', Other: 'Other' } as const;

const STATUS_LABELS: Record<AppointmentStatus, string> = {
  Booked: 'Booked',
  Arrived: 'Arrived',
  Cancelled: 'Cancelled',
  NoShow: 'No-show',
};

export function appointmentStatusLabel(status: AppointmentStatus): string {
  return STATUS_LABELS[status];
}
