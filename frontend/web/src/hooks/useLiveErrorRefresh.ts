import { useEffect, useRef } from 'react';
import type { FieldErrors, FieldValues, Path, UseFormClearErrors, UseFormSetError, UseFormWatch } from 'react-hook-form';
import type { ZodTypeAny } from 'zod';

interface FlatFieldError {
  path: string;
  type: string;
  message: string;
}

/** Walks react-hook-form's nested errors object down to its leaf FieldErrors, keyed by the same
 * dotted path a Zod issue produces (e.g. "primaryPhone.number", "additionalEmergencyContacts.0.phone"). */
function flattenFieldErrors(node: unknown, prefix: string, out: FlatFieldError[]): FlatFieldError[] {
  if (!node || typeof node !== 'object') return out;
  const record = node as Record<string, unknown>;
  if (typeof record.type === 'string') {
    // A field array's own (array-level) error sits under ".root" in RHF but has no suffix in Zod.
    out.push({ path: prefix.endsWith('.root') ? prefix.slice(0, -'.root'.length) : prefix, type: record.type, message: String(record.message ?? '') });
    return out;
  }
  for (const [key, value] of Object.entries(record)) {
    if (key === 'ref') continue;
    flattenFieldErrors(value, prefix ? `${prefix}.${key}` : key, out);
  }
  return out;
}

interface LiveErrorRefreshOptions<T extends FieldValues> {
  schema: ZodTypeAny;
  watch: UseFormWatch<T>;
  errors: FieldErrors<T>;
  setError: UseFormSetError<T>;
  clearErrors: UseFormClearErrors<T>;
}

/**
 * Keeps already-visible validation errors in step with what the user types, for forms that
 * validate step by step with trigger() instead of handleSubmit.
 *
 * react-hook-form only re-validates on change once a form has been *submitted* (its
 * reValidateMode applies after submit — see skipValidation in react-hook-form's source), and
 * trigger() never marks a form submitted. So in the patient wizards and the billing step, an error
 * shown by Next/Collect Payment stayed on screen after the field was fixed ("Date of birth cannot
 * be in the future" next to a valid date) until the next trigger().
 *
 * On every change this re-checks the form's own schema synchronously and only touches fields that
 * already show an error: a fixed field is cleared, and a still-wrong one gets the current message
 * (so fixing the date of birth also clears the Title-vs-age error on Title). It never adds an
 * error to a field that doesn't have one — errors still first appear when the user tries to move
 * on — and, being synchronous and clear/update-only, it can't race those async trigger() passes
 * the way `mode: 'onChange'` did. Server errors are left alone unless their own field changed.
 */
export function useLiveErrorRefresh<T extends FieldValues>({ schema, watch, errors, setError, clearErrors }: LiveErrorRefreshOptions<T>) {
  const errorsRef = useRef(errors);
  errorsRef.current = errors;

  useEffect(() => {
    const subscription = watch((values, { name }) => {
      const current = flattenFieldErrors(errorsRef.current, '', []);
      if (current.length === 0) return;

      const result = schema.safeParse(values);
      // First issue per path — the same one zodResolver reports for that field.
      const issueByPath = new Map<string, string>();
      if (!result.success) {
        for (const issue of result.error.issues) {
          const path = issue.path.join('.');
          if (!issueByPath.has(path)) issueByPath.set(path, issue.message);
        }
      }

      for (const error of current) {
        const changedThisField = Boolean(name) && (error.path === name || error.path.startsWith(`${name}.`) || name!.startsWith(`${error.path}.`));
        if (error.type === 'server' && !changedThisField) continue;

        const message = issueByPath.get(error.path);
        if (message === undefined) {
          clearErrors(error.path as Path<T>);
        } else if (message !== error.message) {
          setError(error.path as Path<T>, { type: 'manual', message });
        }
      }
    });
    return () => subscription.unsubscribe();
  }, [schema, watch, setError, clearErrors]);
}
