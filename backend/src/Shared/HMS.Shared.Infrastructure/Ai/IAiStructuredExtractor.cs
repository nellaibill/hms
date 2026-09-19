using System.Text.Json;
using HMS.Shared.Kernel;

namespace HMS.Shared.Infrastructure.Ai;

public static class AiErrorCodes
{
    public const string NotConfigured = "AI.NOT_CONFIGURED";
    public const string RequestFailed = "AI.REQUEST_FAILED";
}

/// <param name="InputSchema">JSON-schema object (anonymous type or dictionary) describing the tool's
/// arguments — the provider is forced to answer by calling this one tool, so the reply is always
/// schema-shaped JSON rather than prose.</param>
public sealed record AiStructuredExtractionRequest(
    string SystemPrompt,
    string UserContent,
    string ToolName,
    string ToolDescription,
    object InputSchema,
    int MaxTokens = 2048);

/// <summary>
/// Provider-agnostic "turn this text into schema-shaped JSON" call, selected once at startup from
/// <c>Ai:Provider</c> (Anthropic default, or OpenAI). Unlike best-effort notification senders, a
/// missing key or failed call is always returned as a Result.Failure (<see cref="AiErrorCodes"/>) —
/// the caller is a user waiting on the result and needs a real error, not a silent no-op.
/// </summary>
public interface IAiStructuredExtractor
{
    Task<Result<JsonElement>> ExtractAsync(AiStructuredExtractionRequest request, CancellationToken cancellationToken);
}
