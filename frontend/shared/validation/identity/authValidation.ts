import { z } from 'zod';
import { PASSWORD_COMPLEXITY_MESSAGE, PASSWORD_COMPLEXITY_PATTERN, PASSWORD_MIN_LENGTH } from '../passwordPolicy';

/** Mirrors HMS.Modules.Identity.Application.Validators.ChangePasswordRequestValidator, plus a client-only confirm-password check. */
export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required'),
    newPassword: z
      .string()
      .min(PASSWORD_MIN_LENGTH, `New password must be at least ${PASSWORD_MIN_LENGTH} characters`)
      .regex(PASSWORD_COMPLEXITY_PATTERN, PASSWORD_COMPLEXITY_MESSAGE),
    confirmNewPassword: z.string().min(1, 'Confirm the new password'),
  })
  .refine((data) => data.newPassword === data.confirmNewPassword, {
    message: 'Passwords do not match',
    path: ['confirmNewPassword'],
  })
  .refine((data) => data.newPassword !== data.currentPassword, {
    message: 'New password must be different from the current password',
    path: ['newPassword'],
  });

export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;
