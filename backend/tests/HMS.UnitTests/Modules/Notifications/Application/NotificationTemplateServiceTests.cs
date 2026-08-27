using FluentAssertions;
using HMS.Modules.Notifications.Application;
using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Application;

public class NotificationTemplateServiceTests
{
    private readonly INotificationTemplateRepository _repository = Substitute.For<INotificationTemplateRepository>();
    private readonly NotificationTemplateService _sut;

    public NotificationTemplateServiceTests()
    {
        _sut = new NotificationTemplateService(_repository, NullLogger<NotificationTemplateService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithNewKeyAndChannel_CreatesTemplate()
    {
        _repository
            .GetByKeyAndChannelAsync(Arg.Any<string>(), Arg.Any<NotificationChannel>(), Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);

        var request = new CreateNotificationTemplateRequest
        {
            TemplateKey = "appointment.booked",
            Channel = NotificationChannel.InApp,
            BodyTemplate = "Hello {{PatientName}}",
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TemplateKey.Should().Be("appointment.booked");
        await _repository.Received(1).AddAsync(Arg.Any<NotificationTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithExistingKeyAndChannel_ReturnsDuplicateFailure()
    {
        var existing = NotificationTemplate.Create("appointment.booked", NotificationChannel.InApp, null, "body", null);
        _repository
            .GetByKeyAndChannelAsync("appointment.booked", NotificationChannel.InApp, Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new CreateNotificationTemplateRequest
        {
            TemplateKey = "appointment.booked",
            Channel = NotificationChannel.InApp,
            BodyTemplate = "body",
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(NotificationErrorCodes.DuplicateTemplate);
        await _repository.DidNotReceive().AddAsync(Arg.Any<NotificationTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsTemplateNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((NotificationTemplate?)null);

        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateNotificationTemplateRequest { BodyTemplate = "body", IsActive = true }, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(NotificationErrorCodes.TemplateNotFound);
    }

    [Fact]
    public async Task UpdateAsync_OnEmailTemplateWithoutSubject_ReturnsEmailRequiresSubjectFailure()
    {
        var template = NotificationTemplate.Create("appointment.booked", NotificationChannel.Email, "Original subject", "body", null);
        _repository.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var result = await _sut.UpdateAsync(template.Id, new UpdateNotificationTemplateRequest { Subject = null, BodyTemplate = "new body", IsActive = true }, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(NotificationErrorCodes.EmailTemplateRequiresSubject);
    }

    [Fact]
    public async Task UpdateAsync_WithValidContent_UpdatesAndCanDeactivate()
    {
        var template = NotificationTemplate.Create("appointment.booked", NotificationChannel.InApp, null, "old body", null);
        _repository.GetByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var result = await _sut.UpdateAsync(template.Id, new UpdateNotificationTemplateRequest { BodyTemplate = "new body", IsActive = false }, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BodyTemplate.Should().Be("new body");
        result.Value.IsActive.Should().BeFalse();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_MapsEveryTemplate()
    {
        _repository.GetAllAsync(null, Arg.Any<CancellationToken>()).Returns(
        [
            NotificationTemplate.Create("a", NotificationChannel.InApp, null, "body a", null),
            NotificationTemplate.Create("b", NotificationChannel.Sms, null, "body b", null),
        ]);

        var result = await _sut.GetAllAsync(null, CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
