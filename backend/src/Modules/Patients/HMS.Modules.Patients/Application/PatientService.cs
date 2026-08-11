using HMS.Modules.Masters.Application;
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
    private readonly IDepartmentService _departmentService;
    private readonly IConsultantService _consultantService;
    private readonly IAppointmentTypeService _appointmentTypeService;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        IPatientRepository repository,
        IPatientIdentifierGenerator identifierGenerator,
        IPatientFileStorage fileStorage,
        IDepartmentService departmentService,
        IConsultantService consultantService,
        IAppointmentTypeService appointmentTypeService,
        ILogger<PatientService> logger)
    {
        _repository = repository;
        _identifierGenerator = identifierGenerator;
        _fileStorage = fileStorage;
        _departmentService = departmentService;
        _consultantService = consultantService;
        _appointmentTypeService = appointmentTypeService;
        _logger = logger;
    }

    // DepartmentId/ConsultantId/AppointmentTypeId are all cross-module references into
    // Masters' reference data (see docs/DecisionLog.md — Department/Consultant consolidated
    // there; AppointmentType followed the same pattern). AppointmentTypeId is optional
    // (OP-only), so it's only checked when the caller actually supplied one.
    private async Task<Result?> ValidateRegistrationReferencesAsync(Guid departmentId, Guid consultantId, Guid? appointmentTypeId, CancellationToken cancellationToken)
    {
        if (!(await _departmentService.GetByIdAsync(departmentId, cancellationToken)).IsSuccess)
        {
            return Result.Failure(PatientErrorCodes.InvalidDepartment, $"Department '{departmentId}' was not found.");
        }

        if (!(await _consultantService.GetByIdAsync(consultantId, cancellationToken)).IsSuccess)
        {
            return Result.Failure(PatientErrorCodes.InvalidConsultant, $"Consultant '{consultantId}' was not found.");
        }

        if (appointmentTypeId.HasValue && !(await _appointmentTypeService.GetByIdAsync(appointmentTypeId.Value, cancellationToken)).IsSuccess)
        {
            return Result.Failure(PatientErrorCodes.InvalidAppointmentType, $"Appointment type '{appointmentTypeId}' was not found.");
        }

        return null;
    }

    public async Task<Result<PatientResponse>> CreateAsync(CreatePatientRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var duplicate = await _repository.FindDuplicateAsync(request.PrimaryPhone, request.FirstName, request.LastName, cancellationToken);
        if (duplicate is not null)
        {
            return Result<PatientResponse>.Failure(
                PatientErrorCodes.DuplicatePatient,
                $"A patient named '{duplicate.FirstName} {duplicate.LastName}' with this phone number is already registered (UHID: {duplicate.Uhid}). If this is a returning patient, record a new visit against their existing record instead of registering them again.");
        }

        var referenceError = await ValidateRegistrationReferencesAsync(request.Registration.DepartmentId, request.Registration.ConsultantId, request.Registration.AppointmentTypeId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<PatientResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

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
            request.AlternatePhoneRelation,
            request.AlternatePhone2,
            request.AlternatePhone2Relation,
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
            request.Registration.DepartmentId,
            request.Registration.ConsultantId,
            request.Registration.AppointmentTypeId,
            request.Registration.AdmissionType,
            request.Registration.ReferralSource,
            request.Registration.Category,
            actorId);

        patient.AddRegistration(registration);

        await _repository.AddAsync(patient, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered patient {PatientId} with UHID {Uhid}", patient.Id, patient.Uhid);

        return Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
    }

    public async Task<Result<PatientResponse>> UpdateAsync(Guid id, UpdatePatientRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(id, cancellationToken);
        if (patient is null)
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{id}' was not found.");
        }

        // Optimistic concurrency: the client must be editing the version it actually loaded.
        // Without this, two people editing the same patient at once silently overwrite each
        // other — the second save always wins with no warning to either party.
        var loadedRowVersion = _repository.GetRowVersion(patient);
        if (request.RowVersion != loadedRowVersion)
        {
            return Result<PatientResponse>.Failure(
                PatientErrorCodes.ConcurrencyConflict,
                "This patient's details were changed by someone else since this page was loaded. Reload the page to see the latest version, then try your edit again.");
        }

        patient.UpdateDemographics(request.Title, request.FirstName, request.LastName, request.DateOfBirth, request.Gender, request.BloodGroup, actorId);
        patient.UpdateAddress(request.AddressLine1, request.AddressLine2, request.AddressLine3, request.District, request.State, request.Pincode, actorId);
        patient.UpdateContact(
            request.PrimaryPhone,
            request.PrimaryPhoneRelation,
            request.AlternatePhone,
            request.AlternatePhoneRelation,
            request.AlternatePhone2,
            request.AlternatePhone2Relation,
            request.Email,
            request.Profession,
            actorId);
        patient.UpdateEmergencyContact(request.EmergencyContactRelationship, request.EmergencyContactName, request.EmergencyContactPhone, actorId);
        patient.UpdateAllergyDetails(request.HasKnownAllergy, request.AllergyType, request.AllergySeverity, actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
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
            : Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
    }

    public async Task<Result<PatientRegistrationResponse>> AddRegistrationAsync(Guid patientId, PatientRegistrationDetails request, Guid? actorId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return Result<PatientRegistrationResponse>.Failure(PatientErrorCodes.NotFound, $"Patient '{patientId}' was not found.");
        }

        var referenceError = await ValidateRegistrationReferencesAsync(request.DepartmentId, request.ConsultantId, request.AppointmentTypeId, cancellationToken);
        if (referenceError is not null)
        {
            return Result<PatientRegistrationResponse>.Failure(referenceError.ErrorCode!, referenceError.Error!);
        }

        var registrationNumber = await _identifierGenerator.NextRegistrationNumberAsync(request.EncounterType, cancellationToken);

        var registration = PatientRegistration.Create(
            patient.Id,
            registrationNumber,
            request.EncounterType,
            request.ModeOfArrival,
            request.DepartmentId,
            request.ConsultantId,
            request.AppointmentTypeId,
            request.AdmissionType,
            request.ReferralSource,
            request.Category,
            actorId);

        patient.AddRegistration(registration);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recorded new registration {RegistrationNumber} for patient {PatientId}", registration.RegistrationNumber, patient.Id);

        return Result<PatientRegistrationResponse>.Success(registration.ToResponse());
    }

    public async Task<Result<IReadOnlyList<PatientRegistrationResponse>>> GetRegistrationsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(patientId, cancellationToken);
        if (patient is null)
        {
            return Result<IReadOnlyList<PatientRegistrationResponse>>.Failure(PatientErrorCodes.NotFound, $"Patient '{patientId}' was not found.");
        }

        var registrations = patient.Registrations
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.ToResponse())
            .ToList();

        return Result<IReadOnlyList<PatientRegistrationResponse>>.Success(registrations);
    }

    public async Task<PagedResult<PatientResponse>> GetPagedAsync(PatientListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        var mapped = items.Select(p => p.ToResponse(_repository.GetRowVersion(p))).ToList();

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

        if (!await LooksLikeJpegOrPngAsync(content, cancellationToken))
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.InvalidFile, "The uploaded file is not a valid JPG or PNG image.");
        }

        var path = await _fileStorage.SaveAsync(id, "photo", fileName, content, cancellationToken);
        patient.SetPhoto(path, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded photo for patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
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

        if (!await LooksLikeJpegOrPngOrPdfAsync(content, cancellationToken))
        {
            return Result<PatientResponse>.Failure(PatientErrorCodes.InvalidFile, "The uploaded file is not a valid JPG, PNG, or PDF.");
        }

        var path = await _fileStorage.SaveAsync(id, "id-proof", fileName, content, cancellationToken);
        patient.SetIdProof(idProofType, path, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded ID proof for patient {PatientId}", patient.Id);

        return Result<PatientResponse>.Success(patient.ToResponse(_repository.GetRowVersion(patient)));
    }

    // A client-supplied Content-Type header is just a claim — this checks the file's
    // actual bytes against the well-known signatures for the allowed formats, so a
    // renamed non-image (e.g. "chart.jpg" containing a script) is still rejected. Mirrors
    // HMS.Modules.Identity.UserService's LooksLikeAnAllowedImageAsync. Resets the stream
    // position afterward so the full content is still there for SaveAsync.
    private static async Task<bool> LooksLikeJpegOrPngAsync(Stream content, CancellationToken cancellationToken)
    {
        var (header, bytesRead) = await ReadHeaderAsync(content, cancellationToken);
        return IsJpeg(header, bytesRead) || IsPng(header, bytesRead);
    }

    private static async Task<bool> LooksLikeJpegOrPngOrPdfAsync(Stream content, CancellationToken cancellationToken)
    {
        var (header, bytesRead) = await ReadHeaderAsync(content, cancellationToken);
        return IsJpeg(header, bytesRead) || IsPng(header, bytesRead) || IsPdf(header, bytesRead);
    }

    private static async Task<(byte[] Header, int BytesRead)> ReadHeaderAsync(Stream content, CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var bytesRead = await content.ReadAtLeastAsync(header.AsMemory(0, header.Length), header.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;
        return (header, bytesRead);
    }

    // JPEG: FF D8 FF
    private static bool IsJpeg(byte[] header, int bytesRead) =>
        bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

    // PNG: 89 50 4E 47 0D 0A 1A 0A
    private static bool IsPng(byte[] header, int bytesRead) =>
        bytesRead >= 8 &&
        header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
        header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

    // PDF: "%PDF-"
    private static bool IsPdf(byte[] header, int bytesRead) =>
        bytesRead >= 5 &&
        header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46 && header[4] == 0x2D;
}
