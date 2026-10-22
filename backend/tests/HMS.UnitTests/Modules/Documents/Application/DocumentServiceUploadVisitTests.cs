using System.Text;
using FluentAssertions;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Application.Abstractions;
using HMS.Modules.Documents.Application.Indexing;
using HMS.Modules.Documents.Application.Security;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.Documents.Domain;
using HMS.Shared.Infrastructure.Ai;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Application;

/// <summary>Filing a document under one of its owner's visits (a patient's lab report under the
/// OP/IP visit it came from) — accepted only when the owner's own module confirms the visit.</summary>
public class DocumentServiceUploadVisitTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IDocumentFileStorage _fileStorage = Substitute.For<IDocumentFileStorage>();
    private readonly IDocumentAccessPolicy _accessPolicy = Substitute.For<IDocumentAccessPolicy>();
    private readonly IDocumentOwnerExistenceChecker _patientChecker = Substitute.For<IDocumentOwnerExistenceChecker>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly DocumentService _sut;
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _visitId = Guid.NewGuid();

    public DocumentServiceUploadVisitTests()
    {
        _patientChecker.OwnerType.Returns(DocumentOwnerType.Patient);
        _patientChecker.ExistsAsync(_patientId, Arg.Any<CancellationToken>()).Returns(true);
        _accessPolicy.CanWrite(Arg.Any<DocumentActor>(), Arg.Any<DocumentOwnerType>()).Returns(true);
        _fileStorage.SaveAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(new SavedFile("documents/report.pdf", "checksum"));
        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _tenantContext.ConnectionString.Returns("Host=localhost");

        _sut = new DocumentService(
            _repository,
            Substitute.For<IDocumentChunkRepository>(),
            Substitute.For<IDocumentIndexer>(),
            Substitute.For<IDocumentChunkEmbedder>(),
            Substitute.For<IAiEmbeddingProvider>(),
            _fileStorage,
            _accessPolicy,
            Substitute.For<IDocumentScanQueue>(),
            [_patientChecker],
            _tenantContext,
            new ConfigurationBuilder().Build(),
            NullLogger<DocumentService>.Instance);
    }

    private Task<Result<DocumentResponse>> UploadAsync(DocumentOwnerType ownerType, Guid ownerId, Guid? visitId)
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.4 lab report");
        var request = new UploadDocumentRequest { OwnerType = ownerType, OwnerId = ownerId, DocumentType = DocumentType.Report, VisitId = visitId };
        return _sut.UploadAsync(request, new MemoryStream(pdf), "report.pdf", "application/pdf", pdf.Length, new DocumentActor(Guid.NewGuid(), "Staff"), CancellationToken.None);
    }

    [Fact]
    public async Task UploadAsync_UnderOneOfThePatientsVisits_SavesTheDocumentWithThatVisit()
    {
        _patientChecker.HasVisitAsync(_patientId, _visitId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await UploadAsync(DocumentOwnerType.Patient, _patientId, _visitId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitId.Should().Be(_visitId);
        await _repository.Received(1).AddAsync(Arg.Is<Document>(d => d.VisitId == _visitId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_UnderAVisitThatIsNotThePatients_FailsAndStoresNothing()
    {
        _patientChecker.HasVisitAsync(_patientId, _visitId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await UploadAsync(DocumentOwnerType.Patient, _patientId, _visitId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DocumentErrorCodes.InvalidVisit);
        await _fileStorage.DidNotReceiveWithAnyArgs().SaveAsync(default, default!, default!, default);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // No module confirms visits for this owner type, so a visit can't be given for it.
    [Fact]
    public async Task UploadAsync_WithAVisitForAnOwnerTypeWithNoChecker_Fails()
    {
        var result = await UploadAsync(DocumentOwnerType.Vendor, Guid.NewGuid(), _visitId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(DocumentErrorCodes.InvalidVisit);
    }

    [Fact]
    public async Task UploadAsync_WithNoVisit_SavesTheDocumentWithoutOne()
    {
        var result = await UploadAsync(DocumentOwnerType.Patient, _patientId, visitId: null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.VisitId.Should().BeNull();
        await _patientChecker.DidNotReceiveWithAnyArgs().HasVisitAsync(default, default, default);
    }
}
