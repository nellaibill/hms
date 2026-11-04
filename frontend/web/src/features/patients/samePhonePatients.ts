import type { Patient } from '@hms/shared';
import { queryOptions } from '@tanstack/react-query';
import { patientsApi } from '../../services/apiClient';

/** A primary phone complete enough to look up — the registration form's own 10-digit rule. */
export function isCompletePhone(phone: string | undefined): phone is string {
  return /^[0-9]{10}$/.test(phone?.trim() ?? '');
}

/** The full name, case- and spacing-insensitive, whichever way it's split between first and last
 * name — mirrors the API's PatientDuplicateMatching.FullNameKey. Patients imported from the old
 * system carry their whole name in firstName with lastName blank ("Jeya Sathya" + ""), so a
 * registration typed as "Jeya" + "Sathya" is the same name. */
export function fullNameKey(firstName: string | undefined, lastName: string | undefined): string {
  return `${firstName ?? ''} ${lastName ?? ''}`.trim().split(/\s+/).join(' ').toLowerCase();
}

export function isSameFullName(patient: Pick<Patient, 'firstName' | 'lastName'>, firstName: string | undefined, lastName: string | undefined): boolean {
  const key = fullNameKey(firstName, lastName);
  return key !== '' && fullNameKey(patient.firstName, patient.lastName) === key;
}

/** At most this many are listed — a number is shared by a handful of family members at most. */
const SAME_PHONE_LIMIT = 10;

/**
 * Patients already registered with this phone (primary or secondary) — what New Patient
 * Registration warns about before a returning patient gets a second UHID. Shared by the inline
 * notice (useQuery) and the check run right before the patient is created (fetchQuery), so both
 * read the same cache entry.
 */
export function samePhonePatientsQueryOptions(phone: string) {
  return queryOptions({
    queryKey: ['patients', 'same-phone', phone] as const,
    queryFn: async (): Promise<Patient[]> =>
      (await patientsApi.getPatients({ phone, pageSize: SAME_PHONE_LIMIT, includeLastVisit: true })).items,
    staleTime: 30_000,
  });
}
