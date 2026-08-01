using System.Reflection;
using FluentAssertions;
using HMS.Modules.Identity.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Identity;

/// <summary>
/// Enforces the module-boundary rules from docs/Architecture.md §3–4: everything
/// outside Contracts is internal, and Contracts is the module's only public surface —
/// with two deliberate, narrow exceptions (see <see cref="AllowedPublicTypeNames"/>), both
/// required by tooling that only operates on public types, not a relaxation of the rule.
/// Every future module should carry an equivalent test.
/// </summary>
public class IdentityModuleBoundaryTests
{
    private static readonly Assembly IdentityAssembly = typeof(UsersController).Assembly;

    /// <summary>
    /// Simple type names (NetArchTest's HaveNameMatching/DoNotHaveNameMatching match against
    /// <see cref="System.Type.Name"/>, not the namespace-qualified name) that must stay
    /// public despite living in an otherwise-internal namespace, each for a hard technical
    /// reason rather than convenience:
    /// - IUserService, IRoleService, IPermissionService, IAuthenticationService: each
    ///   module controller — UsersController, RolesController, PermissionsController,
    ///   AuthenticationController — which ASP.NET Core requires to be a public class with
    ///   a public constructor for controller discovery/DI activation — takes its
    ///   respective service as a constructor dependency; a public constructor cannot have
    ///   an internal parameter type (CS0051). These are this module's deliberate seams
    ///   between its public HTTP boundary and its internal Application/Domain/
    ///   Infrastructure layers.
    /// - IdentityDbContext: resolved by type from HMS.Api's Program.cs (a different
    ///   assembly, with no InternalsVisibleTo grant) for the startup-time migration call.
    /// </summary>
    private const string AllowedPublicTypeNamePattern = "^(IUserService|IRoleService|IPermissionService|IAuthenticationService|IdentityDbContext)$";

    [Theory]
    [InlineData("HMS.Modules.Identity.Domain")]
    [InlineData("HMS.Modules.Identity.Application")]
    [InlineData("HMS.Modules.Identity.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(IdentityAssembly)
            .That()
            .ResideInNamespaceStartingWith(layerNamespace)
            .And()
            .DoNotHaveNameMatching(AllowedPublicTypeNamePattern)
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    [Fact]
    public void Contracts_ShouldBePublic()
    {
        var result = Types.InAssembly(IdentityAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Identity.Contracts")
            .Should()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FailureMessage(result));
    }

    private static string FailureMessage(TestResult result) =>
        result.FailingTypeNames is null
            ? "Rule failed."
            : "Rule failed for: " + string.Join(", ", result.FailingTypeNames);
}
