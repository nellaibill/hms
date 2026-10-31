import {
  ApiError,
  consultantNewLoginSchema,
  newLoginSchema,
  type EmployeeLoginRequest,
} from '@hms/shared';

/**
 * The Login access card on the Employee and Consultant forms (ADR-108): a login is made in
 * the same place as the person's own record, so nobody has to go to Settings → Users and
 * re-type a name, email and phone that are already there.
 */
export type LoginAccessSubject = 'employee' | 'consultant';

/** Everything the card holds until the record itself is saved. */
export interface LoginAccessDraft {
  enabled: boolean;
  username: string;
  roleId: string;
  password: string;
  confirmPassword: string;
  /** Consultant only — an employee's login uses the employee's own email and phone. */
  email: string;
  phoneNumber: string;
}

export type LoginAccessErrors = Partial<Record<keyof LoginAccessDraft, string>>;

export function emptyLoginDraft(enabled = false): LoginAccessDraft {
  return {
    enabled,
    username: '',
    roleId: '',
    password: '',
    confirmPassword: '',
    email: '',
    phoneNumber: '',
  };
}

/** Empty when the draft can be saved — including when login access is simply off. */
export function validateLoginDraft(
  draft: LoginAccessDraft,
  subject: LoginAccessSubject,
): LoginAccessErrors {
  if (!draft.enabled) {
    return {};
  }

  const result = (subject === 'consultant' ? consultantNewLoginSchema : newLoginSchema).safeParse(
    draft,
  );
  if (result.success) {
    return {};
  }

  const errors: LoginAccessErrors = {};
  for (const issue of result.error.issues) {
    const key = issue.path[0] as keyof LoginAccessDraft;
    errors[key] ??= issue.message;
  }
  return errors;
}

export function hasLoginErrors(errors: LoginAccessErrors): boolean {
  return Object.values(errors).some(Boolean);
}

/** "Priya R" → "priya.r", "Dr. Meena Sundaram" → "meena.s" — only ever a suggestion the user can change. */
export function suggestUsername(fullName: string): string {
  const tokens = fullName
    .toLowerCase()
    .split(/\s+/)
    .map((token) => token.replace(/[^a-z0-9]/g, ''))
    .filter((token) => token && token !== 'dr');
  if (tokens.length === 0) {
    return '';
  }
  return tokens.length === 1 ? tokens[0] : `${tokens[0]}.${tokens[tokens.length - 1][0]}`;
}

/**
 * A consultant has one Name field but a login needs a first and last name. A one-word name
 * becomes "Dr." + name, so the Users page still reads naturally ("Dr. Meena").
 */
export function splitPersonName(fullName: string): { firstName: string; lastName: string } {
  const tokens = fullName.trim().split(/\s+/).filter(Boolean);
  if (tokens.length <= 1) {
    return { firstName: 'Dr.', lastName: tokens[0] ?? '' };
  }
  return { firstName: tokens.slice(0, -1).join(' '), lastName: tokens[tokens.length - 1] };
}

/** The `login` an Employee create/update request carries for a draft — null when login access is off. */
export function toEmployeeLoginRequest(draft: LoginAccessDraft): EmployeeLoginRequest | null {
  return draft.enabled
    ? { username: draft.username.trim(), password: draft.password, roleId: draft.roleId }
    : null;
}

/**
 * Server failures that belong to one of the card's own fields, so the form shows them there
 * instead of as a general banner. HR passes Identity's error codes straight through.
 */
export function loginErrorsFromApiError(error: ApiError | null | undefined): LoginAccessErrors {
  if (!error) {
    return {};
  }

  const errors: LoginAccessErrors = {};
  for (const issue of error.validationErrors ?? []) {
    if (issue.field.startsWith('Login.')) {
      const field = issue.field.slice('Login.'.length);
      errors[(field.charAt(0).toLowerCase() + field.slice(1)) as keyof LoginAccessDraft] =
        issue.message;
    }
  }

  switch (error.errorCode) {
    case 'IDENTITY.USER_USERNAME_DUPLICATE':
      errors.username = 'This username is already taken.';
      break;
    case 'IDENTITY.USER_EMAIL_DUPLICATE':
      errors.email = 'Another login already uses this email.';
      break;
    case 'IDENTITY.USER_INVALID_ROLE':
      errors.roleId = 'Choose a role.';
      break;
  }
  return errors;
}
