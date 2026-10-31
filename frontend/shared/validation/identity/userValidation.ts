import { z } from 'zod';
import { PASSWORD_COMPLEXITY_MESSAGE, PASSWORD_COMPLEXITY_PATTERN, PASSWORD_MIN_LENGTH } from '../passwordPolicy';

/**
 * Mirrors HMS.Modules.Identity.Application.Validators.CreateUserRequestValidator /
 * UpdateUserRequestValidator — client-side convenience only, the backend remains
 * authoritative (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const phoneDigitsPattern = /^\d+$/;
const usernamePattern = /^[a-zA-Z0-9._-]+$/;

export const userProfileSchema = z.object({
  username: z.string().trim().min(1, 'Username is required').max(100).regex(usernamePattern, 'Username may contain only letters, numbers, dots, underscores and hyphens'),
  firstName: z.string().trim().min(1, 'First name is required').max(100),
  lastName: z.string().trim().min(1, 'Last name is required').max(100),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address').max(256),
  phoneNumber: z
    .string()
    .min(1, 'Phone number is required')
    .length(10, 'Phone number must be exactly 10 digits')
    .regex(phoneDigitsPattern, 'Phone number can contain digits only'),
  roleId: z.string().trim().min(1, 'Role is required'),
});

export const createUserSchema = userProfileSchema;
export const updateUserSchema = userProfileSchema;

export type UserProfileFormValues = z.infer<typeof userProfileSchema>;

/** Mirrors HMS.Modules.Identity.Application.Validators.SetPasswordRequestValidator, plus a client-only confirm-password check. */
export const setPasswordSchema = z
  .object({
    password: z
      .string()
      .min(PASSWORD_MIN_LENGTH, `Password must be at least ${PASSWORD_MIN_LENGTH} characters`)
      .regex(PASSWORD_COMPLEXITY_PATTERN, PASSWORD_COMPLEXITY_MESSAGE),
    confirmPassword: z.string().min(1, 'Confirm the password'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

export type SetPasswordFormValues = z.infer<typeof setPasswordSchema>;

/**
 * The Login access card on the Employee/Consultant forms (ADR-108) — only what a login has
 * that the person's own record doesn't. Mirrors HMS.Modules.HR's EmployeeLoginRequestValidator
 * plus Identity's password policy, with the same client-only confirm-password check as
 * setPasswordSchema.
 */
export const newLoginSchema = z
  .object({
    username: userProfileSchema.shape.username,
    roleId: userProfileSchema.shape.roleId,
    password: setPasswordSchema.innerType().shape.password,
    confirmPassword: z.string().min(1, 'Confirm the password'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

export type NewLoginFormValues = z.infer<typeof newLoginSchema>;

/** A consultant record has no email or phone of its own, so its Login access card asks for
 * both — with the same rules as any other login. */
export const consultantNewLoginSchema = z
  .object({
    username: userProfileSchema.shape.username,
    roleId: userProfileSchema.shape.roleId,
    email: userProfileSchema.shape.email,
    phoneNumber: userProfileSchema.shape.phoneNumber,
    password: setPasswordSchema.innerType().shape.password,
    confirmPassword: z.string().min(1, 'Confirm the password'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

export type ConsultantNewLoginFormValues = z.infer<typeof consultantNewLoginSchema>;

/**
 * Mirrors HMS.Modules.HR.Application.MobileNumber.ToTenDigits — the employee's free-text
 * phone as the bare 10-digit number a login needs (spaces, dashes and a +91/0 prefix
 * dropped), or null when that isn't possible.
 */
export function toTenDigitMobile(phone: string | null | undefined): string | null {
  let digits = (phone ?? '').replace(/\D/g, '');
  if (digits.length === 12 && digits.startsWith('91')) {
    digits = digits.slice(2);
  } else if (digits.length === 11 && digits.startsWith('0')) {
    digits = digits.slice(1);
  }
  return digits.length === 10 ? digits : null;
}
