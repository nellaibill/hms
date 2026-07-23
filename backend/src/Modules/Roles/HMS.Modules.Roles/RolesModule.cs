using FluentValidation;
using HMS.Modules.Roles.Application;
using HMS.Modules.Roles.Application.Abstractions;
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
        services.AddDbContext<RolesDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Database")));

        services.AddScoped<IRoleRepository, RoleRepository>();

        services.AddScoped<IRoleService, RoleService>();

        services.AddValidatorsFromAssembly(
            typeof(RolesModule).Assembly);

        return services;
    }
}