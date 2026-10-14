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

export const PURCHASE_ORDER_STATUSES = ['Draft', 'Ordered', 'PartlyReceived', 'Received', 'Cancelled'] as const;
export type PurchaseOrderStatus = (typeof PURCHASE_ORDER_STATUSES)[number];

export const PURCHASE_RETURN_REASONS = ['Expired', 'NearExpiry', 'Damaged', 'Excess', 'WrongItem', 'RateDifference', 'Other'] as const;
export type PurchaseReturnReason = (typeof PURCHASE_RETURN_REASONS)[number];

export const SUPPLIER_PAYMENT_METHODS = ['Cash', 'Cheque', 'BankTransfer', 'Upi'] as const;
export type SupplierPaymentMethod = (typeof SUPPLIER_PAYMENT_METHODS)[number];

export const SUPPLIER_ENTRY_TYPES = ['Purchase', 'Return', 'Payment'] as const;
export type SupplierEntryType = (typeof SUPPLIER_ENTRY_TYPES)[number];

export const INDENT_STATUSES = ['Pending', 'PartlyIssued', 'Issued', 'Cancelled'] as const;
export type IndentStatus = (typeof INDENT_STATUSES)[number];

export const ADJUSTMENT_REASONS = ['Expired', 'Damaged', 'Shortage', 'Excess', 'FreeIssue', 'StockCount', 'Correction', 'Other'] as const;
export type AdjustmentReason = (typeof ADJUSTMENT_REASONS)[number];

export const STOCK_COUNT_STATUSES = ['Open', 'Posted', 'Cancelled'] as const;
export type StockCountStatus = (typeof STOCK_COUNT_STATUSES)[number];
