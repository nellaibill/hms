namespace HMS.Modules.Laboratory.Application;

internal static class LaboratoryErrorCodes
{
    public const string OrderNotFound = "LABORATORY.ORDER_NOT_FOUND";
    public const string ItemNotFound = "LABORATORY.ITEM_NOT_FOUND";
    public const string InvalidServiceOrPackage = "LABORATORY.INVALID_SERVICE_OR_PACKAGE";
    public const string EmptyOrder = "LABORATORY.EMPTY_ORDER";

    /// <summary>One shared code for every mutator's precondition failure (an illegal status
    /// transition) — the message string explains the specifics, mirroring how
    /// Invoice.Void/InvoiceService's own InvalidOperationException catch blocks work.</summary>
    public const string InvalidStatusTransition = "LABORATORY.INVALID_STATUS_TRANSITION";

    public const string NotAllItemsVerified = "LABORATORY.NOT_ALL_ITEMS_VERIFIED";
    public const string ReportNotGenerated = "LABORATORY.REPORT_NOT_GENERATED";
    public const string AlreadyReleased = "LABORATORY.ALREADY_RELEASED";
}
