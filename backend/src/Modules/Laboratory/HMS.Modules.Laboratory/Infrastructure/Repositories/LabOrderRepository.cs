using HMS.Modules.Laboratory.Application.Abstractions;
using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Laboratory.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Laboratory.Infrastructure.Repositories;

internal class LabOrderRepository : ILabOrderRepository
{
    private readonly LaboratoryDbContext _dbContext;

    public LabOrderRepository(LaboratoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(LabOrder order, CancellationToken cancellationToken)
        => await _dbContext.LabOrders.AddAsync(order, cancellationToken);

    public Task<LabOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => FullGraph().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<LabOrder?> GetByInvoiceIdAsync(Guid invoiceId, CancellationToken cancellationToken)
        => FullGraph().FirstOrDefaultAsync(o => o.InvoiceId == invoiceId, cancellationToken);

    public Task<LabOrder?> GetByItemIdAsync(Guid itemId, CancellationToken cancellationToken)
        => FullGraph().FirstOrDefaultAsync(o => o.Items.Any(i => i.Id == itemId), cancellationToken);

    public async Task<(IReadOnlyList<LabOrder> Items, int TotalCount)> GetPagedAsync(LabOrderListQuery query, CancellationToken cancellationToken)
    {
        var orders = FullGraph();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            orders = orders.Where(o =>
                EF.Functions.ILike(o.PatientName, term) ||
                EF.Functions.ILike(o.PatientUhid, term) ||
                EF.Functions.ILike(o.LabOrderNumber, term));
        }

        if (query.Priority.HasValue)
        {
            orders = orders.Where(o => o.Priority == query.Priority.Value);
        }

        if (query.DateFrom.HasValue)
        {
            orders = orders.Where(o => o.CreatedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            orders = orders.Where(o => o.CreatedAt <= query.DateTo.Value);
        }

        if (query.Status.HasValue)
        {
            orders = ApplyStatusFilter(orders, query.Status.Value);
        }

        orders = ApplySort(orders, query.Sort);

        var totalCount = await orders.CountAsync(cancellationToken);

        var items = await orders
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<LabOrder>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken)
        => await FullGraph()
            .Where(o => o.PatientId == patientId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<LabDashboardSummaryResponse> GetDashboardCountsAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var items = _dbContext.LabOrderItems.AsQueryable();
        var orders = _dbContext.LabOrders.AsQueryable();

        return new LabDashboardSummaryResponse
        {
            TotalRequestsToday = await orders.CountAsync(o => o.CreatedAt >= today && o.CreatedAt < tomorrow, cancellationToken),
            PendingSampleCollection = await items.CountAsync(i => i.Status == LabOrderItemStatus.PendingCollection, cancellationToken),
            SamplesCollected = await items.CountAsync(i => i.Status == LabOrderItemStatus.Collected, cancellationToken),
            SamplesReceived = await items.CountAsync(i => i.Status == LabOrderItemStatus.Received, cancellationToken),
            TestsInProgress = await items.CountAsync(i => i.Status == LabOrderItemStatus.Processing, cancellationToken),
            ResultsPendingEntry = await items.CountAsync(i => i.Status == LabOrderItemStatus.ResultEntryInProgress, cancellationToken),
            PendingVerification = await items.CountAsync(i => i.Status == LabOrderItemStatus.PendingVerification, cancellationToken),
            ReportsReady = await orders.CountAsync(o => o.ReportGeneratedAt != null && o.ReportReleasedAt == null, cancellationToken),
            ReportsReleased = await orders.CountAsync(o => o.ReportReleasedAt != null, cancellationToken),
            RejectedOrRecollectionRequired = await items.CountAsync(
                i => i.Status == LabOrderItemStatus.Rejected || i.Status == LabOrderItemStatus.RecollectionRequired,
                cancellationToken),
        };
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<LabOrder> FullGraph()
        => _dbContext.LabOrders
            .Include(o => o.Items).ThenInclude(i => i.Parameters)
            .Include(o => o.Items).ThenInclude(i => i.Events);

    private static IQueryable<LabOrder> ApplySort(IQueryable<LabOrder> orders, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return orders.OrderByDescending(o => o.CreatedAt);
        }

        var descending = sort.StartsWith('-');
        var field = descending ? sort[1..] : sort;

        return field.ToLowerInvariant() switch
        {
            "patientname" => descending ? orders.OrderByDescending(o => o.PatientName) : orders.OrderBy(o => o.PatientName),
            "laborder number" or "labordernumber" => descending ? orders.OrderByDescending(o => o.LabOrderNumber) : orders.OrderBy(o => o.LabOrderNumber),
            "priority" => descending ? orders.OrderByDescending(o => o.Priority) : orders.OrderBy(o => o.Priority),
            "createdat" => descending ? orders.OrderByDescending(o => o.CreatedAt) : orders.OrderBy(o => o.CreatedAt),
            _ => orders.OrderByDescending(o => o.CreatedAt),
        };
    }

    /// <summary>Reproduces Domain/LabOrder.cs's OverallStatus precedence ladder as a query
    /// predicate (first-match-wins over Items, so each case must also exclude every
    /// higher-precedence condition) — a computed C# property can't be translated by EF Core
    /// directly, so filtering by the worklist's Status query param needs its own expression,
    /// kept in exact lock-step with the entity's own getter.</summary>
    private static IQueryable<LabOrder> ApplyStatusFilter(IQueryable<LabOrder> orders, LabOrderStatus status)
    {
        return status switch
        {
            LabOrderStatus.Released => orders.Where(o => o.ReportReleasedAt != null),

            LabOrderStatus.ReadyForRelease => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt != null),

            LabOrderStatus.RecollectionRequired => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired)),

