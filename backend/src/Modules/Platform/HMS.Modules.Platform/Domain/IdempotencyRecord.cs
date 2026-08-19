namespace HMS.Modules.Platform.Domain;

internal enum IdempotencyStatus
{
    Pending,
    Completed,
}

/// <summary>
/// Backs the Idempotency-Key protection on <c>POST /api/platform/hospitals</c> (HMS
/// Security Hardening: hospital registration had no retry protection — a timed-out request
/// could be resubmitted and risk double-provisioning a hospital database). Deliberately not
/// an <see cref="HMS.Shared.Kernel.Entity"/> — this is infrastructure plumbing scoped to one
/// use case, not a business aggregate with audit/soft-delete semantics.
///
/// Lifecycle: a request "reserves" a key by inserting a Pending row (the table's unique
/// index on Key is what makes two concurrent requests with the same key race safely — the
/// loser gets a unique-constraint violation instead of both provisioning a hospital). Once
/// the underlying work finishes, the row is completed with the outcome, so a retry with the
/// same key + same request body replays the stored result instead of re-executing.
/// </summary>
internal sealed class IdempotencyRecord
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public IdempotencyStatus Status { get; private set; }
    public bool? ResultIsSuccess { get; private set; }
    public string? ResultErrorCode { get; private set; }
    public string? ResultErrorMessage { get; private set; }
    public string? ResultResponseJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Required by EF Core materialization.
    private IdempotencyRecord()
    {
    }

    private IdempotencyRecord(Guid id, string key, string requestHash)
    {
        Id = id;
        Key = key;
        RequestHash = requestHash;
        Status = IdempotencyStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static IdempotencyRecord Reserve(string key, string requestHash) =>
        new(Guid.CreateVersion7(), key, requestHash);

    public void Complete(bool isSuccess, string? errorCode, string? errorMessage, string? responseJson)
    {
        Status = IdempotencyStatus.Completed;
        ResultIsSuccess = isSuccess;
        ResultErrorCode = errorCode;
        ResultErrorMessage = errorMessage;
        ResultResponseJson = responseJson;
        CompletedAt = DateTime.UtcNow;
    }
}
