using FluentAssertions;
using HMS.Modules.ActivityLog.Application;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Application.Abstractions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Application;

public class PatientFieldSettingsServiceTests
{
    private readonly IPatientFieldSettingsRepository _repository = Substitute.For<IPatientFieldSettingsRepository>();
    private readonly IActivityLogService _activityLog = Substitute.For<IActivityLogService>();

    private PatientFieldSettingsService NewService() => new(_repository, _activityLog, NullLogger<PatientFieldSettingsService>.Instance);

    [Fact]
    public async Task GetAsync_BeforeAnythingIsSaved_ReturnsTodaysBehaviour()
    {
        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns((PatientFieldSettings?)null);

        var requirements = await NewService().GetAsync(CancellationToken.None);

        requirements.Should().BeEquivalentTo(PatientFieldRequirements.Default);
        requirements.LastName.Should().BeTrue();
        requirements.BloodGroup.Should().BeFalse("the form has always started Blood group on Unknown");
    }

    [Fact]
    public async Task UpdateAsync_ThenGetAsync_ReturnsWhatWasSaved_AndAFreshScopeReadsItBack()
    {
        PatientFieldSettings? saved = null;
        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns(_ => saved);
        await _repository.AddAsync(Arg.Do<PatientFieldSettings>(s => saved = s), Arg.Any<CancellationToken>());
        var choice = new PatientFieldRequirements { LastName = false, Pincode = false, EmergencyContact = false };

        var service = NewService();
        var result = await service.UpdateAsync(choice, actorId: Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await service.GetAsync(CancellationToken.None)).Should().BeEquivalentTo(choice);
        (await NewService().GetAsync(CancellationToken.None)).Should().BeEquivalentTo(choice);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WhenTheStoredRowNoLongerParses_FallsBackToTheDefaults()
    {
        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns(PatientFieldSettings.Create("{not json", createdBy: null));

        var requirements = await NewService().GetAsync(CancellationToken.None);

        requirements.Should().BeEquivalentTo(PatientFieldRequirements.Default);
    }

    [Fact]
    public async Task GetAsync_ReadsTheRowOncePerScope()
    {
        _repository.GetAsync(Arg.Any<CancellationToken>()).Returns((PatientFieldSettings?)null);
        var service = NewService();

        await service.GetAsync(CancellationToken.None);
        await service.GetAsync(CancellationToken.None);

        await _repository.Received(1).GetAsync(Arg.Any<CancellationToken>());
    }
}
