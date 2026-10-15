using HMS.Shared.Kernel;

namespace HMS.Modules.Appointments.Domain;

/// <summary>
/// A stretch of days on which a consultant can't be booked — leave, a conference, a cancelled
/// clinic. <see cref="Session"/> null closes the whole day; 1 or 2 closes just that visiting
/// session. Removing a block soft-deletes it, so the slots open again.
/// </summary>
internal class ConsultantBlock : Entity
{
    public const int MaxDays = 366;

    public Guid ConsultantId { get; private set; }
    public DateOnly FromDate { get; private set; }
    public DateOnly ToDate { get; private set; }
    public int? Session { get; private set; }
    public string Reason { get; private set; } = null!;

    // Required by EF Core materialization.
    private ConsultantBlock()
    {
    }

    private ConsultantBlock(Guid id, Guid? createdBy)
        : base(id, createdBy)
    {
    }

    public static ConsultantBlock Create(Guid consultantId, DateOnly fromDate, DateOnly toDate, int? session, string reason, Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(reason, nameof(reason));

        if (toDate < fromDate)
        {
            throw new ArgumentException("The block must end on or after the day it starts.", nameof(toDate));
        }

        if (toDate.DayNumber - fromDate.DayNumber + 1 > MaxDays)
        {
            throw new ArgumentException($"A block can cover at most {MaxDays} days.", nameof(toDate));
        }

        if (session is not (null or 1 or 2))
        {
            throw new ArgumentException("Session must be 1, 2, or empty for the whole day.", nameof(session));
        }

        return new ConsultantBlock(Guid.CreateVersion7(), createdBy)
        {
            ConsultantId = consultantId,
            FromDate = fromDate,
            ToDate = toDate,
            Session = session,
            Reason = reason.Trim(),
        };
    }

    /// <summary>True when this block closes the given session on the given date.</summary>
    public bool Covers(DateOnly date, int session)
        => date >= FromDate && date <= ToDate && (Session is null || Session == session);
}
