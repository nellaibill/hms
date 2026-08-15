using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Application.Abstractions;

public interface IHospitalRegistrationService
{
    Task<Result<CreateHospitalResponse>> RegisterAsync(CreateHospitalRequest request, CancellationToken cancellationToken);
}
