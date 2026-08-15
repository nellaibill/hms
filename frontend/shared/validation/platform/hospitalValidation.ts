import { z } from 'zod';

/**
 * Mirrors HMS.Modules.Platform.Application.Validators.CreateHospitalRequestValidator —
 * client-side convenience only, the backend remains authoritative
 * (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const tenDigitPhone = z
  .string()
  .trim()
  .min(1, 'Required')
  .length(10, 'Must be exactly 10 digits')
  .regex(/^\d+$/, 'Digits only');

export const createHospitalSchema = z.object({
  hospitalName: z.string().trim().min(1, 'Hospital name is required').max(200),
  hospitalCode: z
    .string()
    .trim()
    .min(1, 'Hospital code is required')
    .max(100)
    .regex(/^[a-zA-Z0-9._-]+$/, 'May contain only letters, numbers, dots, underscores and hyphens'),
  mobileNumber: tenDigitPhone,
  address: z.string().trim().min(1, 'Address is required').max(500),
  city: z.string().trim().min(1, 'City is required').max(100),
  state: z.string().trim().min(1, 'State is required').max(100),
  pincode: z.string().trim().min(1, 'Pincode is required').length(6, 'Must be exactly 6 digits').regex(/^\d+$/, 'Digits only'),

  superAdminUsername: z
    .string()
    .trim()
    .min(1, 'Username is required')
    .max(100)
    .regex(/^[a-zA-Z0-9._-]+$/, 'May contain only letters, numbers, dots, underscores and hyphens'),
  superAdminFirstName: z.string().trim().min(1, 'First name is required').max(100),
  superAdminLastName: z.string().trim().min(1, 'Last name is required').max(100),
  superAdminEmail: z.string().trim().min(1, 'Email is required').email('Not a valid email').max(256),
  superAdminPhoneNumber: tenDigitPhone,
  superAdminPassword: z.string().min(8, 'Must be at least 8 characters'),
});

export type CreateHospitalFormValues = z.infer<typeof createHospitalSchema>;
