/** Mirrors HMS.Modules.Pharmacy.Contracts.PharmacyEnums — serialized as strings (JsonStringEnumConverter). */
export const TRANSACTION_TYPES = ['Receipt', 'Dispense'] as const;
export type TransactionType = (typeof TRANSACTION_TYPES)[number];

export const STOCK_STATUSES = ['InStock', 'LowStock', 'OutOfStock', 'NotStocked'] as const;
export type StockStatus = (typeof STOCK_STATUSES)[number];

export const STOCK_SUMMARY_FILTERS = ['NeedsReorder', 'ExpiringSoon', 'Expired'] as const;
export type StockSummaryFilter = (typeof STOCK_SUMMARY_FILTERS)[number];
