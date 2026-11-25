import type { IdProofType } from '@hms/shared';

/** "DrivingLicense" -> "Driving Licence" etc. — the stored value is a code, not a label. */
const LABELS: Record<IdProofType, string> = {
  Aadhaar: 'Aadhaar',
  Passport: 'Passport',
  DrivingLicense: 'Driving Licence',
  VoterId: 'Voter ID',
  Other: 'Other',
};

export function idProofTypeLabel(type: IdProofType | string): string {
  return LABELS[type as IdProofType] ?? type;
}
