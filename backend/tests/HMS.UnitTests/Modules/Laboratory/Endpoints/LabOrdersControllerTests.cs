using System.Security.Claims;
using FluentValidation;
using HMS.Modules.Laboratory.Application;
using HMS.Modules.Laboratory.Contracts;
using HMS.Modules.Laboratory.Endpoints;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Laboratory.Endpoints;

// Controller-level, same rationale as InvoicesControllerTests (ADR-077/080) — see
// ClaimsPrincipalExtensions.GetScopedConsultantId's own doc comment for the rule being
// verified here. This endpoint also backs the OPD Investigations List tab (source=OP).
public class LabOrdersControllerTests
{
    private readonly ILabOrderService _service = Substitute.For<ILabOrderService>();
    private readonly LabOrdersController _sut;

    public LabOrdersControllerTests()
    {
        _sut = new LabOrdersController(
            _service,
            Substitute.For<IValidator<CollectSampleRequest>>(),
            Substitute.For<IValidator<RejectSampleRequest>>(),
            Substitute.For<IValidator<SaveResultDraftRequest>>(),
            Substitute.For<IValidator<RejectForCorrectionRequest>>());
    }

    [Fact]
    public async Task GetPaged_OverridesConsultantIdFilter_WhenCallerIsAScopedConsultant()
    {
        var ownConsultantId = Guid.NewGuid();
        var otherConsultantId = Guid.NewGuid();
        SetCaller(ownConsultantId, "Doctor / Consultant");
        _service.GetPagedAsync(Arg.Any<LabOrderListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LabOrderResponse>([], 1, 20, 0));

        await _sut.GetPaged(new LabOrderListQuery { ConsultantId = otherConsultantId }, CancellationToken.None);

        await _service.Received(1).GetPagedAsync(
            Arg.Is<LabOrderListQuery>(q => q.ConsultantId == ownConsultantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPaged_LeavesConsultantIdFilterAlone_WhenCallerIsNotAScopedConsultant()
    {
        SetCaller(Guid.NewGuid(), "Hospital Administrator");
        _service.GetPagedAsync(Arg.Any<LabOrderListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LabOrderResponse>([], 1, 20, 0));

        var requestedConsultantId = Guid.NewGuid();
        await _sut.GetPaged(new LabOrderListQuery { ConsultantId = requestedConsultantId }, CancellationToken.None);

        await _service.Received(1).GetPagedAsync(
            Arg.Is<LabOrderListQuery>(q => q.ConsultantId == requestedConsultantId),
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
