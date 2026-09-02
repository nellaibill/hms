using System.Globalization;
using HMS.Modules.Patients.Application.Excel;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Maps one parsed Excel row into a CreatePatientRequest, the exact same contract type manual
/// registration builds — everything downstream (CreatePatientRequestValidator,
/// PatientService.CreateAsync) is then shared with the manual-registration path, so business
/// rules can never drift between the two. Parsing failures here (an enum column that isn't one
/// of the dropdown's values, an unparseable date, a State/District name that doesn't resolve)
/// are collected as ImportRowErrors alongside whatever CreatePatientRequestValidator later adds
/// — a row can report several problems at once instead of the client fixing one, re-uploading,
/// and finding the next.
/// </summary>
internal static class PatientImportRowMapper
{
    public static async Task<(CreatePatientRequest Request, List<ImportRowError> Errors)> MapAsync(
        IReadOnlyDictionary<string, string?> raw,
        PatientImportReferenceData referenceData,
        CancellationToken cancellationToken)
    {
        var errors = new List<ImportRowError>();

        var title = ParseEnum<Title>(raw, PatientImportColumns.Title, errors);
        var gender = ParseEnum<Gender>(raw, PatientImportColumns.Gender, errors);
        var bloodGroup = ParseEnum<BloodGroup>(raw, PatientImportColumns.BloodGroup, errors);
        var maritalStatus = ParseEnum<MaritalStatus>(raw, PatientImportColumns.MaritalStatus, errors);
        var modeOfArrivalSource = ParseEnum<ModeOfArrivalSource>(raw, PatientImportColumns.ModeOfArrivalSource, errors);
        var idProofType = ParseOptionalEnum<IdProofType>(raw, PatientImportColumns.IdProofType, errors);
        var dateOfBirth = ParseDate(raw, PatientImportColumns.DateOfBirth, errors);

        var (stateId, stateName) = await ResolveStateAsync(raw, referenceData, errors, cancellationToken);
        var districtId = await ResolveDistrictAsync(raw, referenceData, stateId, stateName, errors, cancellationToken);

        var hasEmergencyContact = !string.IsNullOrWhiteSpace(GetValue(raw, PatientImportColumns.EmergencyContactName));
        var relationship = hasEmergencyContact
            ? ParseEnum<Relationship>(raw, PatientImportColumns.EmergencyContactRelationship, errors)
            : default;

        var request = new CreatePatientRequest
        {
            Title = title,
            FirstName = GetValue(raw, PatientImportColumns.FirstName) ?? string.Empty,
            LastName = GetValue(raw, PatientImportColumns.LastName) ?? string.Empty,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            BloodGroup = bloodGroup,
            MaritalStatus = maritalStatus,
            PrimaryPhone = GetValue(raw, PatientImportColumns.PrimaryPhone) ?? string.Empty,
            SecondaryPhone = GetValue(raw, PatientImportColumns.SecondaryPhone),
            Email = GetValue(raw, PatientImportColumns.Email),
            Profession = GetValue(raw, PatientImportColumns.Profession),
            IdProofType = idProofType,
            IdProofNumber = GetValue(raw, PatientImportColumns.IdProofNumber),
            ModeOfArrivalSource = modeOfArrivalSource,
            ModeOfArrivalChannel = GetValue(raw, PatientImportColumns.ModeOfArrivalChannel),
            Address = new AddressRequest
            {
                AddressLine1 = GetValue(raw, PatientImportColumns.AddressLine1) ?? string.Empty,
                AddressLine2 = GetValue(raw, PatientImportColumns.AddressLine2),
                AddressLine3 = GetValue(raw, PatientImportColumns.AddressLine3),
                StateId = stateId,
                DistrictId = districtId,
                Pincode = GetValue(raw, PatientImportColumns.Pincode) ?? string.Empty,
            },
            EmergencyContacts = hasEmergencyContact
                ?
                [
                    new EmergencyContactRequest
                    {
                        Relationship = relationship,
                        Name = GetValue(raw, PatientImportColumns.EmergencyContactName) ?? string.Empty,
                        Phone = GetValue(raw, PatientImportColumns.EmergencyContactPhone) ?? string.Empty,
                    },
                ]
                : [],
        };

        if (!hasEmergencyContact)
        {
            errors.Add(new ImportRowError { Field = PatientImportColumns.EmergencyContactName, Message = "At least one emergency contact is required." });
        }

        return (request, errors);
    }

