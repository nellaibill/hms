namespace HMS.Modules.Pharmacy.Application;

internal static class PharmacyErrorCodes
{
    public const string InvalidProduct = "PHARMACY.INVALID_PRODUCT";
    public const string InvalidBatch = "PHARMACY.INVALID_BATCH";
    public const string InvalidPatient = "PHARMACY.INVALID_PATIENT";
    public const string InvalidStore = "PHARMACY.INVALID_STORE";
    public const string DuplicateStoreCode = "PHARMACY.DUPLICATE_STORE_CODE";
    public const string DefaultStoreRequired = "PHARMACY.DEFAULT_STORE_REQUIRED";
    public const string ProductInactive = "PHARMACY.PRODUCT_INACTIVE";
    public const string BatchInactive = "PHARMACY.BATCH_INACTIVE";
    public const string BatchExpired = "PHARMACY.BATCH_EXPIRED";
    public const string InsufficientStock = "PHARMACY.INSUFFICIENT_STOCK";
    public const string NotFound = "PHARMACY.NOT_FOUND";
}
