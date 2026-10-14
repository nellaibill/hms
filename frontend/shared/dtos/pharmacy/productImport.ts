import type { ProductImportRowStatus } from '../../enums';

/** Mirrors HMS.Modules.Pharmacy.Contracts.ImportProductRow — one spreadsheet row. Only the name is required. */
export interface ImportProductRow {
  productName: string;
  genericName?: string | null;
  sku?: string | null;
  category?: string | null;
  manufacturer?: string | null;
  brand?: string | null;
  unit?: string | null;
  hsnCode?: string | null;
  gstRatePercent?: number | null;
  unitsPerPack?: number | null;
  dosageForm?: string | null;
  strength?: string | null;
  schedule?: string | null;
  rackLocation?: string | null;
  reorderLevel?: number | null;
  mrp?: number | null;
  purchaseRate?: number | null;
  sellingRate?: number | null;
  batchNo?: string | null;
  /** ISO date (yyyy-MM-dd). */
  expiryDate?: string | null;
  /** Units on the shelf (tablets, not strips). */
  openingQuantity?: number | null;
}

export interface ImportProductsRequest {
  storeId?: string;
  validateOnly: boolean;
  /** Spreadsheet row number of the first row in this request. */
  firstRowNumber: number;
  rows: ImportProductRow[];
}

export interface ProductImportRowResult {
  rowNumber: number;
  productName: string;
  status: ProductImportRowStatus;
  stockAdded: number;
  message?: string | null;
}

export interface ImportProductsResponse {
  validateOnly: boolean;
  productsCreated: number;
  productsMatched: number;
  batchesStocked: number;
  errors: number;
  rows: ProductImportRowResult[];
}
