namespace HMS.Modules.Pharmacy.Contracts;

public record CreateStoreRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public StoreType StoreType { get; init; } = StoreType.Pharmacy;

    /// <summary>Makes this the store used when a stock request names none, taking the flag
    /// from whichever store held it.</summary>
    public bool IsDefault { get; init; }
}

/// <summary>Code is immutable after creation, like every other coded record in this codebase.</summary>
public record UpdateStoreRequest
{
    public string Name { get; init; } = string.Empty;
    public StoreType StoreType { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>True moves the default to this store. False leaves the default where it is —
    /// the flag can only be moved to another store, never simply switched off.</summary>
    public bool IsDefault { get; init; }
}

public record StoreResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public StoreType StoreType { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
