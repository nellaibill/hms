using FluentValidation;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Application.Validators;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Infrastructure;
using HMS.Modules.Pharmacy.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Pharmacy;

public static class PharmacyModule
{
    public static IServiceCollection AddPharmacyModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale (mirrors HMS.Modules.IPD.IPDModule's registration exactly).
        services.AddDbContext<PharmacyDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "PharmacyDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", PharmacyDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IPharmacyStoreRepository, PharmacyStoreRepository>();
        services.AddScoped<IPharmacyStockBalanceRepository, PharmacyStockBalanceRepository>();
        services.AddScoped<IPharmacyStockTransactionRepository, PharmacyStockTransactionRepository>();

        services.AddScoped<IPharmacyGoodsReceiptRepository, PharmacyGoodsReceiptRepository>();
        services.AddScoped<IPharmacySaleRepository, PharmacySaleRepository>();
        services.AddScoped<IPharmacySettingsRepository, PharmacySettingsRepository>();
        services.AddScoped<IPharmacyProcurementRepository, PharmacyProcurementRepository>();
        services.AddScoped<IStockPoster, StockPoster>();
        services.AddScoped<ISellableStockReader, SellableStockReader>();
        services.AddScoped<IAdmissionChargePoster, AdmissionChargePoster>();
        services.AddScoped<IPharmacyUnitOfWork, PharmacyUnitOfWork>();

        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();
        services.AddScoped<IProductImportService, ProductImportService>();
        services.AddScoped<ICounterService, CounterService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
        services.AddScoped<ISupplierAccountService, SupplierAccountService>();
        services.AddScoped<IStockReceiptService, StockReceiptService>();
        services.AddScoped<IDispenseService, DispenseService>();
        services.AddScoped<IStockBalanceService, StockBalanceService>();
        services.AddScoped<IStockLedgerService, StockLedgerService>();

        // Explicit registration, not AddValidatorsFromAssemblyContaining — that scanner only
        // finds *public* IValidator<T> implementations, and this module's validators are
        // internal by design, so it would silently register nothing
        // (docs/DeveloperHandbook.md §8/§20 — the single highest-impact bug hit building the
        // Users module).
        services.AddScoped<IValidator<CreateStoreRequest>, CreateStoreRequestValidator>();
        services.AddScoped<IValidator<UpdateStoreRequest>, UpdateStoreRequestValidator>();
        services.AddScoped<IValidator<CreateStockReceiptRequest>, CreateStockReceiptRequestValidator>();
        services.AddScoped<IValidator<CreateGoodsReceiptRequest>, CreateGoodsReceiptRequestValidator>();
        services.AddScoped<IValidator<ImportProductsRequest>, ImportProductsRequestValidator>();
        services.AddScoped<IValidator<CreateSaleRequest>, CreateSaleRequestValidator>();
        services.AddScoped<IValidator<CollectSalePaymentRequest>, CollectSalePaymentRequestValidator>();
        services.AddScoped<IValidator<CreateSaleReturnRequest>, CreateSaleReturnRequestValidator>();
        services.AddScoped<IValidator<HoldBillRequest>, HoldBillRequestValidator>();
        services.AddScoped<IValidator<UpdatePharmacySettingsRequest>, UpdatePharmacySettingsRequestValidator>();
        services.AddScoped<IValidator<SavePurchaseOrderRequest>, SavePurchaseOrderRequestValidator>();
        services.AddScoped<IValidator<CreatePurchaseReturnRequest>, CreatePurchaseReturnRequestValidator>();
        services.AddScoped<IValidator<CreateSupplierPaymentRequest>, CreateSupplierPaymentRequestValidator>();
        services.AddScoped<IValidator<CreateDispenseRequest>, CreateDispenseRequestValidator>();
        services.AddScoped<IValidator<CreateDispenseCartRequest>, CreateDispenseCartRequestValidator>();

        return services;
    }
}
