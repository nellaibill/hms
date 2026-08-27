namespace HMS.Modules.Messaging.Contracts;

/// <summary>Entity-owned enum kept in Contracts (not Domain) because it's also referenced
/// by DTOs added in a later phase — mirrors HMS.Modules.Pharmacy's Contracts/
/// PharmacyEnums.cs convention (e.g. TransactionType).</summary>
public enum ConversationType
{
    OneToOne,
    Group,
}
