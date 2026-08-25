namespace HMS.Modules.Patients.Contracts;

/// <summary>Adds one allergy row to an existing patient — the "Add another Allergy" endpoint.</summary>
public record AddAllergyRequest
{
    public AllergyType AllergyType { get; init; }
    public string? Specify { get; init; }
    public AllergySeverity Severity { get; init; }
}
