import { z } from 'zod';

/**
 * Mirrors HMS.Modules.Pharmacy.Application.Validators.CreateStockReceiptRequestValidator
 * exactly (client-side convenience only, the backend remains authoritative —
 * docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
export const createStockReceiptSchema = z.object({
  productId: z.string().trim().min(1, 'Product is required'),
  productBatchId: z.string().trim().min(1, 'Batch is required'),
  quantity: z.coerce.number().positive('Quantity must be greater than 0'),
  remarks: z.string().trim().max(500).optional().or(z.literal('')),
});

export type StockReceiptFormValues = z.infer<typeof createStockReceiptSchema>;
