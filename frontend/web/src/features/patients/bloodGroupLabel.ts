import type { BloodGroup } from '@hms/shared';

/** "APositive" -> "A+ve" etc., matching LH Software.docx's "A+ve, A-ve, B+ve, B-ve, AB+ve, AB-ve, O+ve, O-ve" labels. */
const LABELS: Record<Exclude<BloodGroup, 'Unknown'>, string> = {
  APositive: 'A+ve',
  ANegative: 'A-ve',
  BPositive: 'B+ve',
  BNegative: 'B-ve',
  ABPositive: 'AB+ve',
  ABNegative: 'AB-ve',
  OPositive: 'O+ve',
  ONegative: 'O-ve',
};

export function bloodGroupLabel(bloodGroup: BloodGroup): string {
  return bloodGroup === 'Unknown' ? 'Unknown' : LABELS[bloodGroup];
}
