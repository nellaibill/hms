using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): ConsultantsController requires a public constructor dependency
/// (CS0051 otherwise). Interface and implementation share this file, matching the other
/// Masters entities' {Entity}Service.cs convention.
/// </summary>
public interface IConsultantService
{
    Task<Result<ConsultantResponse>> CreateAsync(CreateConsultantRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ConsultantResponse>> UpdateAsync(Guid id, UpdateConsultantRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<ConsultantResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<ConsultantResponse>> GetPagedAsync(ConsultantListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    /// <summary>Uploads (or replaces) a consultant's photo — a separate action from Create/
    /// Update, mirroring HMS.Modules.Identity's IUserService.UploadProfilePhotoAsync: a photo
    /// is only ever added once the consultant record already exists.</summary>
    Task<Result<ConsultantResponse>> UploadPhotoAsync(
        Guid id, Stream content, string fileName, string contentType, long length, Guid? actorId, CancellationToken cancellationToken);
}

internal class ConsultantService : IConsultantService
{
    // JPG/PNG only (not WEBP) and 2MB, matching the Consultant Photo upload area's own
    // "JPG, PNG (Max 2MB)" caption — deliberately narrower than Identity's profile-photo
    // upload (which also allows WEBP), since that's what this specific feature asked for.
    private static readonly string[] AllowedPhotoExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedPhotoContentTypes = ["image/jpeg", "image/png"];
    private const long MaxPhotoSizeBytes = 2 * 1024 * 1024;

    private readonly IConsultantRepository _repository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IConsultationTypeRepository _consultationTypeRepository;
    private readonly IConsultantFileStorage _fileStorage;

    public ConsultantService(
        IConsultantRepository repository,
        IDepartmentRepository departmentRepository,
        IConsultationTypeRepository consultationTypeRepository,
        IConsultantFileStorage fileStorage)
    {
        _repository = repository;
        _departmentRepository = departmentRepository;
        _consultationTypeRepository = consultationTypeRepository;
        _fileStorage = fileStorage;
    }

    public async Task<Result<ConsultantResponse>> CreateAsync(CreateConsultantRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        // No duplicate-name check here, deliberately — unlike AppointmentType/ConsultationType,
        // two consultants can legitimately share a display name (e.g. two "Dr. Sharma"s); see
        // ConsultantConfiguration's own comment on why Name carries no uniqueness constraint.
        if (request.DepartmentId.HasValue && !await _departmentRepository.ExistsAsync(request.DepartmentId.Value, cancellationToken))
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidReference, $"Department '{request.DepartmentId}' was not found.");
        }

        var invalidConsultationTypeId = await FindInvalidConsultationTypeIdAsync(request.ConsultationTypeCharges, cancellationToken);
        if (invalidConsultationTypeId.HasValue)
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidReference, $"Consultation type '{invalidConsultationTypeId}' was not found.");
        }

        var consultant = Consultant.Create(
            request.Name,
            request.DepartmentId,
            request.Specialization,
            request.IsActive,
            request.Priority,
            request.AvailableDays,
            request.VisitStartTime,
            request.VisitEndTime,
            ToSelections(request.ConsultationTypeCharges),
            actorId,
            request.VisitStartTime2,
            request.VisitEndTime2);

        await _repository.AddAsync(consultant, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ConsultantResponse>.Success(consultant.ToResponse());
    }

