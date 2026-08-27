using System.Net.Http.Headers;
using System.Net.Http.Json;
using HMS.Modules.Notifications.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Notifications.Infrastructure;

/// <summary>
/// Sends real SMS via a generic HTTP gateway, config-driven under
/// <c>Notifications:Sms:*</c>. No specific SMS vendor SDK (Twilio, MSG91, etc.) is
/// referenced — which gateway a hospital actually uses is a real infra decision the design
/// doc deliberately left to whoever configures a tenant's deployment, not something to bake
/// into this codebase's dependencies. This posts a JSON body
/// (<c>{ to, from, message }</c> with a Bearer <c>ApiKey</c>) to a configured
/// <c>BaseUrl</c> — a shape common enough to front most REST-based SMS gateways directly or
/// via a thin adapter; a gateway with a genuinely different contract needs its own
/// ISmsSender implementation, not a change to this one's shape.
///
/// Missing configuration logs a warning and no-ops rather than throwing — same reasoning as
/// SmtpEmailSender: Sms is a best-effort channel, and a tenant that hasn't configured a
/// gateway yet should not have every notification delivery attempt throw.
/// </summary>
internal sealed class HttpSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly string? _baseUrl;
    private readonly string? _apiKey;
    private readonly string? _senderId;
    private readonly ILogger<HttpSmsSender> _logger;

    public HttpSmsSender(HttpClient httpClient, IConfiguration configuration, ILogger<HttpSmsSender> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Notifications:Sms:BaseUrl"];
        _apiKey = configuration["Notifications:Sms:ApiKey"];
        _senderId = configuration["Notifications:Sms:SenderId"];
        _logger = logger;
    }

    public async Task SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("Notifications:Sms:BaseUrl/ApiKey is not configured — SMS to {ToPhoneNumber} was not sent.", toPhoneNumber);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _apiKey) },
            Content = JsonContent.Create(new { to = toPhoneNumber, from = _senderId, message = body }),
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
