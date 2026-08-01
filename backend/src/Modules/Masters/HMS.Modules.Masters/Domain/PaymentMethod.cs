using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>Method available for recording payments (docs/03_Masters_ERD, "Finance &amp; Payment"), e.g. Cash, Card, Bank Transfer.</summary>
internal class PaymentMethod : Entity
{
    public string MethodCode { get; private set; } = null!;
    public string MethodName { get; private set; } = null!;
    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    private PaymentMethod()
    {
    }

    private PaymentMethod(Guid id, string methodCode, string methodName, string? description, bool isActive, Guid? createdBy)
        : base(id, createdBy)
    {
        MethodCode = methodCode;
        MethodName = methodName;
        Description = description;
        IsActive = isActive;
    }

    public static PaymentMethod Create(string methodCode, string methodName, string? description, bool isActive, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(methodCode, nameof(methodCode));
        Guard.AgainstNullOrWhiteSpace(methodName, nameof(methodName));

        return new PaymentMethod(Guid.CreateVersion7(), methodCode.Trim().ToUpperInvariant(), methodName.Trim(), description?.Trim(), isActive, createdBy);
    }

    public void Update(string methodName, string? description, bool isActive, Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(methodName, nameof(methodName));

        MethodName = methodName.Trim();
        Description = description?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
