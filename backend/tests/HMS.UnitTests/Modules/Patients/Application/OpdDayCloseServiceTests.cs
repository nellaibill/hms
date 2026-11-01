using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.ActivityLog.Contracts;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application;

public class OpdDayCloseServiceTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid ConsultantId = Guid.NewGuid();

    // 3:00 AM IST on 2 Nov 2026 — the scheduled run. The hospital day began at 18:30 UTC on 1 Nov.
    private static readonly DateTime NowUtc = new(2026, 11, 1, 21, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime StartOfTodayUtc = new(2026, 11, 1, 18, 30, 0, DateTimeKind.Utc);

    private readonly IPatientVisitRepository _repository = Substitute.For<IPatientVisitRepository>();
    private readonly IActivityLogService _activityLog = Substitute.For<IActivityLogService>();
    private readonly OpdDayCloseService _sut;

    public OpdDayCloseServiceTests()
    {
        _sut = new OpdDayCloseService(_repository, _activityLog);
    }

    private static PatientVisit NewOpVisit(params DateTime[] appointmentTimes)
    {
        var visit = PatientVisit.Create(PatientId, VisitType.OP, appointmentTypeId: null, createdBy: null);
        foreach (var appointmentTime in appointmentTimes)
        {
            visit.AddConsultation(PatientVisitConsultation.Create(visit.Id, DepartmentId, ConsultantId, consultationTypeId: null, appointmentTime), updatedBy: null);
        }

        return visit;
    }

    private void RepositoryReturnsOnce(params PatientVisit[] visits)
        => _repository.GetOpVisitsWithOpenConsultationsBeforeAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(visits, Array.Empty<PatientVisit>());

    [Fact]
    public void StartOfHospitalDayUtc_IsMidnightIndiaTime()
    {
        OpdDayCloseService.StartOfHospitalDayUtc(NowUtc).Should().Be(StartOfTodayUtc);

        // 23:59 IST on 1 Nov still belongs to 1 Nov.
        OpdDayCloseService.StartOfHospitalDayUtc(new DateTime(2026, 11, 1, 18, 29, 0, DateTimeKind.Utc))
            .Should().Be(new DateTime(2026, 10, 31, 18, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task AutoCompleteStaleConsultationsAsync_AsksOnlyForLinesBeforeTodayInIndiaTime()
    {
        RepositoryReturnsOnce();

        await _sut.AutoCompleteStaleConsultationsAsync(NowUtc, CancellationToken.None);

        await _repository.Received().GetOpVisitsWithOpenConsultationsBeforeAsync(StartOfTodayUtc, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoCompleteStaleConsultationsAsync_ClosesYesterdaysOpenLinesAndRemembersTheirStatus()
    {
        var visit = NewOpVisit(
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),   // 3:30 PM IST, 1 Nov
            new DateTime(2026, 11, 1, 18, 0, 0, DateTimeKind.Utc));  // 11:30 PM IST, 1 Nov
        var waiting = visit.Consultations.First();
        var inConsultation = visit.Consultations.Last();
        inConsultation.StartConsultation();
        RepositoryReturnsOnce(visit);

        var closed = await _sut.AutoCompleteStaleConsultationsAsync(NowUtc, CancellationToken.None);

        closed.Should().Be(2);
        waiting.Status.Should().Be(OpdConsultationStatus.AutoCompleted);
        waiting.AutoCompletedFrom.Should().Be(OpdConsultationStatus.Waiting);
        waiting.AutoCompletedAt.Should().Be(NowUtc);
        inConsultation.Status.Should().Be(OpdConsultationStatus.AutoCompleted);
        inConsultation.AutoCompletedFrom.Should().Be(OpdConsultationStatus.InConsultation);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoCompleteStaleConsultationsAsync_LeavesTodaysLineOnTheSameVisitOpen()
    {
        // 19:00 UTC on 1 Nov is 12:30 AM IST on 2 Nov — the same UTC date as the stale line, but today.
        var visit = NewOpVisit(
            new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 1, 19, 0, 0, DateTimeKind.Utc));
        var today = visit.Consultations.Last();
        RepositoryReturnsOnce(visit);

        var closed = await _sut.AutoCompleteStaleConsultationsAsync(NowUtc, CancellationToken.None);

        closed.Should().Be(1);
        today.Status.Should().Be(OpdConsultationStatus.Waiting);
        today.AutoCompletedFrom.Should().BeNull();
    }

    [Fact]
    public async Task AutoCompleteStaleConsultationsAsync_LeavesFinishedLinesAlone()
    {
        var visit = NewOpVisit(
            new DateTime(2026, 10, 30, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 30, 11, 0, 0, DateTimeKind.Utc));
        var completed = visit.Consultations.First();
        completed.StartConsultation();
        completed.Complete();
        var stillWaiting = visit.Consultations.Last();
        RepositoryReturnsOnce(visit);

        await _sut.AutoCompleteStaleConsultationsAsync(NowUtc, CancellationToken.None);

        completed.Status.Should().Be(OpdConsultationStatus.Completed);
        completed.AutoCompletedFrom.Should().BeNull();
        stillWaiting.Status.Should().Be(OpdConsultationStatus.AutoCompleted);
    }

    [Fact]
    public async Task AutoCompleteStaleConsultationsAsync_KeepsFetchingBatchesUntilNoneAreLeft()
    {
        var first = NewOpVisit(new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc));
        var second = NewOpVisit(new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc));
        _repository.GetOpVisitsWithOpenConsultationsBeforeAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([first], [second], Array.Empty<PatientVisit>());

        var closed = await _sut.AutoCompleteStaleConsultationsAsync(NowUtc, CancellationToken.None);

        closed.Should().Be(2);
        await _repository.Received(3).GetOpVisitsWithOpenConsultationsBeforeAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoCompleteStaleConsultationsAsync_NothingStale_DoesNotSaveOrLog()
    {
        RepositoryReturnsOnce();

        var closed = await _sut.AutoCompleteStaleConsultationsAsync(NowUtc, CancellationToken.None);

        closed.Should().Be(0);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _activityLog.DidNotReceive().LogAsync(Arg.Any<ActivityLogRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoCompleteStaleConsultationsAsync_LogsEachClosedLineAsTheSystem()
    {
        var visit = NewOpVisit(new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc));
        RepositoryReturnsOnce(visit);

        await _sut.AutoCompleteStaleConsultationsAsync(NowUtc, CancellationToken.None);

        await _activityLog.Received(1).LogAsync(
            Arg.Is<ActivityLogRequest>(r =>
                r.EntityType == "PatientVisit"
                && r.EntityId == visit.Id.ToString()
                && r.UserId == null
                && r.Description.Contains("was Waiting")),
            Arg.Any<CancellationToken>());
    }
}
