using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HMS.Modules.Radiology.Application;
using HMS.Modules.Radiology.Application.Abstractions;
using HMS.Shared.Kernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HMS.Modules.Radiology.Infrastructure;

/// <summary>
/// Vision-language model call over the OpenAI-compatible Chat Completions wire format, which
/// Hugging Face Inference Endpoints (TGI/vLLM), the HF router, Ollama and llama.cpp all serve — so
/// moving from a hosted endpoint to a self-hosted one is a configuration change only. Config under
/// <c>Ai:Radiology:*</c>: BaseUrl (up to and including "/v1"), ApiKey (optional for a local server),
/// Model (default "google/medgemma-4b-it"), TimeoutSeconds (default 300 — CPU inference is slow).
/// </summary>
internal sealed class OpenAiCompatibleXrayImageAnalyzer : IXrayImageAnalyzer
{
    internal const string DefaultModel = "google/medgemma-4b-it";
    internal const int DefaultTimeoutSeconds = 300;

    private readonly HttpClient _httpClient;
    private readonly string? _baseUrl;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly TimeSpan _timeout;
    private readonly ILogger<OpenAiCompatibleXrayImageAnalyzer> _logger;

    public OpenAiCompatibleXrayImageAnalyzer(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiCompatibleXrayImageAnalyzer> logger)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Ai:Radiology:BaseUrl"]?.Trim().TrimEnd('/');
        _apiKey = configuration["Ai:Radiology:ApiKey"];
        _model = configuration["Ai:Radiology:Model"] is { Length: > 0 } model ? model : DefaultModel;
        _timeout = TimeSpan.FromSeconds(
            int.TryParse(configuration["Ai:Radiology:TimeoutSeconds"], out var seconds) && seconds > 0 ? seconds : DefaultTimeoutSeconds);
        _logger = logger;
    }

    public async Task<Result<XrayImageAnalysis>> AnalyzeAsync(byte[] image, string contentType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            _logger.LogWarning("Ai:Radiology:BaseUrl is not configured — X-ray analysis was not attempted.");
            return Result<XrayImageAnalysis>.Failure(RadiologyAiErrorCodes.NotConfigured, "AI X-ray analysis isn't configured for this environment.");
        }

        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(image)}";
        var body = new
        {
            model = _model,
            max_tokens = 1500,
            temperature = 0.1,
            messages = new object[]
            {
                new { role = "system", content = XrayAnalysisPrompt.System },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = XrayAnalysisPrompt.UserInstruction },
                        new { type = "image_url", image_url = new { url = dataUrl } },
                    },
                },
            },
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions") { Content = JsonContent.Create(body) };
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);

        try
        {
            using var response = await _httpClient.SendAsync(message, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("X-ray AI endpoint returned {StatusCode}.", (int)response.StatusCode);
                return Failed();
            }

            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
            var text = ExtractText(json.RootElement);
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogError("X-ray AI endpoint returned no message content.");
                return Failed();
            }

            return Result<XrayImageAnalysis>.Success(new XrayImageAnalysis(text.Trim(), _model));
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Covers transport errors, our own timeout (an OperationCanceledException on the linked
            // token) and unparseable JSON; a caller-initiated cancel still propagates.
            _logger.LogError(ex, "X-ray AI request failed or timed out.");
            return Failed();
        }
    }

    internal static string? ExtractText(JsonElement root)
    {
        if (root.TryGetProperty("choices", out var choices)
            && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0
            && choices[0].TryGetProperty("message", out var message)
            && message.TryGetProperty("content", out var content)
            && content.ValueKind == JsonValueKind.String)
        {
            return content.GetString();
        }

        return null;
    }

    private static Result<XrayImageAnalysis> Failed() =>
        Result<XrayImageAnalysis>.Failure(RadiologyAiErrorCodes.RequestFailed, "The AI service couldn't analyze this image. Please try again.");
}
