/** Mirrors HMS.Modules.Pharmacy.Contracts.PharmacyEnums — serialized as strings (JsonStringEnumConverter). */
export const TRANSACTION_TYPES = [
  'Receipt',
  'Dispense',
  'OpeningStock',
  'PurchaseReturn',
  'Sale',
  'SaleReturn',
  'TransferOut',
  'TransferIn',
  'AdjustmentIn',
  'AdjustmentOut',
] as const;
export type TransactionType = (typeof TRANSACTION_TYPES)[number];

export const STOCK_STATUSES = ['InStock', 'LowStock', 'OutOfStock', 'NotStocked'] as const;
export type StockStatus = (typeof STOCK_STATUSES)[number];

export const STOCK_SUMMARY_FILTERS = ['NeedsReorder', 'ExpiringSoon', 'Expired'] as const;
export type StockSummaryFilter = (typeof STOCK_SUMMARY_FILTERS)[number];

export const STORE_TYPES = ['MainStore', 'Pharmacy', 'SubStore'] as const;
export type StoreType = (typeof STORE_TYPES)[number];

/** The kind of document a stock movement was posted from. */
export const STOCK_SOURCE_TYPES = ['GoodsReceipt', 'PurchaseReturn', 'Sale', 'SaleReturn', 'Transfer', 'Adjustment', 'StockCount', 'Import'] as const;
export type StockSourceType = (typeof STOCK_SOURCE_TYPES)[number];

export const PRODUCT_IMPORT_ROW_STATUSES = ['Created', 'Matched', 'Error'] as const;
export type ProductImportRowStatus = (typeof PRODUCT_IMPORT_ROW_STATUSES)[number];

/** How money changed hands at the pharmacy counter. */
export const SALE_PAYMENT_METHODS = ['Cash', 'Card', 'Upi', 'BankTransfer'] as const;
export type SalePaymentMethod = (typeof SALE_PAYMENT_METHODS)[number];

/** The paper a pharmacy bill is printed on. */
export const BILL_PRINT_FORMATS = ['A5', 'A4', 'Thermal80'] as const;
export type BillPrintFormat = (typeof BILL_PRINT_FORMATS)[number];
