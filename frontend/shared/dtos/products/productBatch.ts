/** Mirrors HMS.Modules.Products.Contracts.CreateProductBatchRequest. */
export interface CreateProductBatchRequest {
  batchNo: string;
  manufactureDate: string;
  expiryDate: string;
  isActive: boolean;
  /** Per pack. */
  mrp?: number | null;
  purchaseRate?: number | null;
  sellingRate?: number | null;
}

/** Mirrors HMS.Modules.Products.Contracts.UpdateProductBatchRequest. */
export interface UpdateProductBatchRequest {
  manufactureDate: string;
  expiryDate: string;
  isActive: boolean;
  /** Per pack. */
  mrp?: number | null;
  purchaseRate?: number | null;
  sellingRate?: number | null;
}

/** Mirrors HMS.Modules.Products.Contracts.ProductBatchResponse. */
export interface ProductBatch {
  id: string;
  productId: string;
  batchNo: string;
  manufactureDate: string;
  expiryDate: string;
  isActive: boolean;
  /** Per pack. purchaseRate is null for a caller without pharmacy.view-cost. */
  mrp?: number | null;
  purchaseRate?: number | null;
  sellingRate?: number | null;
  createdAt: string;
  updatedAt?: string | null;
}

/** Mirrors HMS.Modules.Products.Contracts.ProductBatchListQuery. */
export interface ProductBatchListQuery {
  page?: number;
  pageSize?: number;
  sort?: string;
  search?: string;
  isActive?: boolean;
}
