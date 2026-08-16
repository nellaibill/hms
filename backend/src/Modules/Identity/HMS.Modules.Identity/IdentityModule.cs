using FluentValidation;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Application.Validators;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;
using HMS.Modules.Identity.Infrastructure;
using HMS.Modules.Identity.Infrastructure.Repositories;
using HMS.Modules.Identity.Infrastructure.Seed;
using HMS.Shared.Kernel;
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
        // HMS Multi-Tenancy Phase C: the connection is resolved per-request from
        // ITenantContext, not closed over a static ConnectionStrings:Default value — the
        // (IServiceProvider, DbContextOptionsBuilder) overload runs once per DI scope
        // (i.e. once per HTTP request), at the moment this DbContext is first resolved
        // within that scope, by which point TenantResolutionMiddleware (or, for the login
        // request itself, AuthenticationService — see its own doc comment) has already
        // populated ITenantContext. See docs/DatabaseArchitecture.md.
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var tenantContext = sp.GetRequiredService<ITenantContext>();
            if (!tenantContext.IsResolved)
            {
                throw new InvalidOperationException(
                    "IdentityDbContext was resolved without a tenant having been established for this request.");
            }

            options.UseNpgsql(tenantContext.ConnectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.SchemaName);

                // Migration classes live in HMS.Database.Migrations (per
                // docs/DatabaseArchitecture.md), not in this module's own assembly, which
                // is where EF Core looks for them by default. Referenced by name (a plain
                // string, not typeof(...).Assembly) because HMS.Database.Migrations already
                // references this module — a compile-time reference back would be circular.
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            });
        });

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

        services.Configure<SuperAdminSeedOptions>(configuration.GetSection(SuperAdminSeedOptions.SectionName));
        services.AddScoped<IdentityDataSeeder>();

        return services;
    }

    /// <summary>
    /// Startup data seeding entry point, called once from Program.cs after
    /// <c>IdentityDbContext.Database.Migrate()</c> — the same
    /// "single public seam per module" shape as <see cref="AddIdentityModule"/> itself.
    /// <see cref="IdentityDataSeeder"/> stays internal; resolving it here (inside this
    /// module's own assembly) doesn't require it to be public.
    /// </summary>
    public static Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<IdentityDataSeeder>().SeedAsync(cancellationToken);
    }

    /// <summary>
    /// Provisioning-only seam: creates the "Super Admin" role (every permission attached)
    /// and the hospital's first Super Admin user in a freshly-migrated tenant database.
    /// Called by HMS.Api.Provisioning.TenantProvisioningService, which cannot construct
    /// User/Role directly — they're internal to this assembly (see
    /// HMS.Modules.Platform.Application.Abstractions.ITenantProvisioner's doc comment).
    /// Reuses the exact same domain factories/repositories/password hasher as
    /// IdentityDataSeeder, just against a caller-supplied connection string instead of the
    /// DI-registered <see cref="IdentityDbContext"/>, and does not run migrations itself —
    /// the caller already applied them before invoking this.
    /// </summary>
    public static async Task ProvisionTenantSuperAdminAsync(
        string connectionString,
        string username,
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string password,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.SchemaName);
                npgsql.MigrationsAssembly("HMS.Database.Migrations");
            })
            .Options;

        await using var dbContext = new IdentityDbContext(options);

        var permissionRepository = new PermissionRepository(dbContext);
        var roleRepository = new RoleRepository(dbContext);
        var userRepository = new UserRepository(dbContext);
        var passwordHasher = new PasswordHasher();

        var permissions = await permissionRepository.GetAllAsync(cancellationToken);

        var role = Role.Create(
            "Super Admin",
            "Full system access with every permission assigned. Created automatically during tenant provisioning.",
            isSystemRole: true,
            displayOrder: 0,
            createdBy: null);

        role.ReplacePermissions(permissions.Select(p => p.Id));

        await roleRepository.AddAsync(role, cancellationToken);
        await roleRepository.SaveChangesAsync(cancellationToken);

        var user = User.Create(username, firstName, lastName, email, phoneNumber, role.Id, createdBy: null);
        user.SetPasswordHash(passwordHasher.HashPassword(password), updatedBy: null);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }
}
