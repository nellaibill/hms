import { z } from 'zod';
import { DIAGNOSTIC_SERVICE_TYPES } from '../../dtos/diagnostics/diagnosticService';

/**
 * Mirrors HMS.Modules.Masters.Application.Validators.DiagnosticServiceValidators — Code
 * (create-only, natural key, immutable after creation), Name, Category, and Service Type are
 * required; Provider is required only when Outsourced is checked (superRefine below), same
 * rule the backend validator enforces. Client-side convenience only, the backend remains
 * authoritative (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const diagnosticServiceCommonSchema = {
  name: z.string().trim().min(1, 'Name is required').max(200),
  categoryId: z.string().trim().min(1, 'Category is required'),
  serviceType: z.enum(DIAGNOSTIC_SERVICE_TYPES, { errorMap: () => ({ message: 'Service type is required' }) }),
  isOutsourced: z.boolean(),
  providerId: z.string().trim().optional().or(z.literal('')),
  price: z.coerce.number().min(0, 'Must be zero or greater'),
  isActive: z.boolean(),
};

function providerRequiredWhenOutsourced(data: { isOutsourced: boolean; providerId?: string }, ctx: z.RefinementCtx) {
  if (data.isOutsourced && !data.providerId) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['providerId'], message: 'Provider is required for an outsourced service' });
  }
}

export const createDiagnosticServiceSchema = z
  .object({
    code: z.string().trim().min(1, 'Code is required').max(30),
    ...diagnosticServiceCommonSchema,
  })
  .superRefine(providerRequiredWhenOutsourced);

export const updateDiagnosticServiceSchema = z
  .object({
    code: z.string().trim().min(1, 'Code is required').max(30),
    ...diagnosticServiceCommonSchema,
  })
  .superRefine(providerRequiredWhenOutsourced);

export type DiagnosticServiceFormValues = z.infer<typeof createDiagnosticServiceSchema>;
