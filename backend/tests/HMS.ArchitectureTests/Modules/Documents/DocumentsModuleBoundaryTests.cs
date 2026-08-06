using System.Reflection;
using FluentAssertions;
using HMS.Modules.Documents.Endpoints;
using NetArchTest.Rules;
using Xunit;

namespace HMS.ArchitectureTests.Modules.Documents;

/// <summary>
/// Enforces the same module-boundary rules as
/// HMS.ArchitectureTests.Modules.Identity.IdentityModuleBoundaryTests: everything outside
/// Contracts is internal, and Contracts is the module's only wholesale-public surface — with
/// deliberate, narrow exceptions (see <see cref="AllowedPublicTypeNamePattern"/>), each
/// required by tooling/cross-module wiring that only operates on public types, not a
/// relaxation of the rule.
/// </summary>
public class DocumentsModuleBoundaryTests
{
    private static readonly Assembly DocumentsAssembly = typeof(DocumentsController).Assembly;

    /// <summary>
    /// - IDocumentService: DocumentsController — which ASP.NET Core requires to be public,
    ///   with a public constructor, for controller discovery/DI activation — takes this as a
    ///   constructor dependency. A public constructor cannot have an internal parameter type
    ///   (CS0051).
    /// - DocumentActor, DocumentContent: parameter/return types on IDocumentService's public
    ///   methods — must themselves be public for the same CS0051 reason.
    /// - IDocumentOwnerExistenceChecker: implemented by other modules (e.g.
    ///   HMS.Modules.Patients.Infrastructure.PatientDocumentOwnerExistenceChecker) — the
    ///   module's public seam for cross-module owner-existence validation (US-1).
    /// - DocumentsDbContext: resolved by type from HMS.Api's Program.cs (a different
    ///   assembly, with no InternalsVisibleTo grant) for the startup-time migration call.
    /// </summary>
    private const string AllowedPublicTypeNamePattern = "^(IDocumentService|DocumentActor|DocumentContent|IDocumentOwnerExistenceChecker|DocumentsDbContext)$";

    [Theory]
    [InlineData("HMS.Modules.Documents.Domain")]
    [InlineData("HMS.Modules.Documents.Application")]
    [InlineData("HMS.Modules.Documents.Infrastructure")]
    public void InternalLayers_ShouldNotExposePublicTypes(string layerNamespace)
    {
        var result = Types.InAssembly(DocumentsAssembly)
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
        var result = Types.InAssembly(DocumentsAssembly)
            .That()
            .ResideInNamespace("HMS.Modules.Documents.Contracts")
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
