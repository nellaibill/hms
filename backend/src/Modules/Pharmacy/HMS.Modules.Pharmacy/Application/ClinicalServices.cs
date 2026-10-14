using System.Text.RegularExpressions;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.OpdConsultation.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

public interface IPrescriptionQueueService
{
    /// <summary>Completed OPD consultations with medicines prescribed on the given
    /// hospital-local days, newest first, each with any bills already made against it.</summary>
    Task<IReadOnlyList<PrescriptionQueueItemResponse>> GetQueueAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);
}

/// <summary>
/// The pharmacy's view of what doctors prescribed in OPD. Prescriptions stay where they are
/// written (the OpdConsultation module); this reads them and sets them against the pharmacy's
/// own bills, so the counter can see who is on their way and what has already been given.
/// </summary>
internal partial class PrescriptionQueueService : IPrescriptionQueueService
{
    private readonly IOpdConsultationService _opdConsultationService;
    private readonly IPharmacySaleRepository _saleRepository;

    public PrescriptionQueueService(IOpdConsultationService opdConsultationService, IPharmacySaleRepository saleRepository)
    {
        _opdConsultationService = opdConsultationService;
        _saleRepository = saleRepository;
    }

    public async Task<IReadOnlyList<PrescriptionQueueItemResponse>> GetQueueAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var prescriptions = await _opdConsultationService.GetPrescriptionQueueAsync(
            PharmacyClock.StartOfDayUtc(fromDate), PharmacyClock.StartOfDayUtc(toDate.AddDays(1)), cancellationToken);

        var bills = await _saleRepository.GetBillsForPrescriptionsAsync(prescriptions.Select(p => p.ConsultationId).ToList(), cancellationToken);

        return prescriptions
            .Select(p => new PrescriptionQueueItemResponse
            {
                ConsultationId = p.ConsultationId,
                PatientId = p.PatientId,
                Uhid = p.Uhid,
                PatientName = p.PatientName,
                PhoneNumber = p.PhoneNumber,
                DoctorName = p.ConsultantName,
                DepartmentName = p.DepartmentName,
                PrescribedAt = p.PrescribedAt,
                Drugs = p.Prescriptions
                    .Select(d => new PrescribedDrugResponse
                    {
                        DrugName = d.DrugName,
                        Dose = d.Dose,
                        Route = d.Route,
                        Frequency = d.Frequency,
                        DurationDays = d.DurationDays,
                        Instructions = d.Instructions,
                        SuggestedQuantity = SuggestQuantity(d.Frequency, d.DurationDays),
                    })
                    .ToList(),
                Bills = bills.GetValueOrDefault(p.ConsultationId) ?? [],
            })
            .ToList();
    }

    /// <summary>
    /// Units a course comes to: doses a day × days. Reads the ways a frequency is written on
    /// an Indian prescription — "1-0-1", "1-1-1", "½-0-½", "OD", "BD", "TDS", "QID", "HS",
    /// "twice daily". Returns null when it cannot tell (SOS, "as directed", a blank), rather
    /// than guessing a number onto a bill.
    /// </summary>
    internal static decimal? SuggestQuantity(string? frequency, int? durationDays)
    {
        if (durationDays is not > 0 || string.IsNullOrWhiteSpace(frequency))
        {
            return null;
        }

        var perDay = DosesPerDay(frequency.Trim());
        return perDay is { } doses and > 0 ? Math.Ceiling(doses * durationDays.Value) : null;
    }

    private static decimal? DosesPerDay(string frequency)
    {
        // "1-0-1", "1 - 1 - 1", "½-0-½", "0.5-0-0.5": morning-noon-night amounts.
        var pattern = DosePattern().Match(frequency);
        if (pattern.Success)
        {
            return pattern.Groups["dose"].Captures.Sum(capture => ParseDose(capture.Value));
        }

        var text = frequency.ToUpperInvariant().Replace(".", string.Empty).Trim();
        return text switch
        {
            "OD" or "QD" or "HS" or "BBF" or "ONCE DAILY" or "ONCE A DAY" or "DAILY" or "AT NIGHT" or "AT BEDTIME" or "1X" => 1m,
            "BD" or "BID" or "TWICE DAILY" or "TWICE A DAY" or "2X" => 2m,
            "TDS" or "TID" or "THRICE DAILY" or "THREE TIMES A DAY" or "THREE TIMES DAILY" or "3X" => 3m,
            "QID" or "QDS" or "FOUR TIMES A DAY" or "FOUR TIMES DAILY" or "4X" => 4m,
            _ => null,
        };
    }

    private static decimal ParseDose(string value) => value switch
    {
        "½" => 0.5m,
        "¼" => 0.25m,
        "1/2" => 0.5m,
        "1/4" => 0.25m,
        _ => decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var dose) ? dose : 0m,
    };

    [GeneratedRegex(@"^(?<dose>½|¼|1/2|1/4|\d+(?:\.\d+)?)(?:\s*-\s*(?<dose>½|¼|1/2|1/4|\d+(?:\.\d+)?)){1,3}$")]
    private static partial Regex DosePattern();
}

