using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;

namespace HMS.Modules.Documents.Application.Mapping;

/// <summary>
/// Manual entity-to-DTO mapping — mirrors
/// HMS.Modules.Patients.Application.Mapping.PatientMappingExtensions; two entities don't yet
/// justify a mapping library (see docs/DecisionLog.md ADR-003).
/// </summary>
internal static class DocumentMappingExtensions
{
    public static DocumentResponse ToResponse(this Document document) => new()
    {
        Id = document.Id,
        OwnerType = document.OwnerType,
        OwnerId = document.OwnerId,
        DocumentType = document.DocumentType,
        Classification = document.Classification,
        OriginalFileName = document.OriginalFileName,
        ContentType = document.ContentType,
        SizeBytes = document.SizeBytes,
        Status = document.Status,
        IsArchived = document.IsArchived,
        UploadedByUserId = document.UploadedByUserId,
        ExpiryDate = document.ExpiryDate,
        CreatedAt = document.CreatedAt,
        UpdatedAt = document.UpdatedAt,
    };
}
