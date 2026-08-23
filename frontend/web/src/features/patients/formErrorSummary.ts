import type { FieldErrors } from 'react-hook-form';

/**
 * Recursively collects every `.message` string found anywhere within a react-hook-form
 * FieldErrors (sub)tree. Needed because a tab's errors aren't all flat top-level fields —
 * e.g. arrivalSource.patientRelativeReferral.source, additionalEmergencyContacts.0.name, or
 * registration.departmentId are nested several levels deep, and a top-level field like
 * "registration" has no `.message` of its own to read directly.
 */
function collectMessages(node: unknown, messages: string[]): void {
  if (!node || typeof node !== 'object') {
    return;
  }
  const record = node as Record<string, unknown>;
  if (typeof record.message === 'string' && record.message) {
    messages.push(record.message);
  }
  for (const key of Object.keys(record)) {
    // 'ref' holds a DOM element (for leaf errors) — walking into it isn't useful and risks
    // recursing into unrelated object internals; 'type'/'types' are RHF's rule-name metadata,
    // not human-readable text.
    if (key === 'message' || key === 'type' || key === 'types' || key === 'ref') {
      continue;
    }
    collectMessages(record[key], messages);
  }
}

/**
 * Every validation error message for a tab's fields (a TAB_ERROR_FIELDS entry) — deduplicated,
 * in field order — so a "here's everything wrong on this tab" summary can sit next to the
 * existing per-field inline messages instead of making the user hunt for them one at a time.
 */
export function tabErrorMessages<T extends Record<string, unknown>>(errors: FieldErrors<T>, fields: readonly (keyof T)[]): string[] {
  const messages: string[] = [];
  for (const field of fields) {
    collectMessages((errors as Record<string, unknown>)[field as string], messages);
  }
  return [...new Set(messages)];
}
