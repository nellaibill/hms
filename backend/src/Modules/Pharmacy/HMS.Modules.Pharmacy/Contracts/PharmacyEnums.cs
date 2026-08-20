namespace HMS.Modules.Pharmacy.Contracts;

/// <summary>Entity-owned enum kept in Contracts (not Domain) because it's also referenced by
/// DTOs (StockTransactionResponse, StockLedgerListQuery) — mirrors HMS.Modules.IPD's
/// Contracts/IPDEnums.cs convention (e.g. BedStatus).</summary>
public enum TransactionType
{
    Receipt,
    Dispense,
}
