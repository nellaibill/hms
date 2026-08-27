using System.Net;
using System.Net.Mail;
using HMS.Modules.Notifications.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Notifications.Infrastructure;

/// <summary>
/// Sends real email via SMTP, config-driven under <c>Notifications:Smtp:*</c> — reads
/// IConfiguration directly in the constructor, matching
/// HMS.Modules.Identity.Infrastructure.JwtTokenGenerator's identical pattern (no
/// IOptions&lt;T&gt; wrapper; nothing else in this codebase uses that). Uses the .NET runtime's
/// built-in <see cref="SmtpClient"/> rather than adding a new NuGet dependency (e.g.
/// MailKit) for a single send-a-message operation.
///
/// Unlike HMS.Modules.Identity's Jwt:* configuration (missing values throw at startup,
/// since auth cannot function without them), missing SMTP configuration here logs a warning
/// and no-ops instead — Email is a best-effort channel (see NotificationDeliveryBackgroundService's
/// own doc comment); a hospital that hasn't configured SMTP yet should not have every
/// notification delivery attempt throw.
/// </summary>
internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly string? _host;
    private readonly int _port;
    private readonly bool _enableSsl;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string? _fromAddress;
    private readonly string _fromName;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _host = configuration["Notifications:Smtp:Host"];
        _port = configuration.GetValue<int?>("Notifications:Smtp:Port") ?? 587;
        _enableSsl = configuration.GetValue<bool?>("Notifications:Smtp:EnableSsl") ?? true;
        _username = configuration["Notifications:Smtp:Username"];
        _password = configuration["Notifications:Smtp:Password"];
        _fromAddress = configuration["Notifications:Smtp:FromAddress"];
        _fromName = configuration["Notifications:Smtp:FromName"] ?? "HMS";
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_host) || string.IsNullOrWhiteSpace(_fromAddress))
        {
            _logger.LogWarning("Notifications:Smtp:Host/FromAddress is not configured — email to {ToEmail} was not sent.", toEmail);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_fromAddress, _fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_host, _port) { EnableSsl = _enableSsl };
        if (!string.IsNullOrWhiteSpace(_username))
        {
            client.Credentials = new NetworkCredential(_username, _password);
        }

        // SmtpClient.SendMailAsync(MailMessage) has no CancellationToken-accepting overload
        // (a long-standing BCL gap) — cancellationToken is accepted for interface symmetry
        // with ISmsSender but isn't honored mid-send here.
        await client.SendMailAsync(message);
    }
}
