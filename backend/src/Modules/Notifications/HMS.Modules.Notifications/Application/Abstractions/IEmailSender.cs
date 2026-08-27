namespace HMS.Modules.Notifications.Application.Abstractions;

/// <summary>Implemented in Infrastructure by SmtpEmailSender.</summary>
internal interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
}
