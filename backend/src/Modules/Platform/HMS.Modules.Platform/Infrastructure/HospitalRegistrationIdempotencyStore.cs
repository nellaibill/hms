using System.Text.Json;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Contracts;
using HMS.Modules.Platform.Domain;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HMS.Modules.Platform.Infrastructure;

internal sealed class HospitalRegistrationIdempotencyStore : IHospitalRegistrationIdempotencyStore
{
    private const string UniqueViolationSqlState = "23505";

    private readonly PlatformDbContext _dbContext;

    public HospitalRegistrationIdempotencyStore(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyReservation> ReserveAsync(string key, string requestHash, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.IdempotencyRecords.FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

        if (existing is null)
        {
            var record = IdempotencyRecord.Reserve(key, requestHash);
            _dbContext.IdempotencyRecords.Add(record);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return new IdempotencyReservation(IdempotencyReservationOutcome.Reserved, RecordId: record.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Lost the race to a concurrent request reserving the same key — detach our
                // half-added row and fall through to read the winner's row below instead.
                _dbContext.Entry(record).State = EntityState.Detached;
                existing = await _dbContext.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Key == key, cancellationToken)
                    ?? throw new InvalidOperationException($"Idempotency key '{key}' hit a unique-constraint violation but no row was found on re-read.");
            }
        }

        if (existing.RequestHash != requestHash)
        {
            return new IdempotencyReservation(IdempotencyReservationOutcome.KeyReusedForDifferentRequest);
        }

        if (existing.Status == IdempotencyStatus.Pending)
        {
            return new IdempotencyReservation(IdempotencyReservationOutcome.ReplayInProgress);
        }

        var replayedValue = existing.ResultIsSuccess == true
            ? JsonSerializer.Deserialize<CreateHospitalResponse>(existing.ResultResponseJson!)
            : null;

        var replayedResult = existing.ResultIsSuccess == true
            ? Result<CreateHospitalResponse>.Success(replayedValue!)
            : Result<CreateHospitalResponse>.Failure(existing.ResultErrorCode!, existing.ResultErrorMessage!);

        return new IdempotencyReservation(IdempotencyReservationOutcome.ReplayCompleted, ReplayedResult: replayedResult);
    }

    public async Task CompleteAsync(Guid recordId, Result<CreateHospitalResponse> result, CancellationToken cancellationToken)
    {
        var record = await _dbContext.IdempotencyRecords.FirstAsync(r => r.Id == recordId, cancellationToken);

        record.Complete(
            result.IsSuccess,
            result.ErrorCode,
            result.Error,
            result.IsSuccess ? JsonSerializer.Serialize(result.Value) : null);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState };
}
