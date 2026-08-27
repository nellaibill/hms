using FluentAssertions;
using HMS.Modules.Notifications.Application;
using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Notifications.Application;

public class NotificationServiceTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly INotificationRecipientRepository _recipientRepository = Substitute.For<INotificationRecipientRepository>();
    private readonly INotificationTemplateRepository _templateRepository = Substitute.For<INotificationTemplateRepository>();
    private readonly INotificationPreferenceRepository _preferenceRepository = Substitute.For<INotificationPreferenceRepository>();
    private readonly INotificationDeliveryRepository _deliveryRepository = Substitute.For<INotificationDeliveryRepository>();
    private readonly INotificationDeliveryQueue _deliveryQueue = Substitute.For<INotificationDeliveryQueue>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        // A resolved tenant, matching what's already guaranteed by the time NotifyAsync
        // reaches its enqueue step in real usage (see that method's own doc comment).
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _tenantContext.ConnectionString.Returns("Host=localhost;Database=test");

        _sut = new NotificationService(
            _notificationRepository,
            _recipientRepository,
            _templateRepository,
            _preferenceRepository,
            _deliveryRepository,
            _deliveryQueue,
            _tenantContext,
            NullLogger<NotificationService>.Instance);
    }

    private static NotifyRequest Request(params Guid[] recipientUserIds) => new()
    {
        TemplateKey = "appointment.booked",
        Category = "appointment",
        Title = "Appointment booked",
        Body = "Your appointment is confirmed.",
        SourceModule = "Appointments",
        RecipientUserIds = recipientUserIds,
    };

    [Fact]
    public async Task NotifyAsync_WithRecipients_CreatesNotificationAndFansOutRecipients()
    {
        var recipientIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var result = await _sut.NotifyAsync(Request(recipientIds), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecipientCount.Should().Be(2);
        await _notificationRepository.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _recipientRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<NotificationRecipient>>(r => r.Count() == 2),
            Arg.Any<CancellationToken>());
        await _notificationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_DeduplicatesRecipientUserIds()
    {
        var userId = Guid.NewGuid();

        var result = await _sut.NotifyAsync(Request(userId, userId), actorId: null, CancellationToken.None);

        result.Value!.RecipientCount.Should().Be(1);
    }

    [Fact]
    public async Task NotifyAsync_WithNoSavedPreference_QueuesOnlyEmailDelivery()
    {
        // Default when no NotificationPreference row exists: email on, sms off — mirrors
        // NotificationPreference.Create's own defaults (see ResolveChannelsAsync's doc
        // comment).
        _preferenceRepository
            .GetByUserAndCategoryAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((NotificationPreference?)null);

        await _sut.NotifyAsync(Request(Guid.NewGuid()), actorId: null, CancellationToken.None);

        await _deliveryRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<NotificationDelivery>>(d => d.Count() == 1 && d.Single().Channel == NotificationChannel.Email),
            Arg.Any<CancellationToken>());
        await _deliveryQueue.Received(1).EnqueueAsync(Arg.Any<NotificationDeliveryQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WithBothChannelsDisabled_QueuesNoDeliveries()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, "appointment", null);
        preference.UpdateChannels(inAppEnabled: true, emailEnabled: false, smsEnabled: false, null);
        _preferenceRepository.GetByUserAndCategoryAsync(userId, "appointment", Arg.Any<CancellationToken>()).Returns(preference);

        await _sut.NotifyAsync(Request(userId), actorId: null, CancellationToken.None);

        await _deliveryRepository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<NotificationDelivery>>(), Arg.Any<CancellationToken>());
        await _deliveryQueue.DidNotReceive().EnqueueAsync(Arg.Any<NotificationDeliveryQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WithEmergencySeverity_QueuesBothChannelsRegardlessOfPreference()
    {
        var userId = Guid.NewGuid();
        var preference = NotificationPreference.Create(userId, "emergency", null);
        preference.UpdateChannels(inAppEnabled: true, emailEnabled: false, smsEnabled: false, null);
        _preferenceRepository.GetByUserAndCategoryAsync(userId, "emergency", Arg.Any<CancellationToken>()).Returns(preference);

        var request = Request(userId) with { Category = "emergency", Severity = NotificationSeverity.Emergency };

        await _sut.NotifyAsync(request, actorId: null, CancellationToken.None);

        await _deliveryRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<NotificationDelivery>>(d => d.Count() == 2),
            Arg.Any<CancellationToken>());
        await _deliveryQueue.Received(2).EnqueueAsync(Arg.Any<NotificationDeliveryQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WithoutBody_RendersFromActiveInAppTemplate()
    {
        var template = NotificationTemplate.Create("appointment.booked", NotificationChannel.InApp, null, "Hello {{PatientName}}, your visit is confirmed.", null);
        _templateRepository
            .GetByKeyAndChannelAsync("appointment.booked", NotificationChannel.InApp, Arg.Any<CancellationToken>())
            .Returns(template);

        var request = Request(Guid.NewGuid()) with { Body = null, Placeholders = new Dictionary<string, string> { ["PatientName"] = "Aravind" } };

        var result = await _sut.NotifyAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _notificationRepository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n.Body == "Hello Aravind, your visit is confirmed."),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WithoutBodyAndNoActiveTemplate_ReturnsTemplateNotFoundFailure()
    {
        _templateRepository
            .GetByKeyAndChannelAsync(Arg.Any<string>(), NotificationChannel.InApp, Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);

        var request = Request(Guid.NewGuid()) with { Body = null };

        var result = await _sut.NotifyAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(NotificationErrorCodes.TemplateNotFound);
        await _notificationRepository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_WithNoRecipients_ReturnsNoRecipientsFailure()
    {
        var result = await _sut.NotifyAsync(Request(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(NotificationErrorCodes.NoRecipients);
        await _notificationRepository.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyNotificationsAsync_MapsRecipientsWithTheirNotifications()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create("k", "cat", "Title", "Body", "Module", null, null, NotificationSeverity.Normal, null);
        var recipient = NotificationRecipient.Create(notification.Id, userId, null);

        _recipientRepository
            .GetByUserAsync(userId, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<NotificationRecipient>([recipient], 1, 20, 1));
        _notificationRepository
            .GetManyByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([notification]);

        var page = await _sut.GetMyNotificationsAsync(userId, null, 1, 20, CancellationToken.None);

        page.Items.Should().HaveCount(1);
        page.Items[0].Id.Should().Be(recipient.Id);
        page.Items[0].Title.Should().Be("Title");
        page.Items[0].IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GetUnreadCountAsync_DelegatesToRepository()
    {
        var userId = Guid.NewGuid();
        _recipientRepository.GetUnreadCountAsync(userId, Arg.Any<CancellationToken>()).Returns(3);

        var count = await _sut.GetUnreadCountAsync(userId, CancellationToken.None);

        count.Should().Be(3);
    }

    [Fact]
    public async Task MarkAsReadAsync_ForOwnNotification_MarksReadAndSucceeds()
    {
        var userId = Guid.NewGuid();
        var recipient = NotificationRecipient.Create(Guid.NewGuid(), userId, null);
        _recipientRepository.GetByIdAsync(recipient.Id, Arg.Any<CancellationToken>()).Returns(recipient);

        var result = await _sut.MarkAsReadAsync(recipient.Id, userId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        recipient.IsRead.Should().BeTrue();
        await _recipientRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_ForAnotherUsersNotification_ReturnsNotFoundFailure()
    {
        var owner = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var recipient = NotificationRecipient.Create(Guid.NewGuid(), owner, null);
        _recipientRepository.GetByIdAsync(recipient.Id, Arg.Any<CancellationToken>()).Returns(recipient);

        var result = await _sut.MarkAsReadAsync(recipient.Id, caller, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(NotificationErrorCodes.NotFound);
        recipient.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenRecipientDoesNotExist_ReturnsNotFoundFailure()
    {
        _recipientRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((NotificationRecipient?)null);

        var result = await _sut.MarkAsReadAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(NotificationErrorCodes.NotFound);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksEveryUnreadRecipientRead()
    {
        var userId = Guid.NewGuid();
        var unread = new[]
        {
            NotificationRecipient.Create(Guid.NewGuid(), userId, null),
            NotificationRecipient.Create(Guid.NewGuid(), userId, null),
        };
        _recipientRepository.GetUnreadByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(unread);

        var result = await _sut.MarkAllAsReadAsync(userId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        unread.Should().OnlyContain(r => r.IsRead);
        await _recipientRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WithNoUnread_SucceedsWithoutSaving()
    {
        _recipientRepository.GetUnreadByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await _sut.MarkAllAsReadAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _recipientRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