            LabOrderStatus.Rejected => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected)),

            LabOrderStatus.CorrectionRequired => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected) &&
                o.Items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired)),

            LabOrderStatus.Verified => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired) &&
                o.Items.Any() && o.Items.All(i => i.Status == LabOrderItemStatus.Verified)),

            LabOrderStatus.PendingVerification => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired) &&
                !(o.Items.Any() && o.Items.All(i => i.Status == LabOrderItemStatus.Verified)) &&
                o.Items.Any(i => i.Status == LabOrderItemStatus.PendingVerification)),

            LabOrderStatus.ResultEntryInProgress => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired) &&
                !(o.Items.Any() && o.Items.All(i => i.Status == LabOrderItemStatus.Verified)) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.PendingVerification) &&
                o.Items.Any(i => i.Status == LabOrderItemStatus.ResultEntryInProgress)),

            LabOrderStatus.Processing => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired) &&
                !(o.Items.Any() && o.Items.All(i => i.Status == LabOrderItemStatus.Verified)) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.PendingVerification) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.ResultEntryInProgress) &&
                o.Items.Any(i => i.Status == LabOrderItemStatus.Processing)),

            LabOrderStatus.Received => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired) &&
                !(o.Items.Any() && o.Items.All(i => i.Status == LabOrderItemStatus.Verified)) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.PendingVerification) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.ResultEntryInProgress) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Processing) &&
                o.Items.All(i => i.Status != LabOrderItemStatus.PendingCollection && i.Status != LabOrderItemStatus.Collected)),

            LabOrderStatus.Collected => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired) &&
                !(o.Items.Any() && o.Items.All(i => i.Status == LabOrderItemStatus.Verified)) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.PendingVerification) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.ResultEntryInProgress) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Processing) &&
                !o.Items.All(i => i.Status != LabOrderItemStatus.PendingCollection && i.Status != LabOrderItemStatus.Collected) &&
                o.Items.Any(i => i.Status == LabOrderItemStatus.Collected)),

            LabOrderStatus.PendingCollection => orders.Where(o =>
                o.ReportReleasedAt == null && o.ReportGeneratedAt == null &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.RecollectionRequired) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Rejected) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.CorrectionRequired) &&
                !(o.Items.Any() && o.Items.All(i => i.Status == LabOrderItemStatus.Verified)) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.PendingVerification) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.ResultEntryInProgress) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Processing) &&
                !o.Items.All(i => i.Status != LabOrderItemStatus.PendingCollection && i.Status != LabOrderItemStatus.Collected) &&
                !o.Items.Any(i => i.Status == LabOrderItemStatus.Collected)),

            _ => orders,
        };
    }
}
