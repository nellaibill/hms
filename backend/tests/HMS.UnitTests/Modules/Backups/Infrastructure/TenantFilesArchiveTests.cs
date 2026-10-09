using System.IO.Compression;
using FluentAssertions;
using HMS.Modules.Backups.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Backups.Infrastructure;

public class TenantFilesArchiveTests : IDisposable
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    private readonly string _contentRoot;
    private readonly TenantFilesArchive _sut;

    public TenantFilesArchiveTests()
    {
        _contentRoot = Path.Combine(Path.GetTempPath(), "hms-files-archive-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_contentRoot);
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_contentRoot);
        _sut = new TenantFilesArchive(environment, NullLogger<TenantFilesArchive>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GetSummaryAsync_ListsEveryKindWithZeroCounts_WhenNothingHasBeenUploadedYet()
    {
        var summary = await _sut.GetSummaryAsync(TenantA, CancellationToken.None);

        summary.Categories.Select(category => category.Key).Should().Equal(
            "documents", "consultant-photos", "user-photos", "product-images", "branding");
        summary.FileCount.Should().Be(0);
        summary.TotalSizeBytes.Should().Be(0);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsOnlyTheRequestedTenantsFiles()
    {
        WriteFile($"App_Data/Tenant/{TenantA}/documents/doc-1.pdf", "12345");
        WriteFile($"App_Data/Tenant/{TenantA}/documents/doc-2.pdf", "123");
        WriteFile($"wwwroot/uploads/Tenant/{TenantA}/products/{Guid.NewGuid()}/images/p.jpg", "1234567");
        WriteFile($"App_Data/Tenant/{TenantB}/documents/someone-elses.pdf", "should-not-count");
        WriteFile($"wwwroot/uploads/Tenant/{TenantB}/users/someone-elses.png", "should-not-count");

        var summary = await _sut.GetSummaryAsync(TenantA, CancellationToken.None);

        summary.FileCount.Should().Be(3);
        summary.TotalSizeBytes.Should().Be(5 + 3 + 7);
        var documents = summary.Categories.Single(category => category.Key == "documents");
        documents.FileCount.Should().Be(2);
        documents.SizeBytes.Should().Be(8);
        summary.Categories.Single(category => category.Key == "product-images").FileCount.Should().Be(1);
        summary.Categories.Single(category => category.Key == "user-photos").FileCount.Should().Be(0);
    }

    [Fact]
    public async Task WriteZipAsync_PutsEachKindUnderItsOwnFolder_WithoutTheTenantIdSegment()
    {
        var productId = Guid.NewGuid();
        WriteFile($"App_Data/Tenant/{TenantA}/documents/doc-1.pdf", "document-bytes");
        WriteFile($"wwwroot/uploads/Tenant/{TenantA}/consultants/c.jpg", "consultant-bytes");
        WriteFile($"wwwroot/uploads/Tenant/{TenantA}/users/u.png", "user-bytes");
        WriteFile($"wwwroot/uploads/Tenant/{TenantA}/products/{productId}/images/p.jpg", "product-bytes");
        WriteFile($"wwwroot/uploads/Tenant/{TenantA}/branding/primary/l.png", "logo-bytes");

        using var zipStream = new MemoryStream();
        var written = await _sut.WriteZipAsync(TenantA, zipStream, CancellationToken.None);

        written.Should().Be(5);
        var entries = ReadEntries(zipStream);
        entries.Should().BeEquivalentTo(new Dictionary<string, string>
        {
            ["documents/doc-1.pdf"] = "document-bytes",
            ["consultant-photos/c.jpg"] = "consultant-bytes",
            ["user-photos/u.png"] = "user-bytes",
            [$"product-images/{productId}/images/p.jpg"] = "product-bytes",
            ["branding/primary/l.png"] = "logo-bytes",
        });
    }

    [Fact]
    public async Task WriteZipAsync_NeverIncludesAnotherTenantsFiles()
    {
        WriteFile($"App_Data/Tenant/{TenantA}/documents/mine.pdf", "mine");
        WriteFile($"App_Data/Tenant/{TenantB}/documents/theirs.pdf", "theirs");
        WriteFile($"wwwroot/uploads/Tenant/{TenantB}/branding/primary/theirs.png", "theirs");

        using var zipStream = new MemoryStream();
        var written = await _sut.WriteZipAsync(TenantA, zipStream, CancellationToken.None);

        written.Should().Be(1);
        ReadEntries(zipStream).Keys.Should().Equal("documents/mine.pdf");
    }

    [Fact]
    public async Task WriteZipAsync_WritesAValidEmptyZipAndReturnsZero_WhenTheTenantHasNoFiles()
    {
        using var zipStream = new MemoryStream();
        var written = await _sut.WriteZipAsync(TenantA, zipStream, CancellationToken.None);

        written.Should().Be(0);
        ReadEntries(zipStream).Should().BeEmpty();
    }

    [Fact]
    public async Task WriteZipAsync_StoresAlreadyCompressedFormatsAsIs_AndDeflatesTheRest()
    {
        var repetitive = string.Concat(Enumerable.Repeat("<svg></svg>", 200));
        WriteFile($"wwwroot/uploads/Tenant/{TenantA}/users/photo.PNG", repetitive);
        WriteFile($"App_Data/Tenant/{TenantA}/documents/report.pdf", repetitive);
        WriteFile($"wwwroot/uploads/Tenant/{TenantA}/branding/primary/logo.svg", repetitive);

        using var zipStream = new MemoryStream();
        await _sut.WriteZipAsync(TenantA, zipStream, CancellationToken.None);

        zipStream.Position = 0;
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        archive.GetEntry("user-photos/photo.PNG")!.CompressedLength.Should().Be(repetitive.Length);
        archive.GetEntry("documents/report.pdf")!.CompressedLength.Should().Be(repetitive.Length);
        archive.GetEntry("branding/primary/logo.svg")!.CompressedLength.Should().BeLessThan(repetitive.Length);
    }

    [Fact]
    public async Task WriteZipAsync_LeavesTheDestinationStreamOpen()
    {
        WriteFile($"App_Data/Tenant/{TenantA}/documents/doc.pdf", "x");

        using var zipStream = new MemoryStream();
        await _sut.WriteZipAsync(TenantA, zipStream, CancellationToken.None);

        // The download endpoint rewinds and streams this same temp-file stream after zipping.
        zipStream.CanRead.Should().BeTrue();
        zipStream.CanSeek.Should().BeTrue();
    }

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_contentRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private static Dictionary<string, string> ReadEntries(MemoryStream zipStream)
    {
        zipStream.Position = 0;
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        return archive.Entries.ToDictionary(
            entry => entry.FullName,
            entry =>
            {
                using var reader = new StreamReader(entry.Open());
                return reader.ReadToEnd();
            });
    }
}
