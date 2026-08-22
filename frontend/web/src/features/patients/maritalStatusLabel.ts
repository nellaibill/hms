import type { MaritalStatus } from '@hms/shared';

/** "NA" -> "N/A" for display; Married/Unmarried are already display-ready. */
export function maritalStatusLabel(maritalStatus: MaritalStatus): string {
  return maritalStatus === 'NA' ? 'N/A' : maritalStatus;
}
