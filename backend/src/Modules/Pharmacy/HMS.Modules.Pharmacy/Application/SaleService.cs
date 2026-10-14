using HMS.Modules.Patients.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Products.Application;
using HMS.Modules.Products.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Pharmacy.Application;

public interface ISaleService
{
    Task<Result<SaleResponse>> CreateAsync(CreateSaleRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SaleResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SaleResponse>> GetPagedAsync(SaleListQuery query, CancellationToken cancellationToken);

    /// <summary>A patient's bills with their lines, and the returns against them, for the
    /// patient's own record.</summary>
    Task<PatientPharmacyBillsResponse> GetPatientBillsAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Takes money against a bill that was saved with a balance due.</summary>
    Task<Result<SaleResponse>> CollectPaymentAsync(Guid saleId, CollectSalePaymentRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SaleReturnResponse>> CreateReturnAsync(Guid saleId, CreateSaleReturnRequest request, Guid? actorId, CancellationToken cancellationToken);

    Task<Result<SaleReturnResponse>> GetReturnByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<SaleReturnResponse>> GetReturnsPagedAsync(SaleReturnListQuery query, CancellationToken cancellationToken);
}

/// <summary>
/// The pharmacy counter's bill. One call takes the cart, the customer and the money and
/// produces a numbered, GST-correct bill with stock already moved — the bill is the record,
/// so there is no separate "dispense" step and nothing to reconcile afterwards.
/// </summary>
internal class SaleService : ISaleService
{
    public const string WalkInName = "Cash customer";

    /// <summary>How many of a patient's bills their record shows, newest first — years of an
    /// inpatient's daily indents would otherwise come back in one response.</summary>
    private const int MaxPatientBills = 500;

    /// <summary>Schedules whose every sale must be traceable to a named patient and a
    /// prescriber — they go into the statutory register.</summary>
    private static readonly DrugSchedule[] RegisterSchedules = [DrugSchedule.H1, DrugSchedule.X, DrugSchedule.Narcotic];

    private readonly IPharmacySaleRepository _repository;
    private readonly IPharmacyStoreRepository _storeRepository;
    private readonly IPharmacyUnitOfWork _unitOfWork;
    private readonly IStockPoster _stockPoster;
    private readonly ISellableStockReader _stockReader;
    private readonly IProductService _productService;
    private readonly IPatientService _patientService;
    private readonly IAdmissionChargePoster _admissionChargePoster;
    private readonly IPharmacySettingsRepository _settingsRepository;
    private readonly IPharmacyIndentRepository _indentRepository;

    public SaleService(
        IPharmacySaleRepository repository,
        IPharmacyStoreRepository storeRepository,
        IPharmacyUnitOfWork unitOfWork,
        IStockPoster stockPoster,
        ISellableStockReader stockReader,
        IProductService productService,
        IPatientService patientService,
        IAdmissionChargePoster admissionChargePoster,
        IPharmacySettingsRepository settingsRepository,
        IPharmacyIndentRepository indentRepository)
    {
        _settingsRepository = settingsRepository;
        _indentRepository = indentRepository;
        _repository = repository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
        _stockPoster = stockPoster;
        _stockReader = stockReader;
        _productService = productService;
        _patientService = patientService;
        _admissionChargePoster = admissionChargePoster;
    }

    public async Task<Result<SaleResponse>> CreateAsync(CreateSaleRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var store = await _storeRepository.ResolveAsync(request.StoreId, cancellationToken);
        if (store is null)
        {
            return Fail(PharmacyErrorCodes.InvalidStore, "The store was not found or is inactive.");
        }

        var maxDiscount = (await _settingsRepository.GetAsync(cancellationToken))?.MaxDiscountPercent ?? 100m;
        var highestDiscount = request.Lines.Select(l => l.DiscountPercent ?? request.DiscountPercent).DefaultIfEmpty(0m).Max();
        if (highestDiscount > maxDiscount)
        {
            return Fail(PharmacyErrorCodes.DiscountLimit, $"The most discount this counter can give is {maxDiscount:0.##}%.");
        }

        // Who the bill is for: a registered patient's own name and UHID, or whatever name the
        // walk-in customer gave (or none — an over-the-counter sale needs none).
        var customerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim();
        string? uhid = null;
        var phone = request.CustomerPhone;

        if (request.PatientId is { } patientId)
        {
            var patientResult = await _patientService.GetByIdAsync(patientId, cancellationToken);
            if (!patientResult.IsSuccess)
            {
                return Fail(PharmacyErrorCodes.InvalidPatient, "The patient was not found.");
            }

            var patient = patientResult.Value!;
            customerName = $"{patient.FirstName} {patient.LastName}".Trim();
            uhid = patient.Uhid;
            phone = string.IsNullOrWhiteSpace(phone) ? patient.PrimaryPhone : phone;
        }

        if (request.ChargeToAdmission || request.AdmissionId.HasValue)
        {
            if (request.PatientId is null || request.AdmissionId is null)
            {
                return Fail(PharmacyErrorCodes.InvalidAdmission, "Choose the admitted patient this bill is for.");
            }

            var admissionProblem = await _admissionChargePoster.ValidateAsync(request.AdmissionId.Value, request.PatientId.Value, cancellationToken);
            if (admissionProblem is not null)
            {
                return Fail(PharmacyErrorCodes.InvalidAdmission, admissionProblem);
            }
        }

        if (request.IndentId is { } requestedIndentId)
        {
            var indent = await _indentRepository.GetByIdAsync(requestedIndentId, cancellationToken);
            if (indent is null)
            {
                return Fail(PharmacyErrorCodes.NotFound, "The indent was not found.");
            }

            if (!indent.IsOpen)
            {
                return Fail(PharmacyErrorCodes.InvalidState, $"Indent {indent.IndentNo} is already {indent.Status.ToString().ToLowerInvariant()}.");
            }

            if (indent.PatientId != request.PatientId)
            {
                return Fail(PharmacyErrorCodes.InvalidState, $"Indent {indent.IndentNo} is for {indent.PatientName}, not the patient on this bill.");
            }
        }

        var products = await _productService.GetByIdsAsync(request.Lines.Select(l => l.ProductId).Distinct().ToList(), cancellationToken);
        var stock = await _stockReader.ReadAsync(products, store.Id, cancellationToken);

        var linesResult = SalePlanner.PlanLines(request.Lines, stock, request.DiscountPercent);
        if (!linesResult.IsSuccess)
        {
            return Fail(linesResult.ErrorCode!, linesResult.Error!);
        }

        var lines = linesResult.Value!;

        var registerDrug = lines.FirstOrDefault(l => RegisterSchedules.Contains(l.Product.Schedule));
        if (registerDrug is not null && (customerName is null || string.IsNullOrWhiteSpace(request.DoctorName)))
        {
            return Fail(
                PharmacyErrorCodes.PrescriptionDetailsRequired,
                $"{registerDrug.Product.ProductName} is a Schedule {ScheduleName(registerDrug.Product.Schedule)} drug: enter the patient's name and the prescribing doctor before billing it.");
        }

        var netAmount = SalePlanner.NetAmount(lines);
        var settled = SalePlanner.SettlePayments(netAmount, request.Payments);
        if (!settled.IsSuccess)
        {
            return Fail(settled.ErrorCode!, settled.Error!);
        }

        var (payments, change, due) = settled.Value;
        if (due > 0)
        {
            if (!request.AllowCredit && !request.ChargeToAdmission)
            {
                return Fail(PharmacyErrorCodes.PaymentShort, $"The bill is {netAmount:0.00} and {netAmount - due:0.00} was received — {due:0.00} short.");
            }

            if (request.PatientId is null)
            {
                return Fail(PharmacyErrorCodes.CustomerRequired, "Credit can only be given to a registered patient. A walk-in customer pays in full.");
            }
        }

        var billNo = await _unitOfWork.NextNumberAsync(DocumentSeries.Sale, cancellationToken);

        var result = await _unitOfWork.ExecuteAsync(
            async () =>
            {
                var sale = PharmacySale.Create(
                    billNo, store.Id, request.PatientId, customerName ?? WalkInName, uhid, phone, request.DoctorName,
                    request.AdmissionId, request.PrescriptionId, request.IndentId, request.Remarks, actorId);

                foreach (var line in lines)
                {
                    sale.AddLine(
                        line.Product.Id, line.Product.ProductName, line.Product.GenericName, line.Product.HsnCode, line.Product.Schedule,
                        line.Batch.Id, line.Batch.BatchNo, line.Batch.ExpiryDate, line.Quantity, line.UnitPrice, line.UnitCost,
                        line.DiscountPercent, line.GstRatePercent, line.Amount.Gross, line.Amount.Discount, line.Amount.Taxable, line.Amount.Tax);

                    var posted = await _stockPoster.PostAsync(
                        new StockMovement(
                            TransactionType.Sale, store.Id, line.Product.Id, line.Batch.Id, line.Quantity,
                            UnitCost: line.UnitCost, UnitPrice: line.UnitPrice,
                            SourceType: StockSourceType.Sale, SourceId: sale.Id, SourceNo: billNo,
                            PatientId: request.PatientId, AdmissionId: request.AdmissionId,
                            Label: $"{line.Product.ProductName} (batch {line.Batch.BatchNo})"),
                        actorId,
                        cancellationToken);

                    if (!posted.IsSuccess)
                    {
                        return Result<PharmacySale>.Failure(posted.ErrorCode!, posted.Error!);
                    }
                }

                sale.Close();

                foreach (var payment in payments)
                {
                    sale.AddPayment(payment.Method, payment.Amount, payment.ReferenceNo, actorId);
                }

                if (request.ChargeToAdmission && sale.BalanceDue > 0)
                {
                    sale.ChargeToAdmission(actorId);
                }

                await _repository.AddAsync(sale, cancellationToken);

                if (request.IndentId is { } indentId)
                {
                    // Read inside the unit of work: on a retry the tracked indent is gone.
                    var indent = await _indentRepository.GetByIdAsync(indentId, cancellationToken);
                    if (indent is { IsOpen: true })
                    {
                        foreach (var issued in lines.GroupBy(l => l.Product.Id))
                        {
                            indent.RecordIssue(issued.Key, issued.Sum(l => l.Quantity), actorId);
                        }
                    }
                }

                if (request.HeldBillId is { } heldBillId && await _repository.GetHeldAsync(heldBillId, cancellationToken) is { } held)
                {
                    _repository.RemoveHeld(held);
                }

                return Result<PharmacySale>.Success(sale);
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            return Fail(result.ErrorCode!, result.Error!);
        }

        var saved = result.Value!;
        if (saved.ChargedToAdmission)
        {
            // After the bill is safely saved, and best-effort: the medicine has left the
            // counter either way, and a charge that failed to post is visible on the bill
            // (still "charged to admission", no IPD charge) for the IP billing desk to add.
            await _admissionChargePoster.PostSaleAsync(saved.AdmissionId!.Value, saved.BillNo, saved.BalanceDue, actorId, cancellationToken);
        }

        return Result<SaleResponse>.Success(ToResponse(saved, store.Name, saved.Lines.Count, includeDetail: true) with { ChangeGiven = change });
    }

    public async Task<Result<SaleResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sale = await _repository.GetByIdAsync(id, cancellationToken);
        if (sale is null)
        {
            return Fail(PharmacyErrorCodes.NotFound, "The bill was not found.");
        }

        var store = await _storeRepository.GetByIdAsync(sale.StoreId, cancellationToken);
        return Result<SaleResponse>.Success(ToResponse(sale, store?.Name ?? string.Empty, sale.Lines.Count, includeDetail: true));
    }

    public async Task<PagedResult<SaleResponse>> GetPagedAsync(SaleListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(query, cancellationToken);
        var storeNames = (await _storeRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);

        var responses = items
            .Select(i => ToResponse(i.Sale, storeNames.GetValueOrDefault(i.Sale.StoreId) ?? string.Empty, i.LineCount, includeDetail: false))
            .ToList();

        return new PagedResult<SaleResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    public async Task<PatientPharmacyBillsResponse> GetPatientBillsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var sales = await _repository.GetForPatientAsync(patientId, MaxPatientBills, cancellationToken);
        if (sales.Count == 0)
        {
            return new PatientPharmacyBillsResponse();
        }

        var storeNames = (await _storeRepository.GetAllAsync(cancellationToken)).ToDictionary(s => s.Id, s => s.Name);
        var returns = await _repository.GetReturnsForSalesAsync(sales.Select(s => s.Id).ToList(), cancellationToken);

        return new PatientPharmacyBillsResponse
        {
            Bills = sales.Select(s => ToResponse(s, storeNames.GetValueOrDefault(s.StoreId) ?? string.Empty, s.Lines.Count, includeDetail: true)).ToList(),
            Returns = returns.Select(ToResponse).ToList(),
        };
    }

    public async Task<Result<SaleResponse>> CollectPaymentAsync(Guid saleId, CollectSalePaymentRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var sale = await _repository.GetByIdAsync(saleId, cancellationToken);
        if (sale is null)
        {
            return Fail(PharmacyErrorCodes.NotFound, "The bill was not found.");
        }

        if (sale.ChargedToAdmission)
        {
            return Fail(PharmacyErrorCodes.InvalidState, $"Bill {sale.BillNo} is charged to the patient's admission and is settled with the hospital bill.");
        }

        if (request.Amount > sale.BalanceDue)
        {
            return Fail(PharmacyErrorCodes.InvalidPayment, $"Only {Math.Max(sale.BalanceDue, 0m):0.00} is due on bill {sale.BillNo}.");
        }

        sale.AddPayment(request.Method, request.Amount, request.ReferenceNo, actorId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var store = await _storeRepository.GetByIdAsync(sale.StoreId, cancellationToken);
        return Result<SaleResponse>.Success(ToResponse(sale, store?.Name ?? string.Empty, sale.Lines.Count, includeDetail: true));
    }

    public async Task<Result<SaleReturnResponse>> CreateReturnAsync(Guid saleId, CreateSaleReturnRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByIdAsync(saleId, cancellationToken);
        if (existing is null)
        {
            return FailReturn(PharmacyErrorCodes.NotFound, "The bill was not found.");
        }

        foreach (var requested in request.Lines)
        {
            var saleLine = existing.Lines.FirstOrDefault(l => l.Id == requested.SaleLineId);
            if (saleLine is null)
            {
                return FailReturn(PharmacyErrorCodes.InvalidQuantity, $"That line is not on bill {existing.BillNo}.");
            }

            if (requested.Quantity > saleLine.ReturnableQuantity)
            {
                return FailReturn(
                    PharmacyErrorCodes.InvalidQuantity,
                    $"Only {saleLine.ReturnableQuantity:0.####} of {saleLine.ProductName} is left to return on bill {existing.BillNo}.");
            }
        }

        var returnNo = await _unitOfWork.NextNumberAsync(DocumentSeries.SaleReturn, cancellationToken);

        var result = await _unitOfWork.ExecuteAsync(
            async () =>
            {
                // Read again inside the unit of work: on a retry the tracked copy is gone, and
                // the quantities already returned may have moved.
                var sale = await _repository.GetByIdAsync(saleId, cancellationToken);
                if (sale is null)
                {
                    return Result<PharmacySaleReturn>.Failure(PharmacyErrorCodes.NotFound, "The bill was not found.");
                }

                var saleReturn = PharmacySaleReturn.Create(returnNo, sale, request.RefundMethod, request.Restock, request.Reason, actorId);

                foreach (var requested in request.Lines)
                {
                    var saleLine = sale.Lines.First(l => l.Id == requested.SaleLineId);
                    if (requested.Quantity > saleLine.ReturnableQuantity)
                    {
                        return Result<PharmacySaleReturn>.Failure(
                            PharmacyErrorCodes.InvalidQuantity,
                            $"Only {saleLine.ReturnableQuantity:0.####} of {saleLine.ProductName} is left to return on bill {sale.BillNo}.");
                    }

                    // Credited at what was actually paid for these units: the same share of
                    // the line's taxable amount and tax as of its quantity.
                    var share = requested.Quantity / saleLine.Quantity;
                    var taxable = GstMath.Round2(saleLine.TaxableAmount * share);
                    var tax = GstMath.Round2(saleLine.TaxAmount * share);

                    saleReturn.AddLine(saleLine, requested.Quantity, taxable, tax);
                    saleLine.RecordReturn(requested.Quantity, actorId);

                    if (request.Restock)
                    {
                        var posted = await _stockPoster.PostAsync(
                            new StockMovement(
                                TransactionType.SaleReturn, sale.StoreId, saleLine.ProductId, saleLine.ProductBatchId, requested.Quantity,
                                UnitCost: saleLine.UnitCost, UnitPrice: saleLine.UnitPrice,
                                SourceType: StockSourceType.SaleReturn, SourceId: saleReturn.Id, SourceNo: returnNo,
                                PatientId: sale.PatientId, AdmissionId: sale.AdmissionId,
                                Remarks: $"Return against {sale.BillNo}"),
                            actorId,
                            cancellationToken);

                        if (!posted.IsSuccess)
                        {
                            return Result<PharmacySaleReturn>.Failure(posted.ErrorCode!, posted.Error!);
                        }
                    }
                }

                // Settled in whole rupees like the bill was, and never for more than is left
                // of the bill — rounding each return up must not refund more than was charged.
                var value = Math.Min(GstMath.RoundToRupee(saleReturn.LinesTotal).Net, sale.NetAmount - sale.ReturnedAmount);
                if (value <= 0)
                {
                    return Result<PharmacySaleReturn>.Failure(PharmacyErrorCodes.InvalidQuantity, "There is nothing left to refund on this bill.");
                }

                saleReturn.Settle(value, sale.ApplyReturn(value, actorId));
                await _repository.AddReturnAsync(saleReturn, cancellationToken);
                return Result<PharmacySaleReturn>.Success(saleReturn);
            },
            cancellationToken);

        if (!result.IsSuccess)
        {
            return FailReturn(result.ErrorCode!, result.Error!);
        }

        var saved = result.Value!;
        if (existing.ChargedToAdmission)
        {
            // The whole value came off what the admission owes, so the IP bill must drop by it.
            await _admissionChargePoster.PostSaleReturnAsync(existing.AdmissionId!.Value, saved.ReturnNo, existing.BillNo, saved.ReturnAmount - saved.RefundAmount, actorId, cancellationToken);
        }

        return Result<SaleReturnResponse>.Success(ToResponse(saved));
    }

    public async Task<Result<SaleReturnResponse>> GetReturnByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var saleReturn = await _repository.GetReturnByIdAsync(id, cancellationToken);
        return saleReturn is null
            ? FailReturn(PharmacyErrorCodes.NotFound, "The return was not found.")
            : Result<SaleReturnResponse>.Success(ToResponse(saleReturn));
    }

    public async Task<PagedResult<SaleReturnResponse>> GetReturnsPagedAsync(SaleReturnListQuery query, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetReturnsPagedAsync(query, cancellationToken);
        return new PagedResult<SaleReturnResponse>(items.Select(ToResponse).ToList(), query.Page, query.PageSize, totalCount);
    }

    private static string ScheduleName(DrugSchedule schedule) => schedule == DrugSchedule.Narcotic ? "X / NDPS" : schedule.ToString();

    private static Result<SaleResponse> Fail(string code, string message) => Result<SaleResponse>.Failure(code, message);

    private static Result<SaleReturnResponse> FailReturn(string code, string message) => Result<SaleReturnResponse>.Failure(code, message);

    internal static SaleResponse ToResponse(PharmacySale sale, string storeName, int lineCount, bool includeDetail) => new()
    {
        Id = sale.Id,
        BillNo = sale.BillNo,
        StoreId = sale.StoreId,
        StoreName = storeName,
        SaleDate = sale.SaleDate,
        PatientId = sale.PatientId,
        CustomerName = sale.CustomerName,
        PatientUhid = sale.PatientUhid,
        CustomerPhone = sale.CustomerPhone,
        DoctorName = sale.DoctorName,
        AdmissionId = sale.AdmissionId,
        PrescriptionId = sale.PrescriptionId,
        IndentId = sale.IndentId,
        GrossAmount = sale.GrossAmount,
        DiscountAmount = sale.DiscountAmount,
        TaxableAmount = sale.TaxableAmount,
        TaxAmount = sale.TaxAmount,
        RoundOff = sale.RoundOff,
        NetAmount = sale.NetAmount,
        PaidAmount = sale.PaidAmount,
        RefundedAmount = sale.RefundedAmount,
        ReturnedAmount = sale.ReturnedAmount,
        BalanceDue = sale.ChargedToAdmission ? 0m : Math.Max(sale.BalanceDue, 0m),
        ChargedToAdmission = sale.ChargedToAdmission,
        Remarks = sale.Remarks,
        LineCount = lineCount,
        CreatedAt = sale.CreatedAt,
        Lines = includeDetail
            ? sale.Lines.Select(l => new SaleLineResponse
            {
                Id = l.Id,
                LineNo = l.LineNo,
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                GenericName = l.GenericName,
                HsnCode = l.HsnCode,
                Schedule = l.Schedule,
                ProductBatchId = l.ProductBatchId,
                BatchNo = l.BatchNo,
                ExpiryDate = l.ExpiryDate,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                GstRatePercent = l.GstRatePercent,
                GrossAmount = l.GrossAmount,
                DiscountAmount = l.DiscountAmount,
                TaxableAmount = l.TaxableAmount,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal,
                ReturnedQuantity = l.ReturnedQuantity,
            }).ToList()
            : [],
        Payments = includeDetail
            ? sale.Payments.Select(p => new SalePaymentResponse { Id = p.Id, Method = p.Method, Amount = p.Amount, ReferenceNo = p.ReferenceNo, PaidAt = p.PaidAt }).ToList()
            : [],
    };

    private static SaleReturnResponse ToResponse(PharmacySaleReturn saleReturn) => new()
    {
        Id = saleReturn.Id,
        ReturnNo = saleReturn.ReturnNo,
        SaleId = saleReturn.SaleId,
        BillNo = saleReturn.BillNo,
        StoreId = saleReturn.StoreId,
        ReturnDate = saleReturn.ReturnDate,
        CustomerName = saleReturn.CustomerName,
        TaxableAmount = saleReturn.TaxableAmount,
        TaxAmount = saleReturn.TaxAmount,
        ReturnAmount = saleReturn.ReturnAmount,
        RefundAmount = saleReturn.RefundAmount,
        RefundMethod = saleReturn.RefundMethod,
        Restocked = saleReturn.Restocked,
        Reason = saleReturn.Reason,
        Lines = saleReturn.Lines.Select(l => new SaleReturnLineResponse
        {
            Id = l.Id,
            SaleLineId = l.SaleLineId,
            ProductId = l.ProductId,
            ProductName = l.ProductName,
            BatchNo = l.BatchNo,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            GstRatePercent = l.GstRatePercent,
            TaxableAmount = l.TaxableAmount,
            TaxAmount = l.TaxAmount,
            LineTotal = l.LineTotal,
        }).ToList(),
    };
}
