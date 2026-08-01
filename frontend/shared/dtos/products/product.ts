/** Mirrors HMS.Modules.Products.Contracts.ProductResponse. */
export interface Product {
  id: string;
  sku: string;
  productCode: string;
  productName: string;
  genericName?: string | null;
  description?: string | null;
  brandId: string;
  manufacturerId: string;
  categoryId: string;
  subCategoryId: string;
  groupId: string;
  uomId: string;
  baseUomId: string;
  isBatchTracked: boolean;
  isSerialized: boolean;
  isActive: boolean;
  reorderLevel: number;
  minStockLevel: number;
  maxStockLevel: number;
  mrp: number;
  costPrice: number;
  sellingPrice: number;
  hsnCode?: string | null;
  weight?: number | null;
  volume?: number | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Products.Contracts.CreateProductRequest. Sku/ProductCode are immutable after creation. */
export interface CreateProductRequest {
  sku: string;
  productCode: string;
  productName: string;
  genericName?: string | null;
  description?: string | null;
  brandId: string;
  manufacturerId: string;
  categoryId: string;
  subCategoryId: string;
  groupId: string;
  uomId: string;
  baseUomId: string;
  isBatchTracked: boolean;
  isSerialized: boolean;
  isActive: boolean;
  reorderLevel: number;
  minStockLevel: number;
  maxStockLevel: number;
  mrp: number;
  costPrice: number;
  sellingPrice: number;
  hsnCode?: string | null;
  weight?: number | null;
  volume?: number | null;
}

/** Mirrors HMS.Modules.Products.Contracts.UpdateProductRequest — no Sku/ProductCode (immutable). */
export interface UpdateProductRequest {
  productName: string;
  genericName?: string | null;
  description?: string | null;
  brandId: string;
  manufacturerId: string;
  categoryId: string;
  subCategoryId: string;
  groupId: string;
  uomId: string;
  baseUomId: string;
  isBatchTracked: boolean;
  isSerialized: boolean;
  isActive: boolean;
  reorderLevel: number;
  minStockLevel: number;
  maxStockLevel: number;
  mrp: number;
  costPrice: number;
  sellingPrice: number;
  hsnCode?: string | null;
  weight?: number | null;
  volume?: number | null;
}

/** Mirrors HMS.Modules.Products.Contracts.ProductListQuery. */
export interface ProductListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
  categoryId?: string;
  brandId?: string;
}
