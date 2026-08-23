namespace HMS.Modules.Masters.Contracts;

public record DistrictResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid StateId { get; init; }
}
