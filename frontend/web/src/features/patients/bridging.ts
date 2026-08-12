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

// The backend's Gender enum now matches PATIENT_GENDERS exactly (Male/Female/Transgender/NA
// — see enums/patients.ts), so this is a lossless 1:1 identity mapping in both directions.
export function toBackendGender(gender: PatientGenderUi): Gender {
  return gender;
}

export function fromBackendGender(gender: Gender): PatientGenderUi {
  return gender;
}

/**
 * Finds the enum token whose humanized label matches a free-text value stored by an earlier
 * bridge (e.g. "Father In Law" -> "FatherInLaw"). Falls back to `fallback` (rather than
 * throwing or leaving the field blank) only for an empty/missing stored value — an
 * unrecognized non-empty value (e.g. pre-dropdown free text like "Parent") intentionally does
 * NOT fall back here; see the two callers below.
 */
function dehumanize<T extends string>(displayValue: string | null | undefined, candidates: readonly T[], fallback: T): T {
  if (!displayValue) {
    return fallback;
  }
  const normalized = displayValue.replace(/[\s-]/g, '').toLowerCase();
  const match = candidates.find((candidate) => humanize(candidate).replace(/[\s-]/g, '').toLowerCase() === normalized);
  if (match) {
    return match;
  }
  // A stored value that doesn't match any known option must not be silently presented as one
  // of the *specific* candidates (e.g. "Parent" or "a" rendering as "Father") — that would
  // display, and on save persist, a fabricated relationship with no indication anything was
  // ever wrong. 'Other' is an honest "this doesn't match a known option" signal instead.
  const other = candidates.find((candidate) => candidate === 'Other');
  return other ?? fallback;
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

/**
 * Best-effort reverse of toAllergyType — splits on the first ": " and matches the category
 * against the known list. A stored value that doesn't parse into a known category (legacy
 * free text entered before the category dropdown existed, or anything else that doesn't
 * match `"<Category>: <text>"` exactly) falls back to the closed list's 'Others' bucket —
 * the same "honest catch-all" pattern dehumanize() uses for Relationship/PhoneRelation —
 * rather than '' (no category). Falling back to '' left hasKnownAllergy=true records with an
 * unparseable legacy value permanently failing allergyRefinement's "category is required"
 * check the moment the Edit form loaded, blocking save of *any* field until the user
 * rebuilt the allergy entry from scratch. 'Others' keeps the record valid on load while the
 * original text is preserved verbatim in `specify`.
 */
export function fromAllergyType(allergyType: string | null | undefined): { category: AllergyCategory | ''; specify: string } {
  if (!allergyType) {
    return { category: '', specify: '' };
  }
  const [prefix, ...rest] = allergyType.split(': ');
  const category = ALLERGY_CATEGORIES.find((c) => c.toLowerCase() === prefix.trim().toLowerCase());
  return category ? { category, specify: rest.join(': ') } : { category: 'Others', specify: allergyType };
}
