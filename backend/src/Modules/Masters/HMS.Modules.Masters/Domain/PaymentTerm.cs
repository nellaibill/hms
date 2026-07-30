using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>Standard payment term offered to/by Suppliers and Customers (docs/03_Masters_ERD, "Finance &amp; Payment"), e.g. Net 30. Uniqueness is on <see cref="TermName"/> — there is no separate code column.</summary>
internal class PaymentTerm : Entity
{
    public string TermName { get; private set; } = null!;
    public int Days { get; private set; }
    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    private PaymentTerm()
    {
    }

    private PaymentTerm(Guid id, string termName, int days, string? description, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        TermName = termName;
        Days = days;
        Description = description;
        IsActive = isActive;
    }

    public static PaymentTerm Create(string termName, int days, string? description, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(termName, nameof(termName));
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Days cannot be negative.");
        }

        return new PaymentTerm(Guid.CreateVersion7(), termName.Trim(), days, description?.Trim(), isActive, createdBy);
    }

    public void Update(string termName, int days, string? description, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(termName, nameof(termName));
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Days cannot be negative.");
        }

        TermName = termName.Trim();
        Days = days;
        Description = description?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
