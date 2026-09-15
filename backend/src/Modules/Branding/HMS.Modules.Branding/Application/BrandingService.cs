using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using HMS.Modules.Branding.Application.Abstractions;
using HMS.Modules.Branding.Application.Mapping;
using HMS.Modules.Branding.Contracts;
using HMS.Modules.Branding.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

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
    private static readonly string[] AllowedIconSizeScales = ["sm", "md", "lg", "xl"];
    private static readonly string[] AllowedLogoExtensions = [".png", ".jpg", ".jpeg", ".svg", ".webp"];
    private const long MaxLogoSizeBytes = 500 * 1024; // 500KB, matching the frontend mock store's limit.
    // Pixel bounds for the header's fixed logo box (a 16px logo already reads fine at that
    // size; nothing in the UI ever needs more than 2000px in either dimension) — also blocks
    // the "tiny file, enormous decoded canvas" decompression-bomb shape of attack, which the
    // byte-size cap above doesn't catch on its own.
    private const int MinLogoDimensionPx = 16;
    private const int MaxLogoDimensionPx = 2000;
    // What gets stored, once validation passes — comfortable headroom above the header's
    // actual display box (today ~128×40px) so the logo still looks sharp on a hi-DPI screen
    // or if that box ever grows, while still cutting a typical full-size upload well below the
    // 500KB cap above. Only ever downscales: an upload already at or under this is stored as-is.
    private const int StoredLogoDimensionPx = 512;

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

        if (!AllowedIconSizeScales.Contains(request.IconSizeScale))
        {
            return Result<BrandingResponse>.Failure(
                BrandingErrorCodes.InvalidIconSizeScale,
                $"'{request.IconSizeScale}' is not a supported icon size scale.");
        }

        var settings = await GetOrCreateAsync(cancellationToken);

        settings.UpdateIdentity(request.HospitalName, request.AppTitle, request.Address, request.PhoneNumber, actorId);
        settings.UpdateTypography(request.FontFamily, request.FontSizeScale, request.IconSizeScale, actorId);
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

        // Buffered once, up front: the filename/extension is never trusted on its own — the
        // actual bytes are either decoded as a real raster image (confirming genuine content
        // and reading its true pixel dimensions) or, for SVG, sanity-checked as markup with no
        // embedded script. Storage then reads from this same buffered copy, so the caller's
        // stream is only consumed once regardless of which check below runs.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var contentError = extension == ".svg" ? ValidateSvg(buffer) : ValidateRasterImage(buffer);
        if (contentError is not null)
        {
            return Result<BrandingResponse>.Failure(BrandingErrorCodes.InvalidFile, contentError);
        }

        buffer.Position = 0;
        // SVG is already resolution-independent (see ValidateSvg's own comment) — only raster
        // formats ever need downscaling. ResizeIfOversized returns null when the upload is
        // already small enough, so a typical already-small logo is stored byte-for-byte as
        // uploaded rather than needlessly re-encoded.
        using var resized = extension == ".svg" ? null : ResizeIfOversized(buffer);
        var toStore = resized ?? buffer;
        toStore.Position = 0;

        var logoPath = await _logoStorage.SaveAsync(fileName, toStore, cancellationToken);

        var settings = await GetOrCreateAsync(cancellationToken);
        settings.UpdateLogo(logoPath, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded new branding logo");

        return Result<BrandingResponse>.Success(settings.ToResponse());
    }

    /// <summary>Decodes the buffer as a real image (rejecting anything that isn't, regardless
    /// of what its extension claimed) and enforces the pixel-dimension bounds the header's
    /// fixed logo box is designed around. Leaves <paramref name="buffer"/> rewound to 0.</summary>
    private static string? ValidateRasterImage(MemoryStream buffer)
    {
        // Image.Identify's own failure behavior (null vs. an exception) varies by what's
        // actually wrong with the content — caught broadly here since every outcome means the
        // same thing to the caller: this isn't a real, decodable image.
        ImageInfo? info;
        try
        {
            info = Image.Identify(buffer);
        }
        catch
        {
            info = null;
        }
        finally
        {
            buffer.Position = 0;
        }

        if (info is null)
        {
            return "The uploaded file is not a valid image.";
        }

        if (info.Width < MinLogoDimensionPx || info.Height < MinLogoDimensionPx)
        {
            return $"Logo must be at least {MinLogoDimensionPx}×{MinLogoDimensionPx} pixels.";
        }

        if (info.Width > MaxLogoDimensionPx || info.Height > MaxLogoDimensionPx)
        {
            return $"Logo must be {MaxLogoDimensionPx}×{MaxLogoDimensionPx} pixels or smaller.";
        }

        return null;
    }

    /// <summary>Downscales a raster logo whose longer edge exceeds StoredLogoDimensionPx,
    /// preserving aspect ratio and the original format (so a transparent PNG stays PNG, etc.).
    /// Returns null when the upload is already small enough — nothing to do, caller stores the
    /// original bytes. Assumes ValidateRasterImage has already confirmed this decodes cleanly.
    /// Leaves <paramref name="buffer"/> rewound to 0 either way.</summary>
    private static MemoryStream? ResizeIfOversized(MemoryStream buffer)
    {
        using var image = Image.Load(buffer);
        buffer.Position = 0;
        var format = image.Metadata.DecodedImageFormat ?? throw new InvalidOperationException("Image format could not be determined after decoding.");

        if (image.Width <= StoredLogoDimensionPx && image.Height <= StoredLogoDimensionPx)
        {
            return null;
        }

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(StoredLogoDimensionPx, StoredLogoDimensionPx),
        }));

        var resized = new MemoryStream();
        image.Save(resized, format);
        return resized;
    }

    /// <summary>SVG is vector, not decodable by an image library — this validates it as real
    /// markup instead, walking the parsed element/attribute tree rather than string-matching a
    /// single literal ("&lt;script"), which a payload using an event-handler attribute
    /// (onload=, onerror=, ...) or a javascript:/data: URI never contained in the first place.
    /// Parsed with DTD processing and external-entity resolution explicitly disabled (SVG has
    /// no legitimate need for either, and allowing them would open an XXE hole on top of the
    /// script-injection risk this method exists to close) — a DOCTYPE or malformed markup fails
    /// closed as "not valid SVG" rather than being silently skipped. Dimension bounds don't
    /// apply: the frontend's fixed logo box already clamps a vector image's rendered size
    /// regardless of whatever intrinsic width/height it declares. Leaves
    /// <paramref name="buffer"/> rewound to 0. Uploaded SVGs are still only ever rendered via
    /// an &lt;img src&gt; (browsers don't execute embedded script for that element), so this is
    /// defense-in-depth against the file being opened by direct navigation, not the only thing
    /// standing between an uploaded SVG and script execution.</summary>
    private static string? ValidateSvg(MemoryStream buffer)
    {
        using var reader = new StreamReader(buffer, leaveOpen: true);
        var text = reader.ReadToEnd();
        buffer.Position = 0;

        if (!text.Contains("<svg", StringComparison.OrdinalIgnoreCase))
        {
            return "The uploaded file is not a valid SVG image.";
        }

        XDocument document;
        try
        {
            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };
            using var stringReader = new StringReader(text);
            using var xmlReader = XmlReader.Create(stringReader, readerSettings);
            document = XDocument.Load(xmlReader, LoadOptions.None);
        }
        catch (XmlException)
        {
            return "The uploaded file is not valid SVG markup.";
        }

        if (document.Root is null)
        {
            return "The uploaded file is not a valid SVG image.";
        }

        foreach (var element in document.Root.DescendantsAndSelf())
        {
            if (element.Name.LocalName.Equals("script", StringComparison.OrdinalIgnoreCase) ||
                element.Name.LocalName.Equals("foreignObject", StringComparison.OrdinalIgnoreCase))
            {
                return "SVG logos containing scripts or embedded HTML are not allowed.";
            }

            foreach (var attribute in element.Attributes())
            {
                if (attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase))
                {
                    return "SVG logos containing event-handler attributes are not allowed.";
                }

                if (attribute.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                {
                    return "SVG logos containing script URIs are not allowed.";
                }
            }
        }

        return null;
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
            BrandingDefaults.IconSizeScale,
            JsonSerializer.Serialize(BrandingDefaults.TokensLight),
            JsonSerializer.Serialize(BrandingDefaults.TokensDark));

        await _repository.AddAsync(settings, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded default branding settings row");

        return settings;
    }
}
