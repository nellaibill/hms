namespace HMS.Modules.Products.Contracts;

/// <summary>The physical form a drug is supplied in. Null on a product means it isn't a
/// drug at all (a consumable, a device). Stored and serialized by name.</summary>
public enum DosageForm
{
    Tablet,
    Capsule,
    Syrup,
    Suspension,
    Injection,
    Infusion,
    Drops,
    Ointment,
    Cream,
    Gel,
    Inhaler,
    Powder,
    Suppository,
    Patch,
    Other,
}

/// <summary>
/// The schedule a drug falls under in India's Drugs and Cosmetics Rules — what decides
/// whether a sale needs a prescription and which register it must be entered in.
/// </summary>
public enum DrugSchedule
{
    /// <summary>Not a scheduled drug — sold without a prescription.</summary>
    None,

    /// <summary>To be taken under medical supervision; carries a caution label.</summary>
    G,

    /// <summary>Prescription only.</summary>
    H,

    /// <summary>Prescription only, and every sale recorded in the Schedule H1 register.</summary>
    H1,

    /// <summary>Prescription only, with the prescription retained and a separate register.</summary>
    X,

    /// <summary>Narcotic or psychotropic substance under the NDPS Act.</summary>
    Narcotic,
}

public enum StorageCondition
{
    RoomTemperature,

    /// <summary>Cool place, 8–25 °C.</summary>
    CoolPlace,

    /// <summary>Refrigerated, 2–8 °C — cold chain.</summary>
    Refrigerated,

    Frozen,
}
