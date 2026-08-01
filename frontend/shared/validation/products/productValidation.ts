import { z } from 'zod';

/**
 * Mirrors HMS.Modules.Products.Application.Validators.CreateProductRequestValidator /
 * UpdateProductRequestValidator — client-side convenience only, the backend remains
 * authoritative (docs/ApiStandards.md §7, docs/FrontendArchitecture.md §9).
 */
const nonNegativeDecimal = z.coerce.number().min(0, 'Must be zero or greater');

/** Empty-string or NaN (from a number input's valueAsNumber on a blank field) both mean "not entered" for an optional numeric field. */
const optionalNonNegativeDecimal = z.preprocess(
  (value) => (value === '' || value === undefined || (typeof value === 'number' && Number.isNaN(value)) ? undefined : value),
  z.coerce.number().min(0, 'Must be zero or greater').optional(),
);

export const productProfileSchema = z.object({
  sku: z.string().trim().min(1, 'SKU is required').max(100),
  productCode: z.string().trim().min(1, 'Product code is required').max(100),
  productName: z.string().trim().min(1, 'Product name is required').max(200),
  genericName: z.string().trim().max(200).optional().or(z.literal('')),
  description: z.string().trim().max(2000).optional().or(z.literal('')),
  brandId: z.string().trim().min(1, 'Brand is required'),
  manufacturerId: z.string().trim().min(1, 'Manufacturer is required'),
  categoryId: z.string().trim().min(1, 'Category is required'),
  subCategoryId: z.string().trim().min(1, 'Sub-category is required'),
  groupId: z.string().trim().min(1, 'Group is required'),
  uomId: z.string().trim().min(1, 'Unit of measure is required'),
  baseUomId: z.string().trim().min(1, 'Base unit of measure is required'),
  isBatchTracked: z.boolean(),
  isSerialized: z.boolean(),
  isActive: z.boolean(),
  reorderLevel: nonNegativeDecimal,
  minStockLevel: nonNegativeDecimal,
  maxStockLevel: nonNegativeDecimal,
  mrp: nonNegativeDecimal,
  costPrice: nonNegativeDecimal,
  sellingPrice: nonNegativeDecimal,
  hsnCode: z.string().trim().max(20).optional().or(z.literal('')),
  weight: optionalNonNegativeDecimal,
  volume: optionalNonNegativeDecimal,
});

export const createProductSchema = productProfileSchema;
export const updateProductSchema = productProfileSchema.omit({ sku: true, productCode: true });

export type ProductProfileFormValues = z.infer<typeof productProfileSchema>;
export type UpdateProductFormValues = z.infer<typeof updateProductSchema>;
