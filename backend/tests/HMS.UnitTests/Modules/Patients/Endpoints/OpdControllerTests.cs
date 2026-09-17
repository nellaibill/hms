using System.Security.Claims;
using FluentAssertions;
using HMS.Modules.Patients.Application;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Endpoints;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Endpoints;

// Controller-level, same rationale as InvoicesControllerTests (ADR-077/080): the scoping
// under test needs the caller's ClaimsPrincipal, which lives in the Controller, not the
// Service — see ClaimsPrincipalExtensions.GetScopedConsultantId's own doc comment for the
// rule (linked ConsultantId AND a Consultant-ish RoleName) being verified here.
public class OpdControllerTests
{
    private readonly IOpdQueryService _service = Substitute.For<IOpdQueryService>();
    private readonly OpdController _sut;

    public OpdControllerTests()
    {
        _sut = new OpdController(_service);
    }

    [Fact]
    public async Task GetPatientList_OverridesConsultantIdFilter_WhenCallerIsAScopedConsultant()
    {
        var ownConsultantId = Guid.NewGuid();
        var otherConsultantId = Guid.NewGuid();
        SetCaller(ownConsultantId, "Doctor / Consultant");
        _service.GetPatientListAsync(Arg.Any<OpdPatientListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<OpdPatientListItem>([], 1, 20, 0));

        // The client asked for a different consultant's patients — the controller must force
        // it back to the caller's own id regardless.
        await _sut.GetPatientList(new OpdPatientListQuery { ConsultantId = otherConsultantId }, CancellationToken.None);

        await _service.Received(1).GetPatientListAsync(
            Arg.Is<OpdPatientListQuery>(q => q.ConsultantId == ownConsultantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPatientList_LeavesConsultantIdFilterAlone_WhenCallerIsNotAScopedConsultant()
    {
        // A Super Admin or Admin linked to a consultant record (the link is independent of
        // role) must still see whatever the client actually asked for — including everyone.
        var linkedButNotConsultantRole = Guid.NewGuid();
        SetCaller(linkedButNotConsultantRole, "Hospital Administrator");
        _service.GetPatientListAsync(Arg.Any<OpdPatientListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<OpdPatientListItem>([], 1, 20, 0));

        var requestedConsultantId = Guid.NewGuid();
        await _sut.GetPatientList(new OpdPatientListQuery { ConsultantId = requestedConsultantId }, CancellationToken.None);

        await _service.Received(1).GetPatientListAsync(
            Arg.Is<OpdPatientListQuery>(q => q.ConsultantId == requestedConsultantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetConsultationSummary_OverridesConsultantIdFilter_WhenCallerIsAScopedConsultant()
    {
        var ownConsultantId = Guid.NewGuid();
        SetCaller(ownConsultantId, "Doctor / Consultant");
        _service.GetConsultationSummaryAsync(Arg.Any<OpdConsultationSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<OpdConsultationSummaryItem>>.Success([]));

        await _sut.GetConsultationSummary(new OpdConsultationSummaryQuery(), CancellationToken.None);

        await _service.Received(1).GetConsultationSummaryAsync(
            Arg.Is<OpdConsultationSummaryQuery>(q => q.ConsultantId == ownConsultantId),
            Arg.Any<CancellationToken>());
    }

    private void SetCaller(Guid? consultantId, string roleName)
    {
        var claims = new List<Claim> { new("UserId", Guid.NewGuid().ToString()), new("RoleName", roleName) };
        if (consultantId.HasValue)
        {
            claims.Add(new Claim("ConsultantId", consultantId.Value.ToString()));
        }

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) },
        };
    }
}
