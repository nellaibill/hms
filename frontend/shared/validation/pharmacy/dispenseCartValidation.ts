import { z } from 'zod';

/**
 * Mirrors HMS.Modules.Pharmacy.Application.Validators.CreateDispenseCartRequestValidator
 * (client-side convenience only, the backend remains authoritative — docs/ApiStandards.md §7,
 * docs/FrontendArchitecture.md §9).
 */
const dispenseCartLineSchema = z.object({
  productId: z.string().trim().min(1, 'Product is required'),
  productBatchId: z.string().trim().min(1, 'Batch is required'),
  quantity: z.coerce.number().positive('Quantity must be greater than 0'),
  remarks: z.string().trim().max(500).optional().or(z.literal('')),
});

export const createDispenseCartSchema = z
  .object({
    patientId: z.string().trim().min(1, 'Patient is required'),
    admissionId: z.string().trim().optional().or(z.literal('')),
    lines: z.array(dispenseCartLineSchema).min(1, 'Add at least one item'),
  })
  .refine(
    (values) => {
      const pairs = values.lines.map((l) => `${l.productId}|${l.productBatchId}`);
      return new Set(pairs).size === pairs.length;
    },
    { message: 'The same product/batch appears more than once in the cart.', path: ['lines'] },
  );

export type DispenseCartLineFormValues = z.infer<typeof dispenseCartLineSchema>;
export type DispenseCartFormValues = z.infer<typeof createDispenseCartSchema>;
