using FluentValidation;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Validators;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Infrastructure;
using HMS.Modules.IPD.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.IPD;

public static class IPDModule
{
    public static IServiceCollection AddIPDModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<IPDDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IPDDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            }));

        services.AddScoped<IWardRepository, WardRepository>();
        services.AddScoped<IWardService, WardService>();

        services.AddScoped<IBedRepository, BedRepository>();
        services.AddScoped<IBedService, BedService>();

        services.AddScoped<IAdmissionRepository, AdmissionRepository>();
        services.AddScoped<IBedTransferHistoryRepository, BedTransferHistoryRepository>();
        services.AddScoped<IAdmissionBedStayRepository, AdmissionBedStayRepository>();
        services.AddScoped<IAdmissionIdentifierGenerator, AdmissionIdentifierGenerator>();
        services.AddScoped<IAdmissionService, AdmissionService>();

        services.AddScoped<IIPDDashboardService, IPDDashboardService>();

        services.AddScoped<IAdmissionChargeRepository, AdmissionChargeRepository>();
        services.AddScoped<IAdmissionChargeService, AdmissionChargeService>();

        services.AddScoped<IValidator<CreateWardRequest>, CreateWardRequestValidator>();
        services.AddScoped<IValidator<UpdateWardRequest>, UpdateWardRequestValidator>();

        services.AddScoped<IValidator<CreateBedRequest>, CreateBedRequestValidator>();
        services.AddScoped<IValidator<UpdateBedRequest>, UpdateBedRequestValidator>();

        services.AddScoped<IValidator<CreateAdmissionRequest>, CreateAdmissionRequestValidator>();
        services.AddScoped<IValidator<UpdateAdmissionRequest>, UpdateAdmissionRequestValidator>();
        services.AddScoped<IValidator<TransferBedRequest>, TransferBedRequestValidator>();
        services.AddScoped<IValidator<DischargeAdmissionRequest>, DischargeAdmissionRequestValidator>();

        services.AddScoped<IValidator<CreateAdmissionChargeRequest>, CreateAdmissionChargeRequestValidator>();

        return services;
    }
}
