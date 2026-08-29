import { z } from 'zod';

/**
 * Mirrors HMS.Modules.Masters.Application.Validators.DiagnosticCategoryValidators — Code
 * (create-only, natural key, immutable after creation) and Name are required; Description is
 * optional. Client-side convenience only, the backend remains authoritative
 * (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const diagnosticCategoryCommonSchema = {
  name: z.string().trim().min(1, 'Name is required').max(200),
  description: z.string().trim().max(1000).optional().or(z.literal('')),
  isActive: z.boolean(),
};

export const createDiagnosticCategorySchema = z.object({
  code: z.string().trim().min(1, 'Code is required').max(30),
  ...diagnosticCategoryCommonSchema,
});

export const updateDiagnosticCategorySchema = z.object(diagnosticCategoryCommonSchema);

export type DiagnosticCategoryFormValues = z.infer<typeof createDiagnosticCategorySchema>;
