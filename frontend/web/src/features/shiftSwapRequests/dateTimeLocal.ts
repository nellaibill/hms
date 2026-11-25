import { fromHospitalDateTimeInput, toHospitalDateTimeInput } from '@/lib/hospitalTime';
/**
 * `<input type="datetime-local">` produces a "Kind=Unspecified" value ("2026-08-05T10:00")
 * with no timezone info — the backend's RequestedDate/ApprovedDate columns are Postgres
 * `timestamp with time zone`, and Npgsql rejects writing an Unspecified-kind DateTime into
 * one. Converting to a proper UTC ISO string before sending is a wire-format fix, not
 * business logic — the backend remains the source of truth for what these fields mean.
 */
export function toUtcIso(dateTimeLocalValue: string): string {
  // Read as India's time, whatever the PC's zone (ADR-147).
  return fromHospitalDateTimeInput(dateTimeLocalValue);
}

/** Inverse — trims an ISO string (from the backend) down to the "YYYY-MM-DDTHH:mm" shape
 * `<input type="datetime-local">` expects. */
export function toDateTimeLocalInput(isoValue: string): string {
  // India's time, not the UTC time `isoValue.slice(0, 16)` gave (ADR-147).
  return toHospitalDateTimeInput(isoValue);
}
