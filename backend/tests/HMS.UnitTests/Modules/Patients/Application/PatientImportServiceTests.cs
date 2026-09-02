using FluentAssertions;
using HMS.Modules.Masters.Application;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application;

public class PatientImportServiceTests
{
    private readonly IPatientImportRepository _repository = Substitute.For<IPatientImportRepository>();
    private readonly IPatientImportQueue _queue = Substitute.For<IPatientImportQueue>();
    private readonly IStateService _stateService = Substitute.For<IStateService>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly PatientImportService _sut;

    public PatientImportServiceTests()
    {
        _sut = new PatientImportService(_repository, _queue, _stateService, _tenantContext, NullLogger<PatientImportService>.Instance);

        _tenantContext.IsResolved.Returns(true);
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _tenantContext.ConnectionString.Returns("Host=localhost;Database=test");
    }

    [Fact]
    public async Task UploadAsync_WithEmptyFile_ReturnsFailure_AndDoesNotEnqueue()
    {
        var result = await _sut.UploadAsync("patients.xlsx", [], uploadedBy: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.ImportFileInvalid);
        await _queue.DidNotReceive().EnqueueValidationAsync(Arg.Any<PatientImportValidationQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WithNonEmptyFile_CreatesBatchAndEnqueuesValidation()
    {
        var result = await _sut.UploadAsync("patients.xlsx", [1, 2, 3], uploadedBy: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ImportBatchStatus.Validating);
        await _repository.Received(1).AddBatchAsync(Arg.Any<PatientImportBatch>(), Arg.Any<CancellationToken>());
        await _queue.Received(1).EnqueueValidationAsync(Arg.Any<PatientImportValidationQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBatchAsync_WhenBatchDoesNotExist_ReturnsFailure()
    {
        _repository.GetBatchByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PatientImportBatch?)null);

        var result = await _sut.GetBatchAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.ImportBatchNotFound);
    }

    [Fact]
    public async Task CommitAsync_WhenBatchIsStillValidating_ReturnsFailure_AndDoesNotEnqueue()
    {
        var batch = PatientImportBatch.Create("patients.xlsx", uploadedBy: null);
        _repository.GetBatchByIdAsync(batch.Id, Arg.Any<CancellationToken>()).Returns(batch);

        var result = await _sut.CommitAsync(batch.Id, committedBy: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientErrorCodes.ImportBatchNotReady);
        await _queue.DidNotReceive().EnqueueCommitAsync(Arg.Any<PatientImportCommitQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommitAsync_WhenBatchIsReadyForReview_EnqueuesCommit_AndReturnsCommittingStatus()
    {
        var batch = PatientImportBatch.Create("patients.xlsx", uploadedBy: null);
        batch.MarkReadyForReview(totalRows: 10, validRows: 8, invalidRows: 2);
        _repository.GetBatchByIdAsync(batch.Id, Arg.Any<CancellationToken>()).Returns(batch);

        var committedBy = Guid.NewGuid();
        var result = await _sut.CommitAsync(batch.Id, committedBy, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ImportBatchStatus.Committing);
        await _queue.Received(1).EnqueueCommitAsync(
            Arg.Is<PatientImportCommitQueueItem>(i => i.BatchId == batch.Id && i.CommittedBy == committedBy),
            Arg.Any<CancellationToken>());
    }
}