    public async Task<Result<ConsultantResponse>> UpdateAsync(Guid id, UpdateConsultantRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultant = await _repository.GetByIdAsync(id, cancellationToken);
        if (consultant is null)
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.NotFound, $"Consultant '{id}' was not found.");
        }

        if (request.DepartmentId.HasValue && !await _departmentRepository.ExistsAsync(request.DepartmentId.Value, cancellationToken))
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidReference, $"Department '{request.DepartmentId}' was not found.");
        }

        var invalidConsultationTypeId = await FindInvalidConsultationTypeIdAsync(request.ConsultationTypeCharges, cancellationToken);
        if (invalidConsultationTypeId.HasValue)
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidReference, $"Consultation type '{invalidConsultationTypeId}' was not found.");
        }

        consultant.Update(
            request.Name,
            request.DepartmentId,
            request.Specialization,
            request.IsActive,
            request.Priority,
            request.AvailableDays,
            request.VisitStartTime,
            request.VisitEndTime,
            ToSelections(request.ConsultationTypeCharges),
            actorId,
            request.VisitStartTime2,
            request.VisitEndTime2);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<ConsultantResponse>.Success(consultant.ToResponse());
    }

    public async Task<Result<ConsultantResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var consultant = await _repository.GetByIdAsync(id, cancellationToken);
        return consultant is null
            ? Result<ConsultantResponse>.Failure(MastersErrorCodes.NotFound, $"Consultant '{id}' was not found.")
            : Result<ConsultantResponse>.Success(consultant.ToResponse());
    }

    public async Task<PagedResult<ConsultantResponse>> GetPagedAsync(ConsultantListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<ConsultantResponse>(items.Select(c => c.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultant = await _repository.GetByIdAsync(id, cancellationToken);
        if (consultant is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Consultant '{id}' was not found.");
        }

        consultant.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // Validation order is cheapest-first: extension/content-type/size are free (already on the
    // request), so they run before the magic-bytes check, which has to read the stream. File
    // save happens only after every check passes and strictly before SetPhoto/SaveChangesAsync
    // — mirrors UserService.UploadProfilePhotoAsync's identical reasoning.
    public async Task<Result<ConsultantResponse>> UploadPhotoAsync(
        Guid id, Stream content, string fileName, string contentType, long length, Guid? actorId, CancellationToken cancellationToken)
    {
        var consultant = await _repository.GetByIdAsync(id, cancellationToken);
        if (consultant is null)
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.NotFound, $"Consultant '{id}' was not found.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedPhotoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
            !AllowedPhotoContentTypes.Contains(contentType))
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidFile, "Only JPG and PNG files are allowed.");
        }

        if (length > MaxPhotoSizeBytes)
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidFile, "Photo must not exceed 2MB.");
        }

        if (!await LooksLikeAnAllowedImageAsync(content, cancellationToken))
        {
            return Result<ConsultantResponse>.Failure(MastersErrorCodes.InvalidFile, "The uploaded file is not a valid image.");
        }

        var previousPath = consultant.PhotoUrl;
        var path = await _fileStorage.SavePhotoAsync(id, fileName, content, cancellationToken);
        consultant.SetPhoto(path, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        // Same as UserService.UploadProfilePhotoAsync: only an extension change leaves an old
        // file behind, and the comparison is case-insensitive so the new file is never deleted.
        if (!string.Equals(previousPath, path, StringComparison.OrdinalIgnoreCase))
        {
            await _fileStorage.DeleteAsync(previousPath, cancellationToken);
        }

        return Result<ConsultantResponse>.Success(consultant.ToResponse());
    }

    private async Task<Guid?> FindInvalidConsultationTypeIdAsync(IReadOnlyList<ConsultationTypeChargeDto> consultationTypeCharges, CancellationToken cancellationToken)
    {
        foreach (var consultationTypeId in consultationTypeCharges.Select(c => c.ConsultationTypeId).Distinct())
        {
            if (!await _consultationTypeRepository.ExistsAsync(consultationTypeId, cancellationToken))
            {
                return consultationTypeId;
            }
        }

        return null;
    }

    private static IReadOnlyList<ConsultationTypeSelection> ToSelections(IReadOnlyList<ConsultationTypeChargeDto> consultationTypeCharges)
        => consultationTypeCharges.Select(c => new ConsultationTypeSelection(c.ConsultationTypeId, c.ConsultantCharge)).ToList();

    // A client-supplied extension and Content-Type header are just claims — this checks the
    // file's actual bytes against the well-known signatures for the two allowed formats, so a
    // renamed non-image (e.g. "virus.jpg" containing plain text) is still rejected. Resets the
    // stream position afterward so the full content is still there for SavePhotoAsync. Mirrors
    // UserService.LooksLikeAnAllowedImageAsync, minus the WEBP branch (not one of this feature's
    // allowed formats).
    private static async Task<bool> LooksLikeAnAllowedImageAsync(Stream content, CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var bytesRead = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        content.Position = 0;

        // JPEG: FF D8 FF
        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytesRead >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return true;
        }

        return false;
    }
}
