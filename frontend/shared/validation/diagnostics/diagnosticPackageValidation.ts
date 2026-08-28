import { z } from 'zod';

/**
 * Mirrors HMS.Modules.Masters.Application.Validators.DiagnosticPackageValidators — Code
 * (create-only, natural key, immutable after creation), Name, and TotalPrice are required.
 * ServiceIds (at least one, required on create by CreateDiagnosticPackageRequestValidator) is
 * deliberately NOT part of this schema — the create page collects only the package's own
 * fields, matching the mockup's own flow where tests are added afterward on the package detail
 * page (see LabPackageDetailPage.tsx). Client-side convenience only, the backend remains
 * authoritative (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const diagnosticPackageCommonSchema = {
  name: z.string().trim().min(1, 'Name is required').max(200),
  description: z.string().trim().max(1000).optional().or(z.literal('')),
  totalPrice: z.coerce.number().min(0, 'Must be zero or greater'),
  isActive: z.boolean(),
};

export const createDiagnosticPackageSchema = z.object({
  code: z.string().trim().min(1, 'Code is required').max(30),
  ...diagnosticPackageCommonSchema,
});

export const updateDiagnosticPackageSchema = z.object({
  code: z.string().trim().min(1, 'Code is required').max(30),
  ...diagnosticPackageCommonSchema,
});

export type DiagnosticPackageFormValues = z.infer<typeof createDiagnosticPackageSchema>;
