using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacySaleRepository : IPharmacySaleRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacySaleRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PharmacySale sale, CancellationToken cancellationToken)
        => await _dbContext.Sales.AddAsync(sale, cancellationToken);

    public Task<PharmacySale?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Sales
            .Include(s => s.Lines.OrderBy(l => l.LineNo))
            .Include(s => s.Payments.OrderBy(p => p.PaidAt))
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<(PharmacySale Sale, int LineCount)> Items, int TotalCount)> GetPagedAsync(SaleListQuery query, CancellationToken cancellationToken)
    {
        var sales = _dbContext.Sales.AsNoTracking();

        if (query.StoreId.HasValue)
        {
            sales = sales.Where(s => s.StoreId == query.StoreId.Value);
        }

        if (query.PatientId.HasValue)
        {
            sales = sales.Where(s => s.PatientId == query.PatientId.Value);
        }

        if (query.FromDate.HasValue)
        {
            var from = PharmacyClock.StartOfDayUtc(query.FromDate.Value);
            sales = sales.Where(s => s.SaleDate >= from);
        }

        if (query.ToDate.HasValue)
        {
            var to = PharmacyClock.StartOfDayUtc(query.ToDate.Value.AddDays(1));
            sales = sales.Where(s => s.SaleDate < to);
        }

        if (query.HasDue == true)
        {
            sales = sales.Where(s => s.NetAmount - s.ReturnedAmount - (s.PaidAmount - s.RefundedAmount) > 0 && !s.ChargedToAdmission);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            sales = sales.Where(s =>
                EF.Functions.ILike(s.BillNo, term)
                || EF.Functions.ILike(s.CustomerName, term)
                || (s.CustomerPhone != null && EF.Functions.ILike(s.CustomerPhone, term))
                || (s.PatientUhid != null && EF.Functions.ILike(s.PatientUhid, term)));
        }

        var totalCount = await sales.CountAsync(cancellationToken);
        var page = await sales
            .OrderByDescending(s => s.SaleDate)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(s => new { Sale = s, LineCount = s.Lines.Count })
            .ToListAsync(cancellationToken);

        return (page.Select(p => (p.Sale, p.LineCount)).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<PharmacySale>> GetForPatientAsync(Guid patientId, int max, CancellationToken cancellationToken)
        => await _dbContext.Sales.AsNoTracking()
            .Where(s => s.PatientId == patientId)
            .OrderByDescending(s => s.SaleDate)
            .Take(max)
            .Include(s => s.Lines.OrderBy(l => l.LineNo))
            .Include(s => s.Payments.OrderBy(p => p.PaidAt))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PharmacySaleReturn>> GetReturnsForSalesAsync(IReadOnlyCollection<Guid> saleIds, CancellationToken cancellationToken)
    {
        if (saleIds.Count == 0)
        {
            return [];
        }

        var ids = saleIds.Distinct().ToList();
        return await _dbContext.SaleReturns.AsNoTracking()
            .Where(r => ids.Contains(r.SaleId))
            .OrderByDescending(r => r.ReturnDate)
            .Include(r => r.Lines)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<PrescriptionBillResponse>>> GetBillsForPrescriptionsAsync(IReadOnlyCollection<Guid> prescriptionIds, CancellationToken cancellationToken)
    {
        if (prescriptionIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<PrescriptionBillResponse>>();
        }

        var ids = prescriptionIds.Distinct().Cast<Guid?>().ToList();
        var bills = await _dbContext.Sales.AsNoTracking()
            .Where(s => ids.Contains(s.PrescriptionId))
            .OrderBy(s => s.SaleDate)
            .Select(s => new { PrescriptionId = s.PrescriptionId!.Value, s.Id, s.BillNo, s.NetAmount })
            .ToListAsync(cancellationToken);

        return bills
            .GroupBy(b => b.PrescriptionId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PrescriptionBillResponse>)g.Select(b => new PrescriptionBillResponse { SaleId = b.Id, BillNo = b.BillNo, NetAmount = b.NetAmount }).ToList());
    }

    public async Task AddReturnAsync(PharmacySaleReturn saleReturn, CancellationToken cancellationToken)
        => await _dbContext.SaleReturns.AddAsync(saleReturn, cancellationToken);

    public Task<PharmacySaleReturn?> GetReturnByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.SaleReturns.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<PharmacySaleReturn> Items, int TotalCount)> GetReturnsPagedAsync(SaleReturnListQuery query, CancellationToken cancellationToken)
    {
        var returns = _dbContext.SaleReturns.AsNoTracking();

        if (query.StoreId.HasValue)
        {
            returns = returns.Where(r => r.StoreId == query.StoreId.Value);
        }

        if (query.SaleId.HasValue)
        {
            returns = returns.Where(r => r.SaleId == query.SaleId.Value);
        }

        if (query.FromDate.HasValue)
        {
            var from = PharmacyClock.StartOfDayUtc(query.FromDate.Value);
            returns = returns.Where(r => r.ReturnDate >= from);
        }

        if (query.ToDate.HasValue)
        {
            var to = PharmacyClock.StartOfDayUtc(query.ToDate.Value.AddDays(1));
            returns = returns.Where(r => r.ReturnDate < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            returns = returns.Where(r => EF.Functions.ILike(r.ReturnNo, term) || EF.Functions.ILike(r.BillNo, term) || EF.Functions.ILike(r.CustomerName, term));
        }

        var totalCount = await returns.CountAsync(cancellationToken);
        var items = await returns
            .Include(r => r.Lines)
            .OrderByDescending(r => r.ReturnDate)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddHeldAsync(PharmacyHeldBill heldBill, CancellationToken cancellationToken)
        => await _dbContext.HeldBills.AddAsync(heldBill, cancellationToken);

    public Task<PharmacyHeldBill?> GetHeldAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.HeldBills.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PharmacyHeldBill>> GetHeldForStoreAsync(Guid storeId, CancellationToken cancellationToken)
        => await _dbContext.HeldBills.AsNoTracking().Where(h => h.StoreId == storeId).OrderByDescending(h => h.CreatedAt).ToListAsync(cancellationToken);

    public void RemoveHeld(PharmacyHeldBill heldBill) => _dbContext.HeldBills.Remove(heldBill);
}
