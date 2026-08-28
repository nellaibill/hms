namespace HMS.Modules.Billing.Application;

internal static class BillingErrorCodes
{
    public const string NotFound = "BILLING.NOT_FOUND";
    public const string EmptyInvoice = "BILLING.EMPTY_INVOICE";
    public const string InvalidPatient = "BILLING.INVALID_PATIENT";
    public const string LineItemNotFound = "BILLING.LINE_ITEM_NOT_FOUND";
    public const string LineItemAlreadyPaid = "BILLING.LINE_ITEM_ALREADY_PAID";
    public const string AlreadyVoided = "BILLING.ALREADY_VOIDED";
    public const string HasPayments = "BILLING.HAS_PAYMENTS";
}
