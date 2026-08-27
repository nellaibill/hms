using HMS.Modules.Notifications.Application.Abstractions;
using HMS.Modules.Notifications.Application.Mapping;
using HMS.Modules.Notifications.Contracts;
using HMS.Modules.Notifications.Domain;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Notifications.Application;

internal class NotificationTemplateService : INotificationTemplateService
{
    private readonly INotificationTemplateRepository _repository;
    private readonly ILogger<NotificationTemplateService> _logger;

    public NotificationTemplateService(INotificationTemplateRepository repository, ILogger<NotificationTemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<NotificationTemplateResponse>> CreateAsync(CreateNotificationTemplateRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByKeyAndChannelAsync(request.TemplateKey, request.Channel, cancellationToken);
        if (existing is not null)
        {
            return Result<NotificationTemplateResponse>.Failure(
                NotificationErrorCodes.DuplicateTemplate,
                $"A template for '{request.TemplateKey}' on channel '{request.Channel}' already exists.");
        }

        var template = NotificationTemplate.Create(request.TemplateKey, request.Channel, request.Subject, request.BodyTemplate, actorId);

        await _repository.AddAsync(template, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created notification template {TemplateKey}/{Channel}", template.TemplateKey, template.Channel);

        return Result<NotificationTemplateResponse>.Success(template.ToResponse());
    }

    public async Task<Result<NotificationTemplateResponse>> UpdateAsync(Guid id, UpdateNotificationTemplateRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return Result<NotificationTemplateResponse>.Failure(NotificationErrorCodes.TemplateNotFound, $"Template '{id}' was not found.");
        }

        // Checked here (with the loaded entity's Channel in hand) rather than relying on
        // NotificationTemplate.UpdateContent's own guard throwing — an expected validation
        // failure must come back as a Result, never an exception, per
        // docs/Architecture.md's exception handling strategy. UpdateContent's guard still
        // exists as defense-in-depth; this check keeps it from ever firing on this path.
        if (template.Channel == NotificationChannel.Email && string.IsNullOrWhiteSpace(request.Subject))
        {
            return Result<NotificationTemplateResponse>.Failure(
                NotificationErrorCodes.EmailTemplateRequiresSubject,
                "Subject is required for an Email template.");
        }

        template.UpdateContent(request.Subject, request.BodyTemplate, actorId);

        if (request.IsActive)
        {
            template.Activate(actorId);
        }
        else
        {
            template.Deactivate(actorId);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated notification template {TemplateId}", template.Id);

        return Result<NotificationTemplateResponse>.Success(template.ToResponse());
    }

    public async Task<IReadOnlyList<NotificationTemplateResponse>> GetAllAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var templates = await _repository.GetAllAsync(isActive, cancellationToken);
        return templates.Select(t => t.ToResponse()).ToList();
    }
}
