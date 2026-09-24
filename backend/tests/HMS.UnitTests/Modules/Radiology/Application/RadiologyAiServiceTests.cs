using FluentAssertions;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Radiology.Application;
using HMS.Modules.Radiology.Application.Abstractions;
using HMS.Modules.Radiology.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Radiology.Application;

public class RadiologyAiServiceTests
{
    private static readonly DocumentActor Doctor = new(Guid.NewGuid(), "doctor");

    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _documentId = Guid.NewGuid();

    private readonly IDocumentService _documents = Substitute.For<IDocumentService>();
    private readonly IXrayImageAnalyzer _analyzer = Substitute.For<IXrayImageAnalyzer>();
    private readonly IXrayAiAnalysisRepository _repository = Substitute.For<IXrayAiAnalysisRepository>();
    private readonly RadiologyAiService _sut;

    public RadiologyAiServiceTests()
    {
        _sut = new RadiologyAiService(_documents, _analyzer, _repository, NullLogger<RadiologyAiService>.Instance);
        GivenDocumentRecord(DocumentOwnerType.Patient);
    }

    private void GivenDocumentRecord(DocumentOwnerType ownerType)
    {
        _documents
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentResponse>.Success(new DocumentResponse { Id = _documentId, OwnerType = ownerType, OwnerId = _patientId }));
    }

    private void GivenDocumentContent(byte[] bytes, string contentType)
    {
        _documents
            .GetContentAsync(Arg.Any<Guid>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(_ => Result<DocumentContent>.Success(new DocumentContent(new MemoryStream(bytes), contentType, "xray.png")));
    }

    private void GivenAnalyzerReplies(string text = "ANATOMICAL REGION:\nWrist", string model = "google/gemma-4-26B-A4B-it")
    {
        _analyzer
            .AnalyzeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<XrayImageAnalysis>.Success(new XrayImageAnalysis(text, model)));
    }

    private async Task AssertNothingSavedAsync()
    {
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task AnalyzeDocument_PassesDocumentLookupFailureThrough_WithoutCallingTheModel()
    {
        _documents
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentResponse>.Failure("DOCUMENTS.DOCUMENT_NOT_FOUND", "not found"));

        var result = await _sut.AnalyzeDocumentAsync(_documentId, Doctor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.DocumentNotFound);
        await _analyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, default);
        await AssertNothingSavedAsync();
    }

    [Theory]
    [InlineData(DocumentOwnerType.Staff)]
    [InlineData(DocumentOwnerType.Billing)]
    public async Task AnalyzeDocument_RejectsDocumentsNotOwnedByAPatient(DocumentOwnerType ownerType)
    {
        GivenDocumentRecord(ownerType);

        var result = await _sut.AnalyzeDocumentAsync(_documentId, Doctor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.NotPatientDocument);
        await _analyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, default);
        await AssertNothingSavedAsync();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/dicom")]
    [InlineData("text/plain")]
    public async Task AnalyzeDocument_RejectsNonImageContentTypes(string contentType)
    {
        GivenDocumentContent([1, 2, 3], contentType);

        var result = await _sut.AnalyzeDocumentAsync(_documentId, Doctor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.UnsupportedImage);
        await _analyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, default);
        await AssertNothingSavedAsync();
    }

    [Fact]
    public async Task AnalyzeDocument_RejectsImagesOverTheSizeCap()
    {
        GivenDocumentContent(new byte[RadiologyAiService.MaxImageBytes + 1], "image/png");

        var result = await _sut.AnalyzeDocumentAsync(_documentId, Doctor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.ImageTooLarge);
        await _analyzer.DidNotReceiveWithAnyArgs().AnalyzeAsync(default!, default!, default);
        await AssertNothingSavedAsync();
    }

    [Fact]
    public async Task AnalyzeDocument_SendsOnlyImageBytes_AndSavesTheDraftAgainstThePatient()
    {
        byte[] bytes = [9, 8, 7];
        GivenDocumentContent(bytes, "image/png");
        GivenAnalyzerReplies();

        var result = await _sut.AnalyzeDocumentAsync(_documentId, Doctor, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PatientId.Should().Be(_patientId);
        result.Value.DocumentId.Should().Be(_documentId);
        result.Value.Analysis.Should().StartWith("ANATOMICAL REGION:");
        result.Value.Model.Should().Be("google/gemma-4-26B-A4B-it");
        result.Value.IsReviewed.Should().BeFalse();
        result.Value.GeneratedByUserId.Should().Be(Doctor.UserId);
        result.Value.Disclaimer.Should().Be(XrayAnalysisPrompt.Disclaimer);

        await _analyzer.Received(1).AnalyzeAsync(Arg.Is<byte[]>(b => b.SequenceEqual(bytes)), "image/png", Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(
            Arg.Is<XrayAiAnalysis>(a => a.PatientId == _patientId && a.DocumentId == _documentId && !a.IsReviewed),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeDocument_PassesAnalyzerFailureThrough_AndSavesNothing()
    {
        GivenDocumentContent([1], "image/jpeg");
        _analyzer
            .AnalyzeAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<XrayImageAnalysis>.Failure(RadiologyAiErrorCodes.NotConfigured, "not configured"));

        var result = await _sut.AnalyzeDocumentAsync(_documentId, Doctor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.NotConfigured);
        await AssertNothingSavedAsync();
    }

    [Fact]
    public async Task GetPatientAnalyses_OnlyQueriesTheDocumentsTheCallerCanSee()
    {
        var visibleDocumentId = Guid.NewGuid();
        _documents
            .GetPagedAsync(Arg.Any<DocumentListQuery>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<DocumentResponse>([new DocumentResponse { Id = visibleDocumentId }], 1, 100, 1));
        var saved = XrayAiAnalysis.Create(_patientId, visibleDocumentId, "ANATOMICAL REGION:\nKnee", "m", Doctor.UserId);
        _repository
            .ListByDocumentIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([saved]);

        var result = await _sut.GetPatientAnalysesAsync(_patientId, Doctor, CancellationToken.None);

        result.Value!.Should().ContainSingle().Which.Id.Should().Be(saved.Id);
        await _documents.Received(1).GetPagedAsync(
            Arg.Is<DocumentListQuery>(q => q.OwnerType == DocumentOwnerType.Patient && q.OwnerId == _patientId),
            Doctor,
            Arg.Any<CancellationToken>());
        await _repository.Received(1).ListByDocumentIdsAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == visibleDocumentId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPatientAnalyses_WhenCallerSeesNoImages_ReturnsEmptyWithoutQueryingAnalyses()
    {
        _documents
            .GetPagedAsync(Arg.Any<DocumentListQuery>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<DocumentResponse>([], 1, 100, 0));

        var result = await _sut.GetPatientAnalysesAsync(_patientId, Doctor, CancellationToken.None);

        result.Value.Should().BeEmpty();
        await _repository.DidNotReceiveWithAnyArgs().ListByDocumentIdsAsync(default!, default);
    }

    [Theory]
    [InlineData("nurse")]
    [InlineData("receptionist")]
    [InlineData(null)]
    public async Task MarkReviewed_RejectsCallersWhoAreNotDoctorsOrRadiologists(string? loginType)
    {
        var result = await _sut.MarkReviewedAsync(Guid.NewGuid(), new DocumentActor(Guid.NewGuid(), loginType), CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.ReviewForbidden);
        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task MarkReviewed_WhenAnalysisIsMissing_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((XrayAiAnalysis?)null);

        var result = await _sut.MarkReviewedAsync(Guid.NewGuid(), Doctor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.AnalysisNotFound);
    }

    [Fact]
    public async Task MarkReviewed_WhenCallerCannotSeeTheImage_ReturnsNotFound_AndDoesNotSave()
    {
        var analysis = XrayAiAnalysis.Create(_patientId, _documentId, "x", "m", null);
        _repository.GetByIdAsync(analysis.Id, Arg.Any<CancellationToken>()).Returns(analysis);
        _documents
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<DocumentActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<DocumentResponse>.Failure("DOCUMENTS.DOCUMENT_NOT_FOUND", "nope"));

        var result = await _sut.MarkReviewedAsync(analysis.Id, Doctor, CancellationToken.None);

        result.ErrorCode.Should().Be(RadiologyAiErrorCodes.AnalysisNotFound);
        analysis.IsReviewed.Should().BeFalse();
        await _repository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData("doctor")]
    [InlineData("radiologist")]
    [InlineData("admin")]
    [InlineData("superAdmin")]
    public async Task MarkReviewed_RecordsTheReviewer(string loginType)
    {
        var reviewer = new DocumentActor(Guid.NewGuid(), loginType);
        var analysis = XrayAiAnalysis.Create(_patientId, _documentId, "x", "m", null);
        _repository.GetByIdAsync(analysis.Id, Arg.Any<CancellationToken>()).Returns(analysis);

        var result = await _sut.MarkReviewedAsync(analysis.Id, reviewer, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsReviewed.Should().BeTrue();
        result.Value.ReviewedByUserId.Should().Be(reviewer.UserId);
        result.Value.ReviewedAtUtc.Should().NotBeNull();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void XrayAiAnalysis_MarkReviewed_IsIdempotent_AndKeepsTheFirstReviewer()
    {
        var analysis = XrayAiAnalysis.Create(_patientId, _documentId, "x", "m", null);
        var first = Guid.NewGuid();

        analysis.MarkReviewed(first);
        var firstReviewedAt = analysis.ReviewedAtUtc;
        analysis.MarkReviewed(Guid.NewGuid());

        analysis.ReviewedByUserId.Should().Be(first);
        analysis.ReviewedAtUtc.Should().Be(firstReviewedAt);
    }
}
