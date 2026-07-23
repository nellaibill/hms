import { z } from 'zod';

/**
 * Mirrors HMS.Modules.Identity.Application.Validators.CreateUserRequestValidator /
 * UpdateUserRequestValidator — client-side convenience only, the backend remains
 * authoritative (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const phonePattern = /^[0-9+\-() ]*$/;

export const userProfileSchema = z.object({
  firstName: z.string().trim().min(1, 'First name is required').max(100),
  lastName: z.string().trim().min(1, 'Last name is required').max(100),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address').max(256),
  phoneNumber: z
    .string()
    .max(30)
    .regex(phonePattern, 'Enter a valid phone number')
    .optional()
    .or(z.literal('')),
});

export const createUserSchema = userProfileSchema;
export const updateUserSchema = userProfileSchema;

export type UserProfileFormValues = z.infer<typeof userProfileSchema>;
