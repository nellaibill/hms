namespace HMS.Modules.Masters.Contracts;

public record StateResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
