using HMS.Modules.Masters.Application.Abstractions;
using HMS.Modules.Masters.Application.Mapping;
using HMS.Modules.Masters.Contracts;
using HMS.Modules.Masters.Domain;
using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Application;

/// <summary>
/// Public (not internal): DiagnosticProvidersController requires a public constructor
/// dependency (CS0051 otherwise); DiagnosticServiceService also depends on this to validate
/// DiagnosticService.ProviderId. Interface and implementation share this file, matching
/// DiagnosticTestService's convention.
/// </summary>
public interface IDiagnosticProviderService
{
    Task<Result<DiagnosticProviderResponse>> CreateAsync(CreateDiagnosticProviderRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticProviderResponse>> UpdateAsync(Guid id, UpdateDiagnosticProviderRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<DiagnosticProviderResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DiagnosticProviderResponse>> GetPagedAsync(DiagnosticProviderListQuery query, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

internal class DiagnosticProviderService : IDiagnosticProviderService
{
    private readonly IDiagnosticProviderRepository _repository;

    public DiagnosticProviderService(IDiagnosticProviderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DiagnosticProviderResponse>> CreateAsync(CreateDiagnosticProviderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByCodeAsync(request.Code.Trim(), excludingId: null, cancellationToken))
        {
            return Result<DiagnosticProviderResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Provider code '{request.Code}' is already in use.");
        }

        var provider = DiagnosticProvider.Create(request.Code, request.Name, request.ContactDetails, request.IsActive, actorId);

        await _repository.AddAsync(provider, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticProviderResponse>.Success(provider.ToResponse());
    }

    public async Task<Result<DiagnosticProviderResponse>> UpdateAsync(Guid id, UpdateDiagnosticProviderRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var provider = await _repository.GetByIdAsync(id, cancellationToken);
        if (provider is null)
        {
            return Result<DiagnosticProviderResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic provider '{id}' was not found.");
        }

        if (await _repository.ExistsByCodeAsync(request.Code.Trim(), excludingId: id, cancellationToken))
        {
            return Result<DiagnosticProviderResponse>.Failure(MastersErrorCodes.DuplicateCode, $"Provider code '{request.Code}' is already in use.");
        }

        provider.Update(request.Code, request.Name, request.ContactDetails, request.IsActive, actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<DiagnosticProviderResponse>.Success(provider.ToResponse());
    }

    public async Task<Result<DiagnosticProviderResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var provider = await _repository.GetByIdAsync(id, cancellationToken);
        return provider is null
            ? Result<DiagnosticProviderResponse>.Failure(MastersErrorCodes.NotFound, $"Diagnostic provider '{id}' was not found.")
            : Result<DiagnosticProviderResponse>.Success(provider.ToResponse());
    }

    public async Task<PagedResult<DiagnosticProviderResponse>> GetPagedAsync(DiagnosticProviderListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<DiagnosticProviderResponse>(items.Select(p => p.ToResponse()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var provider = await _repository.GetByIdAsync(id, cancellationToken);
        if (provider is null)
        {
            return Result.Failure(MastersErrorCodes.NotFound, $"Diagnostic provider '{id}' was not found.");
        }

        provider.SoftDelete(actorId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
