namespace HMS.Modules.Identity.Contracts;

public record PermissionResponse
{
    public Guid Id { get; init; }

    public string Module { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }
}
