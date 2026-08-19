namespace HMS.Shared.Kernel;

/// <summary>
/// Base type for every aggregate root across every module. Carries the standard
/// audit columns defined in docs/DatabaseArchitecture.md so no module has to
/// redeclare them.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    public DateTime CreatedAt { get; protected set; }
    public Guid? CreatedBy { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }

    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(Guid id, Guid? createdBy)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    protected void MarkUpdated(Guid? updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void SoftDelete(Guid? deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>Reverses <see cref="SoftDelete"/> — the caller is responsible for fetching
    /// this entity with its query filter bypassed first (it's excluded from ordinary
    /// queries once deleted), same as any other post-delete lookup.</summary>
    public void Restore(Guid? updatedBy)
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        MarkUpdated(updatedBy);
    }
}
