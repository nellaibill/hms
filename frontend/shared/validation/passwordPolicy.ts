/**
 * Mirrors HMS.Shared.Kernel.PasswordPolicy — the one password-strength rule shared by every
 * credential-setting form (hospital Super Admin creation, admin password reset, self-service
 * change password). Client-side convenience only, the backend remains authoritative
 * (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
export const PASSWORD_MIN_LENGTH = 10;

export const PASSWORD_COMPLEXITY_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$/;

export const PASSWORD_COMPLEXITY_MESSAGE =
  'Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.';
