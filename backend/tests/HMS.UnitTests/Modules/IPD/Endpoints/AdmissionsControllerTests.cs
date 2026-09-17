using System.Security.Claims;
using FluentValidation;
using HMS.Modules.IPD.Application;
using HMS.Modules.IPD.Contracts;
using HMS.Modules.IPD.Endpoints;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.IPD.Endpoints;

// Controller-level, same rationale as InvoicesControllerTests (ADR-077/080) — see
// ClaimsPrincipalExtensions.GetScopedConsultantId's own doc comment for the rule being
// verified here. This endpoint also backs the OPD Admissions List tab.
public class AdmissionsControllerTests
{
    private readonly IAdmissionService _service = Substitute.For<IAdmissionService>();
    private readonly AdmissionsController _sut;

    public AdmissionsControllerTests()
    {
        _sut = new AdmissionsController(
            _service,
            Substitute.For<IValidator<CreateAdmissionRequest>>(),
            Substitute.For<IValidator<UpdateAdmissionRequest>>(),
            Substitute.For<IValidator<TransferBedRequest>>(),
            Substitute.For<IValidator<DischargeAdmissionRequest>>(),
            Substitute.For<IValidator<RequestAdmissionRequest>>(),
            Substitute.For<IValidator<AssignBedRequest>>());
    }

    [Fact]
    public async Task GetPaged_OverridesConsultantIdFilter_WhenCallerIsAScopedConsultant()
    {
        var ownConsultantId = Guid.NewGuid();
        var otherConsultantId = Guid.NewGuid();
        SetCaller(ownConsultantId, "Doctor / Consultant");
        _service.GetPagedAsync(Arg.Any<AdmissionListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AdmissionResponse>([], 1, 20, 0));

        await _sut.GetPaged(new AdmissionListQuery { ConsultantId = otherConsultantId }, CancellationToken.None);

        await _service.Received(1).GetPagedAsync(
            Arg.Is<AdmissionListQuery>(q => q.ConsultantId == ownConsultantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaged_LeavesConsultantIdFilterAlone_WhenCallerIsNotAScopedConsultant()
    {
        SetCaller(Guid.NewGuid(), "Hospital Administrator");
        _service.GetPagedAsync(Arg.Any<AdmissionListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AdmissionResponse>([], 1, 20, 0));

        var requestedConsultantId = Guid.NewGuid();
        await _sut.GetPaged(new AdmissionListQuery { ConsultantId = requestedConsultantId }, CancellationToken.None);

        await _service.Received(1).GetPagedAsync(
            Arg.Is<AdmissionListQuery>(q => q.ConsultantId == requestedConsultantId),
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
