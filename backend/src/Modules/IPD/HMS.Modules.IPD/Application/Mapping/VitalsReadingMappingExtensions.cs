using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Domain;

namespace HMS.Modules.IPD.Application.Mapping;

internal static class VitalsReadingMappingExtensions
{
    public static VitalsReadingResponse ToResponse(this VitalsReading reading) => new()
    {
        Id = reading.Id,
        AdmissionId = reading.AdmissionId,
        RecordedAt = reading.RecordedAt,
        TemperatureF = reading.TemperatureF,
        PulseRate = reading.PulseRate,
        RespiratoryRate = reading.RespiratoryRate,
        BloodPressureSystolic = reading.BloodPressureSystolic,
        BloodPressureDiastolic = reading.BloodPressureDiastolic,
        SpO2Percent = reading.SpO2Percent,
        WeightKg = reading.WeightKg,
        HeightCm = reading.HeightCm,
        PainScore = reading.PainScore,
        BloodGlucoseMgDl = reading.BloodGlucoseMgDl,
        RecordedByUserId = reading.RecordedByUserId,
        Notes = reading.Notes,
        CreatedAt = reading.CreatedAt,
    };
}
