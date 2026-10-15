using HMS.Modules.Appointments.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Appointments.Application;

/// <summary>Public only because AppointmentsController (which must be public) takes it as a
/// constructor dependency (CS0051); the implementation stays internal.</summary>
public interface IAppointmentService
{
    Task<Result<AppointmentResponse>> BookAsync(BookAppointmentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> RescheduleAsync(Guid id, RescheduleAppointmentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> CancelAsync(Guid id, CancelAppointmentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> MarkNoShowAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> MarkArrivedAsync(Guid id, MarkAppointmentArrivedRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<AppointmentResponse>> GetPagedAsync(AppointmentListQuery query, CancellationToken cancellationToken);

    Task<Result<AppointmentSlotsResponse>> GetSlotsAsync(AppointmentSlotsQuery query, CancellationToken cancellationToken);

    Task<AppointmentSummaryResponse> GetSummaryAsync(AppointmentSummaryQuery query, CancellationToken cancellationToken);

    Task<Result<ConsultantBlockCreatedResponse>> CreateBlockAsync(CreateConsultantBlockRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result> RemoveBlockAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConsultantBlockResponse>> GetBlocksAsync(ConsultantBlockListQuery query, CancellationToken cancellationToken);
}
