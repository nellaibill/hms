import { z } from 'zod';

/**
 * Mirrors HMS.Modules.Masters.Application.Validators.DiagnosticProviderValidators — Code
 * (create-only, natural key, immutable after creation) and Name are required; ContactDetails
 * is optional. Client-side convenience only, the backend remains authoritative
 * (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const diagnosticProviderCommonSchema = {
  name: z.string().trim().min(1, 'Name is required').max(200),
  contactDetails: z.string().trim().max(500).optional().or(z.literal('')),
  isActive: z.boolean(),
};

export const createDiagnosticProviderSchema = z.object({
  code: z.string().trim().min(1, 'Code is required').max(30),
  ...diagnosticProviderCommonSchema,
});

export const updateDiagnosticProviderSchema = z.object(diagnosticProviderCommonSchema);

export type DiagnosticProviderFormValues = z.infer<typeof createDiagnosticProviderSchema>;
