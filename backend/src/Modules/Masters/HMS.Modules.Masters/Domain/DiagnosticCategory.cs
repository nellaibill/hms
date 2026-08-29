using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A grouping for <see cref="DiagnosticService"/> rows (e.g. "Hematology", "CT Scan") — part
/// of the normalized replacement for the old flat DiagnosticTest.Category free-text string,
/// covering the Laboratory/Radiology half of that entity. Procedure-type billing stays on the
/// old DiagnosticTest untouched. Categories are scoped per DiagnosticService.ServiceType at
/// the data-migration level (Lab and Radiology categories are never merged even when their
/// names coincide), but this entity itself carries no ServiceType — a category is just a
/// named grouping; DiagnosticService is what ties a category to Laboratory or Radiology.
/// </summary>
internal class DiagnosticCategory : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required by EF Core materialization.
    private DiagnosticCategory()
    {
    }

    private DiagnosticCategory(
        Guid id,
        string code,
        string name,
        string? description,
        bool isActive,
        Guid? createdBy)
        : base(id, createdBy)
    {
        Code = code;
        Name = name;
        Description = description;
        IsActive = isActive;
    }

    public static DiagnosticCategory Create(
        string code,
        string name,
        string? description,
        bool isActive,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        return new DiagnosticCategory(
            Guid.CreateVersion7(),
            code.Trim(),
            name.Trim(),
            description?.Trim(),
            isActive,
            createdBy);
    }

    public void Update(
        string code,
        string name,
        string? description,
        bool isActive,
        Guid? updatedBy)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        Code = code.Trim();
        Name = name.Trim();
        Description = description?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
