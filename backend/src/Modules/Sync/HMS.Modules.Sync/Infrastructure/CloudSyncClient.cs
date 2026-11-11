using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using HMS.Modules.Sync.Application;
using HMS.Modules.Sync.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Sync.Infrastructure;

/// <summary>Result of one HTTP call to the cloud. Error text never contains payload data.</summary>
internal sealed record CloudCallResult<T>(
    CloudTransportOutcome Outcome,
    int? HttpStatus,
    T? Body,
    string? Error,
    int DurationMs);

internal interface ICloudSyncClient
{
    Task<CloudCallResult<SyncBatchResponse>> SendAsync(string apiKey, SyncBatchRequest request, CancellationToken cancellationToken);

    Task<CloudCallResult<SyncPingResponse>> PingAsync(string apiKey, CancellationToken cancellationToken);
}

/// <summary>
/// Talks to the cloud's sync API with the hospital's key in an <c>Authorization: HmsSync</c>
/// header. The key comes from this server's environment only and is never logged or returned to
/// any browser. Classifies every failure (ADR-133 §7.3):
/// no answer (DNS, refused, socket, timeout) → Connectivity; 401/403 → AuthFailed;
/// any other non-2xx or an unreadable body → HttpError; this server shutting down → Cancelled.
/// </summary>
internal sealed class CloudSyncClient : ICloudSyncClient
{
    public const string HttpClientName = "HmsSyncCloud";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;

    public CloudSyncClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<CloudCallResult<SyncBatchResponse>> SendAsync(string apiKey, SyncBatchRequest request, CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/sync/patients")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };

        return CallAsync<SyncBatchResponse>(apiKey, message, cancellationToken);
    }

    public Task<CloudCallResult<SyncPingResponse>> PingAsync(string apiKey, CancellationToken cancellationToken) =>
        CallAsync<SyncPingResponse>(apiKey, new HttpRequestMessage(HttpMethod.Get, "api/v1/sync/ping"), cancellationToken);

    private async Task<CloudCallResult<T>> CallAsync<T>(string apiKey, HttpRequestMessage message, CancellationToken cancellationToken)
        where T : class
    {
        using var _ = message;
        message.Headers.Authorization = new AuthenticationHeaderValue(SyncAuthenticationDefaults.Scheme, apiKey);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var status = (int)response.StatusCode;

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new(CloudTransportOutcome.AuthFailed, status, null, $"The cloud rejected this server's sync key (HTTP {status}).", Elapsed(stopwatch));
            }

            if (!response.IsSuccessStatusCode)
            {
                return new(CloudTransportOutcome.HttpError, status, null, $"The cloud answered HTTP {status}.", Elapsed(stopwatch));
            }

            ApiResponse<T>? envelope;
            try
            {
                envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, cancellationToken);
            }
            catch (JsonException)
            {
                envelope = null;
            }

            return envelope?.Data is { } body
                ? new(CloudTransportOutcome.Success, status, body, null, Elapsed(stopwatch))
                : new(CloudTransportOutcome.HttpError, status, null, "The cloud's answer could not be read.", Elapsed(stopwatch));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(CloudTransportOutcome.Cancelled, null, null, "This server is shutting down.", Elapsed(stopwatch));
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException or IOException or SocketException)
        {
            // TaskCanceledException without our token cancelled is HttpClient's timeout.
            var reason = exception is TaskCanceledException or TimeoutException
                ? "The cloud did not answer in time."
                : $"The cloud could not be reached ({Describe(exception)}).";
            return new(CloudTransportOutcome.Connectivity, null, null, reason, Elapsed(stopwatch));
        }
    }

    private static int Elapsed(Stopwatch stopwatch) => (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds);

    private static string Describe(Exception exception) => exception switch
    {
        HttpRequestException { HttpRequestError: not HttpRequestError.Unknown } http => http.HttpRequestError.ToString(),
        HttpRequestException { InnerException: SocketException socket } => socket.SocketErrorCode.ToString(),
        _ => exception.GetType().Name,
    };
}
