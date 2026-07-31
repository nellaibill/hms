using FluentValidation;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Application.Validators;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Infrastructure;
using HMS.Modules.Identity.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HMS.Modules.Identity;

/// <summary>
/// Single composition entry point for this module, called once from
/// HMS.Api/Configuration — the pattern every future module's registration follows.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly, which
                // is where EF Core looks for them by default. Referenced by name (a plain
                // string, not typeof(...).Assembly) because HMS.Database.Migrations already
                // references this module — a compile-time reference back would be circular.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            }));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserFileStorage, UserFileStorage>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRoleService, RoleService>();

        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionService, PermissionService>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Registered explicitly rather than via AddValidatorsFromAssemblyContaining: that
        // scanner only finds *public* IValidator<T> implementations (confirmed empirically —
        // it silently registers nothing for internal validator classes), so it was a no-op
        // here and left UsersController unable to resolve IValidator<CreateUserRequest> /
        // IValidator<UpdateUserRequest> at runtime. Both validators stay internal (per
        // docs/Architecture.md §4); only the FluentValidation-owned IValidator<T> interface
        // needs to be resolvable, and explicit registration doesn't require the
        // implementation type to be public.
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();

        services.AddScoped<IValidator<CreateRoleRequest>, CreateRoleRequestValidator>();
        services.AddScoped<IValidator<UpdateRoleRequest>, UpdateRoleRequestValidator>();

        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<SetPasswordRequest>, SetPasswordRequestValidator>();

        return services;
    }
}
