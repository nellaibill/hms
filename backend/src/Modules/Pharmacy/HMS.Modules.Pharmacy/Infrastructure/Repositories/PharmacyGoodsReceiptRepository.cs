using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacyGoodsReceiptRepository : IPharmacyGoodsReceiptRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacyGoodsReceiptRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PharmacyGoodsReceipt receipt, CancellationToken cancellationToken)
        => await _dbContext.GoodsReceipts.AddAsync(receipt, cancellationToken);

    public Task<PharmacyGoodsReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.GoodsReceipts.Include(r => r.Lines.OrderBy(l => l.LineNo)).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<bool> ExistsForSupplierInvoiceAsync(Guid supplierId, string supplierInvoiceNo, CancellationToken cancellationToken)
        => _dbContext.GoodsReceipts.AnyAsync(r => r.SupplierId == supplierId && r.SupplierInvoiceNo.ToLower() == supplierInvoiceNo.ToLower(), cancellationToken);

    public async Task<(IReadOnlyList<(PharmacyGoodsReceipt Receipt, int LineCount)> Items, int TotalCount)> GetPagedAsync(GoodsReceiptListQuery query, CancellationToken cancellationToken)
    {
        var receipts = _dbContext.GoodsReceipts.AsNoTracking();

        if (query.SupplierId.HasValue)
        {
            receipts = receipts.Where(r => r.SupplierId == query.SupplierId.Value);
        }

        if (query.StoreId.HasValue)
        {
            receipts = receipts.Where(r => r.StoreId == query.StoreId.Value);
        }

        if (query.FromDate.HasValue)
        {
            receipts = receipts.Where(r => r.ReceivedDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            receipts = receipts.Where(r => r.ReceivedDate <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            receipts = receipts.Where(r =>
                EF.Functions.ILike(r.ReceiptNo, term) || EF.Functions.ILike(r.SupplierInvoiceNo, term) || EF.Functions.ILike(r.SupplierName, term));
        }

        var totalCount = await receipts.CountAsync(cancellationToken);
        var page = await receipts
            .OrderByDescending(r => r.ReceivedDate).ThenByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(r => new { Receipt = r, LineCount = r.Lines.Count })
            .ToListAsync(cancellationToken);

        return (page.Select(p => (p.Receipt, p.LineCount)).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<PharmacyGoodsReceiptLine>> GetLinesForBatchAsync(Guid productBatchId, CancellationToken cancellationToken)
        => await _dbContext.GoodsReceiptLines.Where(l => l.ProductBatchId == productBatchId).OrderByDescending(l => l.CreatedAt).ToListAsync(cancellationToken);
}
