namespace HMS.Modules.Patients.Contracts;

/// <summary>Adds one emergency contact to an existing patient — the "Add another Emergency
/// Contact" endpoint.</summary>
public record AddEmergencyContactRequest
{
    public Relationship Relationship { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}
