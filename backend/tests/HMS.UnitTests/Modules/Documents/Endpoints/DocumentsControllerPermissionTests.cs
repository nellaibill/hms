using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Endpoints;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Endpoints;

/// <summary>ADR-130: a patient's documents open with either records-compliance or the
/// matching patient-management permission, and a caller who came in through
/// patient-management alone is scoped to Patient documents.</summary>
public class DocumentsControllerPermissionTests
{
    private readonly IDocumentService _service = Substitute.For<IDocumentService>();
    private readonly DocumentsController _sut;

    public DocumentsControllerPermissionTests()
    {
        var validator = Substitute.For<IValidator<UploadDocumentRequest>>();
        validator.ValidateAsync(Arg.Any<UploadDocumentRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        _sut = new DocumentsController(_service, validator, NullLogger<DocumentsController>.Instance);

        _service.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentResponse>.Success(new DocumentResponse()));
        _service.GetPagedAsync(Arg.Any<DocumentListQuery>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<DocumentResponse>([], 1, 20, 0));
        _service.UploadAsync(Arg.Any<UploadDocumentRequest>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentResponse>.Success(new DocumentResponse { Id = Guid.NewGuid() }));
        _service.DeleteAsync(Arg.Any<Guid>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
    }

    private void SetCaller(string loginType, params string[] permissions)
    {
        var claims = new List<Claim> { new("UserId", Guid.NewGuid().ToString()), new("LoginType", loginType) };
        claims.AddRange(permissions.Select(p => new Claim("Permission", p)));
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) },
        };
    }

    private static IReadOnlyList<string> AnyOfKeys(string actionName) =>
        typeof(DocumentsController).GetMethod(actionName)!
            .GetCustomAttributes(typeof(RequireAnyPermissionAttribute), inherit: false)
            .Cast<RequireAnyPermissionAttribute>()
            .Single()
            .PermissionKeys;

    [Theory]
    [InlineData(nameof(DocumentsController.GetPaged))]
    [InlineData(nameof(DocumentsController.GetById))]
    [InlineData(nameof(DocumentsController.GetContent))]
    public void Reads_OpenWithRecordsOrPatientView(string actionName)
    {
        AnyOfKeys(actionName).Should().BeEquivalentTo("records-compliance.view", "patient-management.view");
    }

    [Fact]
    public void Upload_OpensWithRecordsCreateOrPatientCreateOrEdit()
    {
        // Registration attaches a photo/ID proof right after creating the patient (create);
        // the patient page attaches visit documents later (edit).
        AnyOfKeys(nameof(DocumentsController.Upload))
            .Should().BeEquivalentTo("records-compliance.create", "patient-management.create", "patient-management.edit");
    }

    [Fact]
    public void Delete_OpensWithRecordsOrPatientDelete()
    {
        AnyOfKeys(nameof(DocumentsController.Delete)).Should().BeEquivalentTo("records-compliance.delete", "patient-management.delete");
    }

    [Theory]
    [InlineData(nameof(DocumentsController.GetSummary))]
    [InlineData(nameof(DocumentsController.Search))]
    [InlineData(nameof(DocumentsController.GetChunks))]
    public void RepositoryWideActions_StillNeedRecordsCompliance(string actionName)
    {
        var method = typeof(DocumentsController).GetMethod(actionName)!;

        method.GetCustomAttributes(typeof(RequireAnyPermissionAttribute), inherit: false).Should().BeEmpty();
        method.GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false).Should().ContainSingle();
    }

    [Fact]
    public async Task GetById_ScopesAReceptionistWithOnlyPatientViewToPatientDocuments()
    {
        SetCaller("receptionist", "patient-management.view", "patient-management.edit");

        await _sut.GetById(Guid.NewGuid(), CancellationToken.None);

        await _service.Received(1).GetByIdAsync(
            Arg.Any<Guid>(),
            Arg.Is<DocumentActor>(a => a.OwnerTypeScope != null && a.OwnerTypeScope.SetEquals(new[] { DocumentOwnerType.Patient })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaged_LeavesACallerWithRecordsViewUnscoped()
    {
        SetCaller("receptionist", "records-compliance.view", "patient-management.view");

        await _sut.GetPaged(new DocumentListQuery(), CancellationToken.None);

        await _service.Received(1).GetPagedAsync(
            Arg.Any<DocumentListQuery>(),
            Arg.Is<DocumentActor>(a => a.OwnerTypeScope == null && a.LoginType == "receptionist"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_ScopesACallerWithoutRecordsCreate()
    {
        // records-compliance.view alone doesn't lift the upload scope — the scope follows the
        // permission matching the action.
        SetCaller("receptionist", "records-compliance.view", "patient-management.create");
        var file = new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", "id-proof.png") { Headers = new HeaderDictionary(), ContentType = "image/png" };
        var request = new UploadDocumentRequest { OwnerType = DocumentOwnerType.Patient, OwnerId = Guid.NewGuid(), DocumentType = DocumentType.IdProof };

        var result = await _sut.Upload(file, request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        await _service.Received(1).UploadAsync(
            request,
            Arg.Any<Stream>(),
            "id-proof.png",
            "image/png",
            3,
            Arg.Is<DocumentActor>(a => a.OwnerTypeScope != null && a.OwnerTypeScope.SetEquals(new[] { DocumentOwnerType.Patient })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_LeavesACallerWithRecordsDeleteUnscoped()
    {
        SetCaller("admin", "records-compliance.delete");

        await _sut.Delete(Guid.NewGuid(), CancellationToken.None);

        await _service.Received(1).DeleteAsync(Arg.Any<Guid>(), Arg.Is<DocumentActor>(a => a.OwnerTypeScope == null), Arg.Any<CancellationToken>());
    }
}
