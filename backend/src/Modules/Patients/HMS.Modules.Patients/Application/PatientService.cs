using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Application.Mapping;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Patients.Application;

/// <summary>
/// Orchestrates Patients use cases. Registration is a single combined transaction (create
/// the Patient master record + its first PatientRegistration together), per
/// docs/PatientRegistrationModule.md's "one save" flow — the multi-step wizard/autosave
/// infrastructure the full spec describes is deferred (see docs/DecisionLog.md).
/// </summary>
internal class PatientService : IPatientService
{
    private static readonly string[] AllowedPhotoContentTypes = ["image/jpeg", "image/png"];
    private static readonly string[] AllowedIdProofContentTypes = ["image/jpeg", "image/png", "application/pdf"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB, per docs/PatientRegistrationModule.md §12

    private readonly IPatientRepository _repository;
    private readonly IPatientIdentifierGenerator _identifierGenerator;
    private readonly IPatientFileStorage _fileStorage;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IPatientRepository repository,
        IPatientIdentifierGenerator identifierGenerator,
        IPatientFileStorage fileStorage,
        ILogger<PatientService> logger)
    {
        _repository = repository;
        _identifierGenerator = identifierGenerator;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var uhid = await _identifierGenerator.NextUhidAsync(cancellationToken);

        var patient = Patient.Create(
            uhid,
            request.Title,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.BloodGroup,
            request.AddressLine1,
            request.AddressLine2,
            request.AddressLine3,
            request.District,
            request.State,
            request.Pincode,
            request.PrimaryPhone,
            request.PrimaryPhoneRelation,
            request.AlternatePhone,
            request.Email,
            request.Profession,
            request.EmergencyContactRelationship,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.HasKnownAllergy,
            request.AllergyType,
            request.AllergySeverity,
            actorId);

        var registrationNumber = await _identifierGenerator.NextRegistrationNumberAsync(request.Registration.EncounterType, cancellationToken);

        var registration = PatientRegistration.Create(
            patient.Id,
            registrationNumber,
            request.Registration.EncounterType,
            request.Registration.ModeOfArrival,
            request.Registration.Department,
            request.Registration.Consultant,
            request.Registration.AdmissionType,
            request.Registration.ReferralSource,
            request.Registration.Category,
            actorId);

        patient.AddRegistration(registration);

        await _repository.AddAsync(patient, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered patient {PatientId} with UHID {Uhid}", patient.Id, patient.Uhid);

        return Result<PatientResponse>.Success(patient.ToResponse());
    }

    public async Task<Result<PatientResponse>> UpdateAsync(Guid id, UpdatePatientRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.");
        }

        patient.UpdateDemographics(request.Title, request.FirstName, request.LastName, request.DateOfBirth, request.Gender, request.BloodGroup, actorId);
        patient.UpdateAddress(request.AddressLine1, request.AddressLine2, request.AddressLine3, request.District, request.State, request.Pincode, actorId);
        patient.UpdateContact(request.PrimaryPhone, request.PrimaryPhoneRelation, request.AlternatePhone, request.Email, request.Profession, actorId);
        patient.UpdateEmergencyContact(request.EmergencyContactRelationship, request.EmergencyContactName, request.EmergencyContactPhone, actorId);
        patient.UpdateAllergyDetails(request.HasKnownAllergy, request.AllergyType, request.AllergySeverity, actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse());
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.");
        }

        patient.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted patient {PatientId}", patient.Id);

        return Result.Success();
    }

    public async Task<Result<PatientResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        return patient is null
            ? Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.")
            : Result<PatientResponse>.Success(patient.ToResponse());
    }

    public async Task<PagedResult<PatientResponse>> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        var mapped = items.Select(p => p.ToResponse()).ToList();

        return new PagedResult<PatientResponse>(mapped, query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<PatientResponse>> UploadPhotoAsync(Guid id, Stream content, string fileName, string contentType, long length, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.");
        }

        if (length > MaxFileSizeBytes || !AllowedPhotoContentTypes.Contains(contentType))
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.InvalidFile, "Photo must be a JPG or PNG file, max 5MB.");
        }

        var path = await _fileStorage.SaveAsync(id, "photo", fileName, content, cancellationToken);
        patient.SetPhoto(path, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded photo for patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse());
    }

    public async Task<Result<PatientResponse>> UploadIdProofAsync(Guid id, IdProofType idProofType, Stream content, string fileName, string contentType, long length, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.");
        }

        if (length > MaxFileSizeBytes || !AllowedIdProofContentTypes.Contains(contentType))
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.InvalidFile, "ID proof must be a JPG, PNG, or PDF file, max 5MB.");
        }

        var path = await _fileStorage.SaveAsync(id, "id-proof", fileName, content, cancellationToken);
        patient.SetIdProof(idProofType, path, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded ID proof for patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse());
    }
}
