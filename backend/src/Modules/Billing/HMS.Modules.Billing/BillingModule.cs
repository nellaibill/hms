using FluentValidation;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Application.Abstractions;
using HMS.Modules.Billing.Application.Validators;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Infrastructure;
using HMS.Modules.Billing.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HMS.Modules.Billing;

public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale.
        services.AddDbContext<BillingDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "BillingDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", BillingDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoiceRevertRequestRepository, InvoiceRevertRequestRepository>();
        services.AddScoped<IInvoiceRevertService, InvoiceRevertService>();
        services.AddScoped<IInvoiceDiscountRequestRepository, InvoiceDiscountRequestRepository>();
        services.AddScoped<IInvoiceDiscountService, InvoiceDiscountService>();
        // The revert service's "made today" rule reads the clock through this, so tests can move it.
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IValidator<CreateInvoiceRequest>, CreateInvoiceRequestValidator>();
        services.AddScoped<IValidator<VoidInvoiceRequest>, VoidInvoiceRequestValidator>();
        services.AddScoped<IValidator<CreateInvoiceRevertRequest>, CreateInvoiceRevertRequestValidator>();
        services.AddScoped<IValidator<ApproveInvoiceRevertRequest>, ApproveInvoiceRevertRequestValidator>();
        services.AddScoped<IValidator<RejectInvoiceRevertRequest>, RejectInvoiceRevertRequestValidator>();
        services.AddScoped<IValidator<CreateInvoiceDiscountRequest>, CreateInvoiceDiscountRequestValidator>();
        services.AddScoped<IValidator<ApproveInvoiceDiscountRequest>, ApproveInvoiceDiscountRequestValidator>();
        services.AddScoped<IValidator<RejectInvoiceDiscountRequest>, RejectInvoiceDiscountRequestValidator>();
        services.AddScoped<IValidator<PayInvoiceRequest>, PayInvoiceRequestValidator>();

        return services;
    }
}
