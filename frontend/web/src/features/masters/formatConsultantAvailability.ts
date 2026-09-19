/** A subset of Consultant/OpdConsultationSummaryItem carrying just the availability fields —
 * shared by the Masters Consultant list (row subtitle) and the OPD Consultation List tab
 * (Availability column), so both render this identically. */
export interface ConsultantAvailability {
  availableDays: string[];
  visitStartTime?: string | null;
  visitEndTime?: string | null;
  visitStartTime2?: string | null;
  visitEndTime2?: string | null;
}

function formatTime(time?: string | null): string | undefined {
  if (!time) return undefined;
  // time is "HH:mm:ss" (TimeOnly's JSON shape) — anchor it to an arbitrary date just to reuse
  // the locale time formatter.
  return new Date(`1970-01-01T${time}`).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
}

function formatRange(start?: string | null, end?: string | null): string | undefined {
  const from = formatTime(start);
  const to = formatTime(end);
  return from && to ? `${from} – ${to}` : undefined;
}

/** "Mon, Wed, Fri · 10:00 AM – 1:00 PM, 4:00 PM – 6:00 PM" — undefined until the consultant's
 * Masters record has this set (see Consultant Type/Availability on the Masters > Consultant
 * form). The second session is only appended when it's actually set. */
export function formatConsultantAvailability(availability: ConsultantAvailability): string | undefined {
  const days = availability.availableDays.map((day) => day.slice(0, 3)).join(', ');
  const timeRanges = [
    formatRange(availability.visitStartTime, availability.visitEndTime),
    formatRange(availability.visitStartTime2, availability.visitEndTime2),
  ]
    .filter(Boolean)
    .join(', ');
  const text = [days, timeRanges].filter(Boolean).join(' · ');
  return text || undefined;
}
