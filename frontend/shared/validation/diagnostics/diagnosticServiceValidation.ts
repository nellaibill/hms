import { z } from 'zod';
import { DIAGNOSTIC_SERVICE_TYPES } from '../../dtos/diagnostics/diagnosticService';

/**
 * Mirrors HMS.Modules.Masters.Application.Validators.DiagnosticServiceValidators — Code
 * (create-only, natural key, immutable after creation), Name, Category, and Service Type are
 * required; Provider is required only when Outsourced is checked (superRefine below), same
 * rule the backend validator enforces. Client-side convenience only, the backend remains
 * authoritative (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
/** One Radiology rate option row on the service form (ADR-140). `id` is kept for an existing
 * option so the backend updates it in place. */
export const diagnosticServiceRateOptionSchema = z.object({
  id: z.string().optional(),
  name: z.string().trim().min(1, 'Name is required').max(40, 'Up to 40 characters'),
  price: z.coerce.number().min(0, 'Must be zero or greater'),
  doctorCharge: z.coerce.number().min(0, 'Must be zero or greater'),
  costPrice: z.coerce.number().min(0, 'Must be zero or greater'),
});

const diagnosticServiceCommonSchema = {
  name: z.string().trim().min(1, 'Name is required').max(200),
  categoryId: z.string().trim().min(1, 'Category is required'),
  serviceType: z.enum(DIAGNOSTIC_SERVICE_TYPES, { errorMap: () => ({ message: 'Service type is required' }) }),
  isOutsourced: z.boolean(),
  providerId: z.string().trim().optional().or(z.literal('')),
  price: z.coerce.number().min(0, 'Must be zero or greater'),
  costPrice: z.coerce.number().min(0, 'Must be zero or greater'),
  isActive: z.boolean(),
  // ADR-140 — Radiology only. A lab test's form never shows these; its page sends 0, no bedside
  // add-on and no rate options whatever they hold.
  doctorCharge: z.coerce.number().min(0, 'Must be zero or greater').default(0),
  bedsideEnabled: z.boolean().default(false),
  bedsidePrice: z.coerce.number().min(0, 'Must be zero or greater').default(0),
  bedsideDoctorCharge: z.coerce.number().min(0, 'Must be zero or greater').default(0),
  rateOptions: z.array(diagnosticServiceRateOptionSchema).max(10, 'At most 10 rate options').default([]),
};

function providerRequiredWhenOutsourced(
  data: { isOutsourced: boolean; providerId?: string; serviceType: string; rateOptions: { name: string }[] },
  ctx: z.RefinementCtx,
) {
  if (data.isOutsourced && !data.providerId) {
    ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['providerId'], message: 'Provider is required for an outsourced service' });
  }
  // Mirrors DiagnosticServiceTariffRules: each rate option needs a different name.
  if (data.serviceType === 'Radiology') {
    const seen = new Set<string>();
    data.rateOptions.forEach((option, index) => {
      const key = option.name.trim().toLowerCase();
      if (key && seen.has(key)) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['rateOptions', index, 'name'], message: 'Each rate option needs a different name' });
      }
      seen.add(key);
    });
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
export type DiagnosticServiceRateOptionFormValues = z.infer<typeof diagnosticServiceRateOptionSchema>;
