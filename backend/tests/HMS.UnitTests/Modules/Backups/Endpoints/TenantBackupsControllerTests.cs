using FluentAssertions;
using HMS.Modules.Backups.Application.Abstractions;
using HMS.Modules.Backups.Contracts;
using HMS.Modules.Backups.Endpoints;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Backups.Endpoints;

public class TenantBackupsControllerTests
{
    private static readonly Guid CallerTenantId = Guid.NewGuid();

    private readonly ITenantFilesArchive _filesArchive = Substitute.For<ITenantFilesArchive>();
    private readonly TenantBackupsController _sut;

    public TenantBackupsControllerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(CallerTenantId);
        _sut = new TenantBackupsController(Substitute.For<IBackupStorage>(), _filesArchive, tenantContext)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    [Fact]
    public async Task GetMyFiles_ReturnsTheCallersOwnTenantSummary()
    {
        _filesArchive.GetSummaryAsync(CallerTenantId, Arg.Any<CancellationToken>()).Returns(new TenantFilesSummary(
        [
            new TenantFileCategory("documents", "Documents", 2, 300),
            new TenantFileCategory("branding", "Branding", 1, 50),
        ]));

        var result = await _sut.GetMyFiles(CancellationToken.None);

        var response = result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<ApiResponse<TenantFilesSummaryResponse>>().Subject.Data!;
        response.FileCount.Should().Be(3);
        response.TotalSizeBytes.Should().Be(350);
        response.Categories.Select(category => category.Key).Should().Equal("documents", "branding");
    }

    [Fact]
    public async Task DownloadMyFiles_ZipsTheCallersOwnTenant_AndStreamsItFromTheStart()
    {
        var zipBytes = "fake-zip-bytes"u8.ToArray();
        _filesArchive.WriteZipAsync(CallerTenantId, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<Stream>().Write(zipBytes);
                return 4;
            });

        var result = await _sut.DownloadMyFiles(CancellationToken.None);

        var file = result.Should().BeOfType<FileStreamResult>().Subject;
        await using var stream = file.FileStream;
        file.ContentType.Should().Be("application/zip");
        file.FileDownloadName.Should().StartWith("documents-and-images-").And.EndWith(".zip");
        stream.Position.Should().Be(0);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        copy.ToArray().Should().Equal(zipBytes);
    }

    [Fact]
    public async Task DownloadMyFiles_Returns404_WhenTheTenantHasNoFilesYet()
    {
        _filesArchive.WriteZipAsync(CallerTenantId, Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(0);

        var result = await _sut.DownloadMyFiles(CancellationToken.None);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeOfType<ApiErrorResponse>().Which.ErrorCode.Should().Be("BACKUPS.NO_FILES");
    }
}