public interface IIndentService
{
    Task<Result<IndentResponse>> CreateAsync(CreateIndentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<IndentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<IndentResponse>> GetPagedAsync(IndentListQuery query, CancellationToken cancellationToken);

    Task<Result<IndentResponse>> CancelAsync(Guid id, Guid? actorId, CancellationToken cancellationToken);
}

/// <summary>Ward requests for an inpatient's medicines. Raising one needs only an admitted
/// patient and a list; issuing it is done at the counter as a bill charged to the admission.</summary>
internal class IndentService : IIndentService
{
    private readonly IPharmacyIndentRepository _repository;
    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly IProductService _productService;
    private readonly IAdmissionService _admissionService;

    public IndentService(
        IPharmacyIndentRepository repository,
        IPharmacyStoreRepository storeRepository,
        IPharmacyUnitOfWork unitOfWork,
        IProductService productService,
        IAdmissionService admissionService)
    {
        _repository = repository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
        _productService = productService;
        _admissionService = admissionService;
    }

    public async Task<Result<IndentResponse>> CreateAsync(CreateIndentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Fail(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        var admissionResult = await _admissionService.GetByIdAsync(request.AdmissionId, cancellationToken);
        if (!admissionResult.IsSuccess)
        {
            return Fail(PharmacyErrorCodes.InvalidAdmission, "The admission was not found.");
        }

        var admission = admissionResult.Value!;
        if (admission.Status != AdmissionStatus.Admitted)
        {
            return Fail(PharmacyErrorCodes.InvalidAdmission, $"Admission {admission.AdmissionNumber} is not currently admitted.");
        }

        var products = await _productService.GetLookupAsync(request.Lines.Select(l => l.ProductId), cancellationToken);
        for (var i = 0; i < request.Lines.Count; i++)
        {
            if (!products.TryGetValue(request.Lines[i].ProductId, out var product) || !product.IsActive)
            {
                return Fail(PharmacyErrorCodes.InvalidProduct, $"Line {i + 1}: the product was not found or is inactive.");
            }
        }

        var location = string.Join(" · ", new[] { admission.WardName, admission.BedNumber }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var indentNo = await _unitOfWork.NextNumberAsync(DocumentSeries.Indent, cancellationToken);
        var indent = PharmacyIndent.Create(
            indentNo, store.Id, admission.Id, admission.AdmissionNumber, admission.PatientId, admission.PatientName, admission.Uhid,
            location, admission.ConsultantName, request.IsUrgent, request.Remarks, actorId);

        foreach (var line in request.Lines)
        {
            indent.AddLine(line.ProductId, products[line.ProductId].ProductName, line.Quantity, line.Instructions);
        }

        await _repository.AddAsync(indent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<IndentResponse>.Success(ToResponse(indent, indent.Lines.Count, includeLines: true));
    }

    public async Task<Result<IndentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var indent = await _repository.GetByIdAsync(id, cancellationToken);
        return indent is null
            ? Fail(PharmacyErrorCodes.NotFound, "The indent was not found.")
            : Result<IndentResponse>.Success(ToResponse(indent, indent.Lines.Count, includeLines: true));
    }

    public async Task<PagedResult<IndentResponse>> GetPagedAsync(IndentListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        return new PagedResult<IndentResponse>(items.Select(i => ToResponse(i, i.Lines.Count, includeLines: true)).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<Result<IndentResponse>> CancelAsync(Guid id, Guid? actorId, CancellationToken cancellationToken)
    {
        var indent = await _repository.GetByIdAsync(id, cancellationToken);
        if (indent is null)
        {
            return Fail(PharmacyErrorCodes.NotFound, "The indent was not found.");
        }

        if (!indent.IsOpen)
        {
            return Fail(PharmacyErrorCodes.InvalidState, $"Indent {indent.IndentNo} is already {indent.Status.ToString().ToLowerInvariant()}.");
        }

        indent.Cancel(actorId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<IndentResponse>.Success(ToResponse(indent, indent.Lines.Count, includeLines: true));
    }

    private static Result<IndentResponse> Fail(string code, string message) => Result<IndentResponse>.Failure(code, message);

    internal static IndentResponse ToResponse(PharmacyIndent indent, int lineCount, bool includeLines) => new()
    {
        Id = indent.Id,
        IndentNo = indent.IndentNo,
        StoreId = indent.StoreId,
        AdmissionId = indent.AdmissionId,
        AdmissionNumber = indent.AdmissionNumber,
        PatientId = indent.PatientId,
        PatientName = indent.PatientName,
        PatientUhid = indent.PatientUhid,
        Location = indent.Location,
        DoctorName = indent.DoctorName,
        RequestedAt = indent.RequestedAt,
        Status = indent.Status,
        IsUrgent = indent.IsUrgent,
        Remarks = indent.Remarks,
        LineCount = lineCount,
        Lines = includeLines
            ? indent.Lines.OrderBy(l => l.LineNo).Select(l => new IndentLineResponse
            {
                Id = l.Id,
                LineNo = l.LineNo,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                IssuedQuantity = l.IssuedQuantity,
                PendingQuantity = l.PendingQuantity,
                Instructions = l.Instructions,
            }).ToList()
            : [],
    };
}
