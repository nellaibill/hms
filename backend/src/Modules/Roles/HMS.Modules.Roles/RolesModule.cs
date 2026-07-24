using FluentValidation;
using HMS.Modules.Roles.Application;
using HMS.Modules.Roles.Application.Abstractions;
using HMS.Modules.Roles.Application.Validators;
using HMS.Modules.Roles.Contracts;
using HMS.Modules.Roles.Infrastructure;
using HMS.Modules.Roles.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Roles;

public static class RolesModule
{
    public static IServiceCollection AddRolesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<RolesDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    RolesDbContext.SchemaName);

                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            }));

        services.AddScoped<IRoleRepository, RoleRepository>();

        services.AddScoped<IRoleService, RoleService>();

        services.AddScoped<IValidator<CreateRoleRequest>, CreateRoleRequestValidator>();
        services.AddScoped<IValidator<UpdateRoleRequest>, UpdateRoleRequestValidator>();

        return services;
    }
}