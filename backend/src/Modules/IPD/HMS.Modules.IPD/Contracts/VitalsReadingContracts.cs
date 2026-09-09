namespace HMS.Modules.IPD.Contracts;

public record CreateVitalsReadingRequest
{
    public DateTime RecordedAt { get; init; }
    public decimal? TemperatureF { get; init; }
    public int? PulseRate { get; init; }
    public int? RespiratoryRate { get; init; }
    public int? BloodPressureSystolic { get; init; }
    public int? BloodPressureDiastolic { get; init; }
    public int? SpO2Percent { get; init; }
    public decimal? WeightKg { get; init; }
    public decimal? HeightCm { get; init; }
    public int? PainScore { get; init; }
    public decimal? BloodGlucoseMgDl { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public string? Notes { get; init; }
}

public record VitalsReadingResponse
{
    public Guid Id { get; init; }
    public Guid AdmissionId { get; init; }
    public DateTime RecordedAt { get; init; }
    public decimal? TemperatureF { get; init; }
    public int? PulseRate { get; init; }
    public int? RespiratoryRate { get; init; }
    public int? BloodPressureSystolic { get; init; }
    public int? BloodPressureDiastolic { get; init; }
    public int? SpO2Percent { get; init; }
    public decimal? WeightKg { get; init; }
    public decimal? HeightCm { get; init; }
    public int? PainScore { get; init; }
    public decimal? BloodGlucoseMgDl { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}
