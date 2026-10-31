using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Endpoints;
using HMS.Shared.Infrastructure;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Billing.Endpoints;

// Controller-level, same rationale as ProductsControllerTests (ADR-077): who may approve their
// own discount (ADR-109) comes from the caller's ClaimsPrincipal, which lives in the
// Controller, not the Service.
public class InvoicesControllerTests
{
    private readonly IInvoiceService _service = Substitute.For<IInvoiceService>();
    private readonly IValidator<CreateInvoiceRequest> _createValidator = Substitute.For<IValidator<CreateInvoiceRequest>>();
    private readonly InvoicesController _sut;

    public InvoicesControllerTests()
    {
        _createValidator.ValidateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var payValidator = Substitute.For<IValidator<PayInvoiceRequest>>();
        payValidator.ValidateAsync(Arg.Any<PayInvoiceRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        _sut = new InvoicesController(_service, _createValidator, Substitute.For<IValidator<VoidInvoiceRequest>>(), payValidator);
        _service.CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<BillingActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Success(new InvoiceResponse { Id = Guid.NewGuid() }));
    }

    [Fact]
    public async Task Create_PassesASuperAdminCallerAsOneWhoseDiscountNeedsNoApproval()
    {
        var userId = SetCaller("superAdmin", "superadmin");
        var request = BuildRequest(discount: 100);

        var result = await _sut.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        await _service.Received(1).CreateAsync(request, new BillingActor(userId, "superadmin", IsSuperAdmin: true), Arg.Any<CancellationToken>());
    }

    // ADR-080's "discount-approve" permission no longer lets anyone self-approve a bill discount:
    // only a Super Admin does (ADR-109) — everyone else's discount becomes a request.
    [Theory]
    [InlineData("admin")]
    [InlineData("receptionist")]
    [InlineData("accounts")]
    public async Task Create_PassesAnyOtherCallerAsOneWhoseDiscountNeedsApproval(string loginType)
    {
        var userId = SetCaller(loginType, "reception1", includingDiscountApprove: true);
        var request = BuildRequest(discount: 100);

        var result = await _sut.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        await _service.Received(1).CreateAsync(request, new BillingActor(userId, "reception1", IsSuperAdmin: false), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pay_ReturnsConflictWhileADiscountIsWaitingForApproval()
    {
        SetCaller("receptionist");
        _service.PayAsync(Arg.Any<Guid>(), Arg.Any<PayInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Failure(BillingErrorCodes.DiscountApprovalPending, "Waiting"));

        var result = await _sut.Pay(Guid.NewGuid(), new PayInvoiceRequest(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    // The front desk collects payment once a discount is decided, and the Receptionist role has
    // finance-billing.create but not .edit — so collecting must only need create.
    [Fact]
    public void Pay_RequiresOnlyTheBillCreatePermission()
    {
        var attribute = typeof(InvoicesController).GetMethod(nameof(InvoicesController.Pay))!
            .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false)
            .Cast<RequirePermissionAttribute>()
            .Single();

        attribute.GetRequirements().Cast<PermissionRequirement>().Single().PermissionKey.Should().Be("finance-billing.create");
    }

    // The scoping under test here needs a ConsultantId/RoleName claim, which no existing
    // Create test cares about — a separate helper from SetCaller(bool) rather than adding
    // unused parameters to it.
    [Fact]
    public async Task GetProcedures_OverridesConsultantIdFilter_WhenCallerIsAScopedConsultant()
    {
        var ownConsultantId = Guid.NewGuid();
        var otherConsultantId = Guid.NewGuid();
        SetCallerConsultant(ownConsultantId, "Doctor / Consultant");
        _service.GetProcedureLineItemsAsync(Arg.Any<ProcedureListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProcedureListItem>([], 1, 20, 0));

        await _sut.GetProcedures(new ProcedureListQuery { ConsultantId = otherConsultantId.ToString() }, CancellationToken.None);

        await _service.Received(1).GetProcedureLineItemsAsync(
            Arg.Is<ProcedureListQuery>(q => q.ConsultantId == ownConsultantId.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProcedures_LeavesConsultantIdFilterAlone_WhenCallerIsNotAScopedConsultant()
    {
        SetCallerConsultant(Guid.NewGuid(), "Hospital Administrator");
        _service.GetProcedureLineItemsAsync(Arg.Any<ProcedureListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ProcedureListItem>([], 1, 20, 0));

        var requestedConsultantId = Guid.NewGuid().ToString();
        await _sut.GetProcedures(new ProcedureListQuery { ConsultantId = requestedConsultantId }, CancellationToken.None);

        await _service.Received(1).GetProcedureLineItemsAsync(
            Arg.Is<ProcedureListQuery>(q => q.ConsultantId == requestedConsultantId),
            Arg.Any<CancellationToken>());
    }

    private Guid SetCaller(string loginType, string username = "user1", bool includingDiscountApprove = false)
    {
        var userId = Guid.NewGuid();
        var claims = new List<Claim> { new("UserId", userId.ToString()), new("Username", username), new("LoginType", loginType) };
        if (includingDiscountApprove)
        {
            claims.Add(new Claim("Permission", "finance-billing.discount-approve"));
        }

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) },
        };
        return userId;
    }

    private void SetCallerConsultant(Guid? consultantId, string roleName)
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

    private static CreateInvoiceRequest BuildRequest(decimal discount) => new()
    {
        PatientId = Guid.NewGuid(),
        VisitId = Guid.NewGuid(),
        PatientName = "Test Patient",
        PatientUhid = "P-2026-000001",
        Items =
        [
            new CreateInvoiceLineItemRequest
            {
                BillingType = BillingType.Consultation,
                Quantity = 1,
                UnitPrice = 500,
                Discount = discount,
            },
        ],
        DiscountType = InvoiceDiscountType.Amount,
        DiscountValue = discount,
        DiscountReason = discount > 0 ? "Staff family" : null,
    };
}
