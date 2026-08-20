/** Mirrors HMS.Modules.Pharmacy.Contracts.PharmacyEnums — serialized as strings (JsonStringEnumConverter). */
export const TRANSACTION_TYPES = ['Receipt', 'Dispense'] as const;
export type TransactionType = (typeof TRANSACTION_TYPES)[number];