    private static async Task<(Guid StateId, string? StateName)> ResolveStateAsync(
        IReadOnlyDictionary<string, string?> raw,
        PatientImportReferenceData referenceData,
        List<ImportRowError> errors,
        CancellationToken cancellationToken)
    {
        var stateName = GetValue(raw, PatientImportColumns.State);
        if (string.IsNullOrWhiteSpace(stateName))
        {
            errors.Add(new ImportRowError { Field = PatientImportColumns.State, Message = "State is required." });
            return (Guid.Empty, null);
        }

        if (!referenceData.TryGetStateId(stateName, out var stateId))
        {
            errors.Add(new ImportRowError { Field = PatientImportColumns.State, Message = $"State '{stateName}' was not found." });
            return (Guid.Empty, stateName);
        }

        await Task.CompletedTask;
        return (stateId, stateName);
    }

    private static async Task<Guid> ResolveDistrictAsync(
        IReadOnlyDictionary<string, string?> raw,
        PatientImportReferenceData referenceData,
        Guid stateId,
        string? stateName,
        List<ImportRowError> errors,
        CancellationToken cancellationToken)
    {
        var districtName = GetValue(raw, PatientImportColumns.District);
        if (string.IsNullOrWhiteSpace(districtName))
        {
            errors.Add(new ImportRowError { Field = PatientImportColumns.District, Message = "District is required." });
            return Guid.Empty;
        }

        if (stateId == Guid.Empty)
        {
            // No point resolving a District when its State didn't resolve — the State error
            // above already explains the row.
            return Guid.Empty;
        }

        var districtId = await referenceData.FindDistrictIdAsync(stateId, districtName, cancellationToken);
        if (districtId is null)
        {
            errors.Add(new ImportRowError { Field = PatientImportColumns.District, Message = $"District '{districtName}' was not found for State '{stateName}'." });
            return Guid.Empty;
        }

        return districtId.Value;
    }

    private static string? GetValue(IReadOnlyDictionary<string, string?> raw, string header)
        => raw.TryGetValue(header, out var value) ? value : null;

    private static T ParseEnum<T>(IReadOnlyDictionary<string, string?> raw, string header, List<ImportRowError> errors) where T : struct, Enum
    {
        var value = GetValue(raw, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ImportRowError { Field = header, Message = $"{header} is required." });
            return default;
        }

        if (!Enum.TryParse<T>(value.Replace(" ", string.Empty), ignoreCase: true, out var parsed))
        {
            errors.Add(new ImportRowError { Field = header, Message = $"'{value}' is not a valid {header} — use the dropdown value." });
            return default;
        }

        return parsed;
    }

    private static T? ParseOptionalEnum<T>(IReadOnlyDictionary<string, string?> raw, string header, List<ImportRowError> errors) where T : struct, Enum
    {
        var value = GetValue(raw, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<T>(value.Replace(" ", string.Empty), ignoreCase: true, out var parsed))
        {
            errors.Add(new ImportRowError { Field = header, Message = $"'{value}' is not a valid {header} — use the dropdown value." });
            return null;
        }

        return parsed;
    }

    private static DateOnly ParseDate(IReadOnlyDictionary<string, string?> raw, string header, List<ImportRowError> errors)
    {
        var value = GetValue(raw, header);
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ImportRowError { Field = header, Message = $"{header} is required." });
            return default;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            || DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed;
        }

        errors.Add(new ImportRowError { Field = header, Message = $"'{value}' is not a valid date — use YYYY-MM-DD." });
        return default;
    }
}
