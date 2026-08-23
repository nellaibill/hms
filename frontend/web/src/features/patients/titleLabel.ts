import type { Title } from '@hms/shared';

/**
 * Age/gender guidance appended to Baby/Master/Miss so reception picks the right title —
 * Mr/Mrs/Ms/Dr are unambiguous and stay plain. Label only: the underlying value (and its
 * age/gender consistency validation in patientRegistrationUiValidation.ts) is unchanged.
 */
const LABELS: Partial<Record<Title, string>> = {
  Baby: 'Baby — up to 1 year',
  Master: 'Master — 1–18 years, boy',
  Miss: 'Miss — 1–18 years, girl',
};

export function titleLabel(title: Title): string {
  return LABELS[title] ?? title;
}
