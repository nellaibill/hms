using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using HMS.Modules.Pharmacy.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HMS.Modules.Pharmacy.Infrastructure.Repositories;

internal class PharmacyIndentRepository : IPharmacyIndentRepository
{
    private readonly PharmacyDbContext _dbContext;

    public PharmacyIndentRepository(PharmacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PharmacyIndent indent, CancellationToken cancellationToken)
        => await _dbContext.Indents.AddAsync(indent, cancellationToken);

    public Task<PharmacyIndent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.Indents.Include(i => i.Lines.OrderBy(l => l.LineNo)).FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<PharmacyIndent> Items, int TotalCount)> GetPagedAsync(IndentListQuery query, CancellationToken cancellationToken)
    {
        var indents = _dbContext.Indents.AsNoTracking();

        if (query.AdmissionId.HasValue)
        {
            indents = indents.Where(i => i.AdmissionId == query.AdmissionId.Value);
        }

        if (query.Status.HasValue)
        {
            indents = indents.Where(i => i.Status == query.Status.Value);
        }

        if (query.OpenOnly == true)
        {
            indents = indents.Where(i => i.Status == IndentStatus.Pending || i.Status == IndentStatus.PartlyIssued);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            indents = indents.Where(i =>
                EF.Functions.ILike(i.IndentNo, term) || EF.Functions.ILike(i.PatientName, term) || EF.Functions.ILike(i.AdmissionNumber, term)
                || (i.PatientUhid != null && EF.Functions.ILike(i.PatientUhid, term)));
        }

        var totalCount = await indents.CountAsync(cancellationToken);

        // Urgent first, then oldest first: the queue is worked from the top.
        var items = await indents
            .Include(i => i.Lines)
            .OrderByDescending(i => i.Status == IndentStatus.Pending || i.Status == IndentStatus.PartlyIssued)
            .ThenByDescending(i => i.IsUrgent)
            .ThenBy(i => i.RequestedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

internal class PharmacyIndentConfiguration : IEntityTypeConfiguration<PharmacyIndent>
{
    public void Configure(EntityTypeBuilder<PharmacyIndent> builder)
    {
        builder.Ignore(i => i.IsOpen);
        builder.MapDocument("pharmacy_indents");

        builder.HasIndex(i => i.IndentNo).IsUnique().HasDatabaseName("ux_pharmacy_indents_indent_no");
        builder.HasIndex(i => i.AdmissionId).HasDatabaseName("ix_pharmacy_indents_admission_id");
        builder.HasIndex(i => i.Status).HasDatabaseName("ix_pharmacy_indents_status");

        builder.HasOne<PharmacyStore>().WithMany().HasForeignKey(i => i.StoreId)
            .HasConstraintName("fk_pharmacy_indents_pharmacy_stores_store_id").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Lines).WithOne().HasForeignKey(l => l.IndentId)
            .HasConstraintName("fk_pharmacy_indent_lines_indent_id").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal class PharmacyIndentLineConfiguration : IEntityTypeConfiguration<PharmacyIndentLine>
{
    public void Configure(EntityTypeBuilder<PharmacyIndentLine> builder)
    {
        builder.Ignore(l => l.PendingQuantity);
        builder.MapDocument("pharmacy_indent_lines");

        builder.HasIndex(l => l.IndentId).HasDatabaseName("ix_pharmacy_indent_lines_indent_id");
    }
}
