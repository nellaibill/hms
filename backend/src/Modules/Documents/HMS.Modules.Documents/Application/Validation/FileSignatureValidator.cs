namespace HMS.Modules.Documents.Application.Validation;

/// <summary>
/// Server-side content-sniffing per docs/ApiStandards.md §10 ("every upload validates
/// content type and the actual file signature, not just the filename extension") — the
/// client-declared <c>IFormFile.ContentType</c> is never trusted on its own (it's a textbook
/// OWASP file-upload bypass; see docs/modules/Documents/DocumentManagement.md's security
/// review), so this checks the file's actual leading bytes against the declared type.
///
/// Limitation, stated plainly rather than silently: DOCX/XLSX are both ZIP containers (OOXML),
/// so this only confirms the upload is a well-formed ZIP, not specifically a Word or Excel
/// file — fully distinguishing them requires inspecting the archive's internal entries (e.g.
/// a `word/` vs `xl/` folder), which is a reasonable future enhancement, not an MVP blocker.
/// </summary>
internal static class FileSignatureValidator
{
    private static readonly byte[] PdfSignature = "%PDF"u8.ToArray();
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04]; // DOCX/XLSX (OOXML) are ZIP containers.

    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static async Task<bool> MatchesDeclaredContentTypeAsync(Stream content, string declaredContentType, CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var startPosition = content.CanSeek ? content.Position : 0;

        var read = await content.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        if (content.CanSeek)
        {
            content.Position = startPosition;
        }

        var signature = header.AsSpan(0, read);

        return declaredContentType switch
        {
            "application/pdf" => signature.StartsWith(PdfSignature),
            "image/jpeg" => signature.StartsWith(JpegSignature),
            "image/png" => signature.StartsWith(PngSignature),
            DocxContentType => signature.StartsWith(ZipSignature),
            XlsxContentType => signature.StartsWith(ZipSignature),
            _ => false, // Anything outside the allow-list is rejected outright, per §10.
        };
    }
}
