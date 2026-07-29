using System.Text.Json;
using HMS.Modules.Branding.Application.Abstractions;
using HMS.Modules.Branding.Application.Mapping;
using HMS.Modules.Branding.Contracts;
using HMS.Modules.Branding.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Branding.Application;

/// <summary>
/// Orchestrates the Branding use cases against the single settings row (see
/// BrandingSettings.SingletonId) — there is no "not found" failure path here, unlike other
/// modules' by-id lookups, since GetOrCreateAsync always has a row to work with.
/// </summary>
internal class BrandingService : IBrandingService
{
    // Curated set, matching frontend/web/src/features/branding/types.ts's FONT_FAMILIES —
    // keep both in sync if the curated list ever changes.
    private static readonly string[] AllowedFontFamilies = ["Inter", "Roboto", "OpenSans", "Lato", "Poppins"];
    private static readonly string[] AllowedFontSizeScales = ["sm", "md", "lg"];
    private static readonly string[] AllowedLogoExtensions = [".png", ".jpg", ".jpeg", ".svg", ".webp"];
    private const long MaxLogoSizeBytes = 500 * 1024; // 500KB, matching the frontend mock store's limit.

    private readonly IBrandingRepository _repository;
    private readonly IBrandingLogoStorage _logoStorage;
    private readonly ILogger<BrandingService> _logger;

    public BrandingService(IBrandingRepository repository, IBrandingLogoStorage logoStorage, ILogger<BrandingService> logger)
    {
        _repository = repository;
        _logoStorage = logoStorage;
        _logger = logger;
    }

    public async Task<BrandingResponse> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return settings.ToResponse();
    }

    public async Task<Result<BrandingResponse>> UpdateAsync(UpdateBrandingRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!AllowedFontFamilies.Contains(request.FontFamily))
        {
            return Result<BrandingResponse>.Failure(
                BrandingErrorCodes.InvalidFontFamily,
                $"'{request.FontFamily}' is not a supported font family.");
        }

        if (!AllowedFontSizeScales.Contains(request.FontSizeScale))
        {
            return Result<BrandingResponse>.Failure(
                BrandingErrorCodes.InvalidFontSizeScale,
                $"'{request.FontSizeScale}' is not a supported font size scale.");
        }

        var settings = await GetOrCreateAsync(cancellationToken);

        settings.UpdateIdentity(request.HospitalName, request.AppTitle, actorId);
        settings.UpdateTypography(request.FontFamily, request.FontSizeScale, actorId);
        settings.UpdateTokens(
            JsonSerializer.Serialize(request.TokensLight),
            JsonSerializer.Serialize(request.TokensDark),
            actorId);

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated branding settings");

        return Result<BrandingResponse>.Success(settings.ToResponse());
    }

    public async Task<Result<BrandingResponse>> UploadLogoAsync(Stream content, string fileName, long length, Guid? actorId, CancellationToken cancellationToken)
    {
        if (length > MaxLogoSizeBytes)
        {
            return Result<BrandingResponse>.Failure(BrandingErrorCodes.InvalidFile, "Logo must be 500KB or smaller.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedLogoExtensions.Contains(extension))
        {
            return Result<BrandingResponse>.Failure(BrandingErrorCodes.InvalidFile, "Logo must be a PNG, JPG, WEBP, or SVG image.");
        }

        var logoPath = await _logoStorage.SaveAsync(fileName, content, cancellationToken);

        var settings = await GetOrCreateAsync(cancellationToken);
        settings.UpdateLogo(logoPath, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded new branding logo");

        return Result<BrandingResponse>.Success(settings.ToResponse());
    }

    /// <summary>
    /// Defense in depth alongside idempotent creation-on-read: a fresh database (or one
    /// where the row was somehow deleted) still gets a working default row on the very
    /// next read, matching the app's pre-feature static config defaults exactly.
    /// </summary>
    private async Task<BrandingSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await _repository.GetAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = BrandingSettings.CreateDefault(
            BrandingDefaults.HospitalName,
            BrandingDefaults.AppTitle,
            BrandingDefaults.FontFamily,
            BrandingDefaults.FontSizeScale,
            JsonSerializer.Serialize(BrandingDefaults.TokensLight),
            JsonSerializer.Serialize(BrandingDefaults.TokensDark));

        await _repository.AddAsync(settings, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded default branding settings row");

        return settings;
    }
}
