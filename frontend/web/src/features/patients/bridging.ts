import {
  ALLERGY_CATEGORIES,
  type AllergyCategory,
  type Gender,
  type PatientGenderUi,
  PHONE_RELATIONS,
  type PhoneRelation,
  RELATIONSHIPS,
  type Relationship,
} from '@hms/shared';
import { humanize } from './humanize';

/**
 * Shared UI-form <-> backend-DTO bridging helpers for the Patients feature — see the
 * top-of-file comment in PatientRegistrationCreatePage.tsx for why this bridge exists
 * (UI ships ahead of the backend in this phase; see docs/DecisionLog.md).
 */

const GENDER_TO_BACKEND: Record<PatientGenderUi, Gender> = {
  Male: 'Male',
  Female: 'Female',
  Transgender: 'Other',
  NA: 'Other',
};

export function toBackendGender(gender: PatientGenderUi): Gender {
  return GENDER_TO_BACKEND[gender];
}

/** Reverse of the above is lossy ("Other" could originally have been Transgender or NA) — defaults to NA. */
export function fromBackendGender(gender: Gender): PatientGenderUi {
  return gender === 'Male' || gender === 'Female' ? gender : 'NA';
}

/** Finds the enum token whose humanized label matches a free-text value stored by an earlier bridge (e.g. "Father In Law" -> "FatherInLaw"). */
function dehumanize<T extends string>(displayValue: string | null | undefined, candidates: readonly T[], fallback: T): T {
  if (!displayValue) {
    return fallback;
  }
  const normalized = displayValue.replace(/[\s-]/g, '').toLowerCase();
  const match = candidates.find((candidate) => humanize(candidate).replace(/[\s-]/g, '').toLowerCase() === normalized);
  return match ?? fallback;
}

export function toPhoneRelationLabel(relation: PhoneRelation): string {
  return humanize(relation);
}

export function fromPhoneRelationLabel(label: string | null | undefined): PhoneRelation {
  return dehumanize(label, PHONE_RELATIONS, 'Self');
}

export function toRelationshipLabel(relationship: Relationship): string {
  return humanize(relationship);
}

export function fromRelationshipLabel(label: string | null | undefined): Relationship {
  return dehumanize(label, RELATIONSHIPS, 'Father');
}

/** Composes the UI's category+specify pair into the single free-text backend AllergyType field. */
export function toAllergyType(category: string, specify: string): string | undefined {
  return [category, specify].filter(Boolean).join(': ') || undefined;
}

/** Best-effort reverse of toAllergyType — splits on the first ": " and matches the category against the known list. */
export function fromAllergyType(allergyType: string | null | undefined): { category: AllergyCategory | ''; specify: string } {
  if (!allergyType) {
    return { category: '', specify: '' };
  }
  const [prefix, ...rest] = allergyType.split(': ');
  const category = ALLERGY_CATEGORIES.find((c) => c.toLowerCase() === prefix.trim().toLowerCase());
  return category ? { category, specify: rest.join(': ') } : { category: '', specify: allergyType };
}
