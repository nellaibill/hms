namespace HMS.Modules.Pharmacy.Contracts;

/// <summary>Entity-owned enum kept in Contracts (not Domain) because it's also referenced by
/// DTOs (StockTransactionResponse, StockLedgerListQuery) — mirrors HMS.Modules.IPD's
/// Contracts/IPDEnums.cs convention (e.g. BedStatus).</summary>
public enum TransactionType
{
    Receipt,
    Dispense,

    /// <summary>Stock already on the shelf when the system went live — entered once, not
    /// bought through it.</summary>
    OpeningStock,
}

/// <summary>What kind of place a pharmacy store is.</summary>
public enum StoreType
{
    /// <summary>Bulk store that receives purchases and supplies the others.</summary>
    MainStore,

    /// <summary>A dispensing counter — OP pharmacy, IP pharmacy.</summary>
    Pharmacy,

    /// <summary>Stock held at the point of use — a ward, a theatre, ICU.</summary>
    SubStore,
}

/// <summary>A product's stock position, judged on what can actually be dispensed (see
/// StockSummaryResponse.AvailableQuantity) rather than everything physically on the shelf.</summary>
public enum StockStatus
{
    InStock,

    /// <summary>Some available stock, but at or below the product's reorder level.</summary>
    LowStock,

    /// <summary>Nothing available for a product the pharmacy does stock — it has been
    /// received before, or has a reorder level set.</summary>
    OutOfStock,

    /// <summary>Nothing available for a catalog product that has never been received and has
    /// no reorder level — kept apart from OutOfStock so a reorder list isn't buried under
    /// every catalog item the pharmacy simply doesn't carry.</summary>
    NotStocked,
}

/// <summary>The "needs attention" views of the stock summary.</summary>
public enum StockSummaryFilter
{
    /// <summary>LowStock or OutOfStock.</summary>
    NeedsReorder,

    /// <summary>Has available stock that expires within the expiring-soon window.</summary>
    ExpiringSoon,

    /// <summary>Has expired stock still on hand.</summary>
    Expired,
}
