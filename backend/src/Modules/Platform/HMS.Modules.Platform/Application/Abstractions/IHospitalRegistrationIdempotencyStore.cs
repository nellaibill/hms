using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Abstractions;

internal enum IdempotencyReservationOutcome
{
    /// <summary>No prior request used this key — the caller may proceed and must call
    /// <see cref="IHospitalRegistrationIdempotencyStore.CompleteAsync"/> with <see cref="IdempotencyReservation.RecordId"/> when done.</summary>
    Reserved,

    /// <summary>A prior request with this exact key and request body already finished —
    /// <see cref="IdempotencyReservation.ReplayedResult"/> is the result to return, unexecuted.</summary>
    ReplayCompleted,

    /// <summary>A prior request with this exact key and request body is still in flight
    /// (hasn't called CompleteAsync yet) — the caller must not execute anything.</summary>
    ReplayInProgress,

    /// <summary>This key was already used for a request with a different body — misuse of
    /// the Idempotency-Key contract, not a legitimate retry.</summary>
    KeyReusedForDifferentRequest,
}

internal sealed record IdempotencyReservation(
    IdempotencyReservationOutcome Outcome,
    Guid? RecordId = null,
    Result<CreateHospitalResponse>? ReplayedResult = null);

/// <summary>
/// Scoped narrowly to hospital registration (not a generic idempotency framework) — this is
/// the only endpoint that needs it today. See IdempotencyRecord's own doc comment for the
/// concurrency guarantee this relies on.
/// </summary>
internal interface IHospitalRegistrationIdempotencyStore
{
    Task<IdempotencyReservation> ReserveAsync(string key, string requestHash, CancellationToken cancellationToken);

    Task CompleteAsync(Guid recordId, Result<CreateHospitalResponse> result, CancellationToken cancellationToken);
}
