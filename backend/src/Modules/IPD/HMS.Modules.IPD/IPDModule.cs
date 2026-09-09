using FluentValidation;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Application.Abstractions;
using HMS.Modules.IPD.Application.Validators;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Infrastructure;
using HMS.Modules.IPD.Infrastructure.Repositories;
using HMS.Shared.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.IPD;

public static class IPDModule
{
    public static IServiceCollection AddIPDModule(this IServiceCollection services, IConfiguration configuration)
    {
        // HMS Multi-Tenancy Phase C: resolved per-request from ITenantContext — see
        // HMS.Modules.Identity.IdentityModule's identical registration for the full
        // rationale.
        services.AddDbContext<IPDDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "IPDDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IPDDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

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

        services.AddScoped<IAdmissionAdvanceRepository, AdmissionAdvanceRepository>();
        services.AddScoped<IAdmissionAdvanceService, AdmissionAdvanceService>();

        services.AddScoped<IVitalsReadingRepository, VitalsReadingRepository>();
        services.AddScoped<IVitalsReadingService, VitalsReadingService>();

        services.AddScoped<IProgressNoteRepository, ProgressNoteRepository>();
        services.AddScoped<IProgressNoteService, ProgressNoteService>();

        services.AddScoped<INursingAssessmentRepository, NursingAssessmentRepository>();
        services.AddScoped<INursingAssessmentService, NursingAssessmentService>();

        services.AddScoped<INursingNoteRepository, NursingNoteRepository>();
        services.AddScoped<INursingNoteService, NursingNoteService>();

        services.AddScoped<IDoctorOrderRepository, DoctorOrderRepository>();
        services.AddScoped<IDoctorOrderService, DoctorOrderService>();

        services.AddScoped<IMedicationOrderRepository, MedicationOrderRepository>();
        services.AddScoped<IMedicationOrderService, MedicationOrderService>();

        services.AddScoped<IMedicationAdministrationRepository, MedicationAdministrationRepository>();
        services.AddScoped<IMedicationAdministrationService, MedicationAdministrationService>();

        services.AddScoped<IIPDLabOrderService, IPDLabOrderService>();

        services.AddScoped<IIPDBillingService, IPDBillingService>();

        services.AddScoped<IValidator<CreateWardRequest>, CreateWardRequestValidator>();
        services.AddScoped<IValidator<UpdateWardRequest>, UpdateWardRequestValidator>();

        services.AddScoped<IValidator<CreateBedRequest>, CreateBedRequestValidator>();
        services.AddScoped<IValidator<UpdateBedRequest>, UpdateBedRequestValidator>();

        services.AddScoped<IValidator<CreateAdmissionRequest>, CreateAdmissionRequestValidator>();
        services.AddScoped<IValidator<UpdateAdmissionRequest>, UpdateAdmissionRequestValidator>();
        services.AddScoped<IValidator<TransferBedRequest>, TransferBedRequestValidator>();
        services.AddScoped<IValidator<DischargeAdmissionRequest>, DischargeAdmissionRequestValidator>();

        services.AddScoped<IValidator<CreateAdmissionChargeRequest>, CreateAdmissionChargeRequestValidator>();
        services.AddScoped<IValidator<CreateAdmissionAdvanceRequest>, CreateAdmissionAdvanceRequestValidator>();

        services.AddScoped<IValidator<CreateVitalsReadingRequest>, CreateVitalsReadingRequestValidator>();
        services.AddScoped<IValidator<CreateProgressNoteRequest>, CreateProgressNoteRequestValidator>();

        services.AddScoped<IValidator<CreateNursingAssessmentRequest>, CreateNursingAssessmentRequestValidator>();
        services.AddScoped<IValidator<CreateNursingNoteRequest>, CreateNursingNoteRequestValidator>();

        services.AddScoped<IValidator<CreateDoctorOrderRequest>, CreateDoctorOrderRequestValidator>();
        services.AddScoped<IValidator<CancelDoctorOrderRequest>, CancelDoctorOrderRequestValidator>();

        services.AddScoped<IValidator<CreateMedicationOrderRequest>, CreateMedicationOrderRequestValidator>();
        services.AddScoped<IValidator<DiscontinueMedicationOrderRequest>, DiscontinueMedicationOrderRequestValidator>();
        services.AddScoped<IValidator<CreateMedicationAdministrationRequest>, CreateMedicationAdministrationRequestValidator>();

        services.AddScoped<IValidator<CreatePlaceLabOrderRequest>, CreatePlaceLabOrderRequestValidator>();

        return services;
    }
}
