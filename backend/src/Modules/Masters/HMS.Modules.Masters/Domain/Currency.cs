using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// Currency master used for pricing and payments (docs/03_Masters_ERD, "Finance &amp; Payment").
/// </summary>
internal class Currency : Entity
{
    public string CurrencyCode { get; private set; } = null!;
    public string CurrencyName { get; private set; } = null!;
    public string Symbol { get; private set; } = null!;
    public int DecimalPlaces { get; private set; }

    /// <summary>Enable/disable without a hard delete (docs/03_Masters_ERD notes) — distinct from Entity's own soft-delete flag.</summary>
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private Currency()
    {
    }

    private Currency(Guid id, string currencyCode, string currencyName, string symbol, int decimalPlaces, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        CurrencyCode = currencyCode;
        CurrencyName = currencyName;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
        IsActive = isActive;
    }

    public static Currency Create(string currencyCode, string currencyName, string symbol, int decimalPlaces, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(currencyCode, nameof(currencyCode));
        Guard.AgainstNullOrWhiteSpace(currencyName, nameof(currencyName));
        Guard.AgainstNullOrWhiteSpace(symbol, nameof(symbol));
        if (decimalPlaces < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places cannot be negative.");
        }

        return new Currency(Guid.CreateVersion7(), currencyCode.Trim().ToUpperInvariant(), currencyName.Trim(), symbol.Trim(), decimalPlaces, isActive, createdBy);
    }

    public void Update(string currencyName, string symbol, int decimalPlaces, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(currencyName, nameof(currencyName));
        Guard.AgainstNullOrWhiteSpace(symbol, nameof(symbol));
        if (decimalPlaces < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "Decimal places cannot be negative.");
        }

        CurrencyName = currencyName.Trim();
        Symbol = symbol.Trim();
        DecimalPlaces = decimalPlaces;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
