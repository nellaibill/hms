using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Masters.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Mapping;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Orchestrates Patients use cases. Registration is a single combined transaction (patient +
/// address + any allergies/emergency contacts supplied up front). Encounter/visit
/// registration (department, consultant, admission type) is out of scope for this iteration.
/// </summary>
internal class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly IPatientIdentifierGenerator _identifierGenerator;
    private readonly IStateService _stateService;
    private readonly IDistrictService _districtService;
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IPatientRepository repository,
        IPatientIdentifierGenerator identifierGenerator,
        IStateService stateService,
        IDistrictService districtService,
        IActivityLogService activityLog,
        ILogger<PatientService> logger)
    {
        _repository = repository;
        _identifierGenerator = identifierGenerator;
        _stateService = stateService;
        _districtService = districtService;
        _activityLog = activityLog;
        _logger = logger;
    }

    // StateId/DistrictId are cross-module references into Masters' reference data — a
    // district must both exist and belong to the given state, matching the Excel's own
    // "must belong to selected state" validation note.
    private async Task<Result?> ValidateAddressReferencesAsync(Guid stateId, Guid districtId, CancellationToken cancellationToken)
    {
        var states = await _stateService.GetAllAsync(cancellationToken);
        if (!states.Any(s => s.Id == stateId))
        {
            return Result.Failure(PatientErrorCodes.InvalidState, $"State '{stateId}' was not found.");
        }

        var districts = await _districtService.GetByStateIdAsync(stateId, cancellationToken);
        if (!districts.Any(d => d.Id == districtId))
        {
            return Result.Failure(PatientErrorCodes.InvalidDistrict, $"District '{districtId}' was not found for the selected state.");
        }

        return null;
    }

    // Best-effort audit write (never throws — see IActivityLogService); runs only after the
    // patient change itself has been saved.
    private Task LogAsync(string action, Patient patient, string description, object? oldValues, object? newValues, Guid? actorId, CancellationToken cancellationToken)
        => _activityLog.LogAsync(new ActivityLogRequest
        {
            Action = action,
            Module = ActivityLogModules.Patients,
            EntityType = "Patient",
            EntityId = patient.Id.ToString(),
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = actorId,
        }, cancellationToken);

    public async Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request, Guid? actorId, CancellationToken cancellationToken, string? uhidOverride = null, bool requiresDataVerification = false)
    {
        var duplicate = await _repository.FindDuplicateAsync(request.PrimaryPhone, request.FirstName, request.LastName, request.IdProofNumber, cancellationToken);
        if (duplicate is not null)
        {
            return Result<PatientResponse>.Failure(
                PatientErrorCodes.DuplicatePatient,
                $"A patient named '{duplicate.FirstName} {duplicate.LastName}' with this phone number (and ID number, if supplied) is already registered (UHID: {duplicate.Uhid}). If this is a returning patient, use their existing record instead of registering them again.");
        }

        var referenceError = await ValidateAddressReferencesAsync(request.Address.StateId, request.Address.DistrictId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<PatientResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        var uhid = uhidOverride ?? await _identifierGenerator.NextUhidAsync(cancellationToken);

        var patient = Patient.Create(
            uhid,
            request.Title,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.BloodGroup,
            request.MaritalStatus,
            request.PrimaryPhone,
            request.SecondaryPhone,
            request.Email,
            request.Profession,
            request.IdProofType,
            request.IdProofNumber,
            request.ModeOfArrivalSource,
            request.ModeOfArrivalChannel,
            request.ModeOfArrivalSpecify,
            actorId,
            requiresDataVerification);

        var address = Address.Create(
            patient.Id,
            request.Address.AddressLine1,
            request.Address.AddressLine2,
            request.Address.AddressLine3,
            request.Address.StateId,
            request.Address.DistrictId,
            request.Address.Pincode);
        patient.SetAddress(address);

        foreach (var allergyRequest in request.Allergies)
        {
            patient.AddAllergy(Allergy.Create(patient.Id, allergyRequest.AllergyType, allergyRequest.Specify, allergyRequest.Severity), actorId);
        }

        foreach (var contactRequest in request.EmergencyContacts)
        {
            patient.AddEmergencyContact(EmergencyContact.Create(patient.Id, contactRequest.Relationship, contactRequest.Name, contactRequest.Phone), actorId);
        }

        await _repository.AddAsync(patient, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered patient {PatientId} with UHID {Uhid}", patient.Id, patient.Uhid);

        var created = patient.ToResponse(_repository.GetRowVersion(patient));
        await LogAsync(ActivityLogActions.Create, patient, $"Registered patient {patient.Uhid}", null, created, actorId, cancellationToken);

        return Result<PatientResponse>.Success(created);
    }

    public async Task<Result<PatientResponse>> UpdateAsync(Guid id, UpdatePatientRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.");
        }

        // Optimistic concurrency: the client must be editing the version it actually loaded.
        var loadedRowVersion = _repository.GetRowVersion(patient);
        if (request.RowVersion != loadedRowVersion)
        {
            return Result<PatientResponse>.Failure(
                PatientErrorCodes.ConcurrencyConflict,
                "This patient's details were changed by someone else since this page was loaded. Reload the page to see the latest version, then try your edit again.");
        }

        var referenceError = await ValidateAddressReferencesAsync(request.Address.StateId, request.Address.DistrictId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<PatientResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        var before = patient.ToResponse(loadedRowVersion);

        patient.UpdateDemographics(request.Title, request.FirstName, request.LastName, request.DateOfBirth, request.Gender, request.BloodGroup, request.MaritalStatus, actorId);
        patient.UpdateContact(request.PrimaryPhone, request.SecondaryPhone, request.Email, request.Profession, actorId);
        patient.UpdateIdProof(request.IdProofType, request.IdProofNumber, actorId);
        patient.UpdateModeOfArrival(request.ModeOfArrivalSource, request.ModeOfArrivalChannel, request.ModeOfArrivalSpecify, actorId);
        patient.UpdateAddress(request.Address.AddressLine1, request.Address.AddressLine2, request.Address.AddressLine3, request.Address.StateId, request.Address.DistrictId, request.Address.Pincode, actorId);

        // A full save through the Edit Patient screen counts as the receptionist having
        // re-confirmed the patient's details with them — see Patient.ClearDataVerificationFlag.
        patient.ClearDataVerificationFlag(actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated patient {PatientId}", patient.Id);

        var updated = patient.ToResponse(_repository.GetRowVersion(patient));
        await LogAsync(ActivityLogActions.Update, patient, $"Updated patient {patient.Uhid}", before, updated, actorId, cancellationToken);

        return Result<PatientResponse>.Success(updated);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.");
        }

        var deleted = patient.ToResponse(_repository.GetRowVersion(patient));

        patient.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted patient {PatientId}", patient.Id);

        await LogAsync(ActivityLogActions.Delete, patient, $"Deleted patient {patient.Uhid}", deleted, null, actorId, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PatientResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        return patient is null
            ? Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.")
            : Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
    }

    public async Task<PagedResult<PatientResponse>> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        var mapped = items.Select(p => p.ToResponse(_repository.GetRowVersion(p))).ToList();

        if (query.IncludeLastVisit == true)
        {
            var lastVisits = await _repository.GetLastVisitsAsync(items.Select(p => p.Id).ToList(), cancellationToken);
            mapped = mapped.Select(response =>
            {
                if (!lastVisits.TryGetValue(response.Id, out var lastVisit)) return response;
                var consultation = lastVisit.Consultations.FirstOrDefault();
                return response with
                {
                    LastVisitDepartmentId = consultation?.DepartmentId,
                    LastVisitConsultantId = consultation?.ConsultantId,
                    LastVisitAppointmentTime = consultation?.AppointmentTime,
                };
            }).ToList();
        }

        return new PagedResult<PatientResponse>(mapped, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Patient Reports' table — same paging/filtering as GetPagedAsync, enriched with
    /// each row's last visit/department (see PatientRepository.GetLastVisitsAsync).</summary>
    public async Task<PagedResult<PatientReportRowResponse>> GetReportPagedAsync(PatientListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        var mapped = await MapReportRowsAsync(items, cancellationToken);
        return new PagedResult<PatientReportRowResponse>(mapped, query.Page, query.PageSize, totalCount);
    }

    /// <summary>Every matching row (up to a safety cap), for the Excel export — see
    /// PatientRepository.GetExportRowsAsync's own doc comment for why this isn't paginated.</summary>
    public async Task<IReadOnlyList<PatientReportRowResponse>> GetReportExportRowsAsync(PatientListQuery query, int maxRows, CancellationToken cancellationToken)
    {
        var items = await _repository.GetExportRowsAsync(query, maxRows, cancellationToken);
        return await MapReportRowsAsync(items, cancellationToken);
    }

    private async Task<List<PatientReportRowResponse>> MapReportRowsAsync(IReadOnlyList<Patient> items, CancellationToken cancellationToken)
    {
        var lastVisits = await _repository.GetLastVisitsAsync(items.Select(p => p.Id).ToList(), cancellationToken);

        return items.Select(p =>
        {
            var hasLastVisit = lastVisits.TryGetValue(p.Id, out var lastVisit);
            return new PatientReportRowResponse
            {
                Patient = p.ToResponse(_repository.GetRowVersion(p)),
                LastVisitAt = hasLastVisit ? lastVisit!.CreatedAt : null,
                LastVisitDepartmentId = hasLastVisit ? lastVisit!.Consultations.FirstOrDefault()?.DepartmentId : null,
            };
        }).ToList();
    }

    /// <summary>Patient Reports' summary cards.</summary>
    public async Task<PatientReportSummaryResponse> GetReportSummaryAsync(PatientListQuery query, CancellationToken cancellationToken)
    {
        var (totalPatients, newPatients, totalVisits) = await _repository.GetReportSummaryAsync(query, cancellationToken);
        return new PatientReportSummaryResponse
        {
            TotalPatients = totalPatients,
            NewPatients = newPatients,
            ReturningPatients = totalPatients - newPatients,
            TotalVisits = totalVisits,
        };
    }

    public async Task<Result<PatientResponse>> AddAllergyAsync(Guid patientId, AddAllergyRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{patientId}' was not found.");
        }

        patient.AddAllergy(Allergy.Create(patient.Id, request.AllergyType, request.Specify, request.Severity), actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added an allergy for patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
    }

    public async Task<Result<PatientResponse>> RemoveAllergyAsync(Guid patientId, Guid allergyId, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{patientId}' was not found.");
        }

        if (!patient.RemoveAllergy(allergyId, actorId))
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.AllergyNotFound, $"Allergy '{allergyId}' was not found for this patient.");
        }

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed an allergy for patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
    }

    public async Task<Result<PatientResponse>> AddEmergencyContactAsync(Guid patientId, AddEmergencyContactRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{patientId}' was not found.");
        }

        patient.AddEmergencyContact(EmergencyContact.Create(patient.Id, request.Relationship, request.Name, request.Phone), actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added an emergency contact for patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
    }

    public async Task<Result<PatientResponse>> RemoveEmergencyContactAsync(Guid patientId, Guid emergencyContactId, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{patientId}' was not found.");
        }

        if (!patient.EmergencyContacts.Any(c => c.Id == emergencyContactId))
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.EmergencyContactNotFound, $"Emergency contact '{emergencyContactId}' was not found for this patient.");
        }

        if (patient.EmergencyContacts.Count <= 1)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.CannotRemoveLastEmergencyContact, "A patient must have at least one emergency contact.");
        }

        patient.RemoveEmergencyContact(emergencyContactId, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed an emergency contact for patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
    }
}
