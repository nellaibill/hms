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

// Controller-level, like InvoiceRevertRequestsControllerTests: who the caller is (ADR-109's
// Super Admin login type, ADR-111's consultant login) is read from claims in the Controller and
// handed to the Service as a BillingActor; the Service decides what that actor may do.
public class InvoiceDiscountRequestsControllerTests
{
    private readonly IInvoiceDiscountService _service = Substitute.For<IInvoiceDiscountService>();
    private readonly InvoiceDiscountRequestsController _sut;

    public InvoiceDiscountRequestsControllerTests()
    {
        var createValidator = Substitute.For<IValidator<CreateInvoiceDiscountRequest>>();
        createValidator.ValidateAsync(Arg.Any<CreateInvoiceDiscountRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var approveValidator = Substitute.For<IValidator<ApproveInvoiceDiscountRequest>>();
        approveValidator.ValidateAsync(Arg.Any<ApproveInvoiceDiscountRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var rejectValidator = Substitute.For<IValidator<RejectInvoiceDiscountRequest>>();
        rejectValidator.ValidateAsync(Arg.Any<RejectInvoiceDiscountRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());

        _sut = new InvoiceDiscountRequestsController(_service, createValidator, approveValidator, rejectValidator);
        _service.CreateAsync(Arg.Any<Guid>(), Arg.Any<CreateInvoiceDiscountRequest>(), Arg.Any<BillingActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceDiscountRequestResponse>.Success(new InvoiceDiscountRequestResponse()));
        _service.ApproveAsync(Arg.Any<Guid>(), Arg.Any<ApproveInvoiceDiscountRequest>(), Arg.Any<BillingActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceDiscountRequestResponse>.Success(new InvoiceDiscountRequestResponse()));
        _service.RejectAsync(Arg.Any<Guid>(), Arg.Any<RejectInvoiceDiscountRequest>(), Arg.Any<BillingActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceDiscountRequestResponse>.Success(new InvoiceDiscountRequestResponse()));
        _service.GetPagedForConsultantAsync(Arg.Any<Guid>(), Arg.Any<InvoiceDiscountRequestListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InvoiceDiscountRequestResponse>([], 1, 20, 0));
    }

    [Fact]
    public async Task Approve_PassesASuperAdminAsTheActor()
    {
        var userId = SetCaller("superAdmin", "superadmin", roleName: "Super Admin");
        var approvalId = Guid.NewGuid();

        var result = await _sut.Approve(approvalId, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _service.Received(1).ApproveAsync(approvalId, Arg.Any<ApproveInvoiceDiscountRequest>(), new BillingActor(userId, "superadmin", IsSuperAdmin: true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_PassesAConsultantLoginsOwnConsultantId()
    {
        var consultantId = Guid.NewGuid();
        var userId = SetCaller("doctor", "karthikeyan", roleName: "Doctor / Consultant", consultantId: consultantId);
        var approvalId = Guid.NewGuid();

        await _sut.Approve(approvalId, new ApproveInvoiceDiscountRequest(), CancellationToken.None);

        await _service.Received(1).ApproveAsync(approvalId, Arg.Any<ApproveInvoiceDiscountRequest>(), new BillingActor(userId, "karthikeyan", IsSuperAdmin: false, ConsultantId: consultantId), Arg.Any<CancellationToken>());
    }

    // A login linked to a consultant record but holding another role doesn't decide that
    // consultant's discounts (ClaimsPrincipalExtensions.GetScopedConsultantId).
    [Fact]
    public async Task Approve_ForALinkedLoginWithoutAConsultantRole_PassesNoConsultantId()
    {
        var userId = SetCaller("receptionist", "reception1", roleName: "Receptionist", consultantId: Guid.NewGuid());
        var approvalId = Guid.NewGuid();

        await _sut.Approve(approvalId, new ApproveInvoiceDiscountRequest(), CancellationToken.None);

        await _service.Received(1).ApproveAsync(approvalId, Arg.Any<ApproveInvoiceDiscountRequest>(), new BillingActor(userId, "reception1", IsSuperAdmin: false), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_WhenThePartIsntTheCallersToDecide_IsForbidden()
    {
        SetCaller("receptionist", roleName: "Receptionist");
        _service.ApproveAsync(Arg.Any<Guid>(), Arg.Any<ApproveInvoiceDiscountRequest>(), Arg.Any<BillingActor>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceDiscountRequestResponse>.Failure(BillingErrorCodes.DiscountNotYourApproval, "No"));

        var result = await _sut.Approve(Guid.NewGuid(), new ApproveInvoiceDiscountRequest(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Reject_PassesTheCallerAsTheActor()
    {
        var consultantId = Guid.NewGuid();
        var userId = SetCaller("doctor", "karthikeyan", roleName: "Doctor / Consultant", consultantId: consultantId);
        var approvalId = Guid.NewGuid();
        var request = new RejectInvoiceDiscountRequest { Notes = "Full fee" };

        var result = await _sut.Reject(approvalId, request, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _service.Received(1).RejectAsync(approvalId, request, new BillingActor(userId, "karthikeyan", IsSuperAdmin: false, ConsultantId: consultantId), Arg.Any<CancellationToken>());
    }

    // Consultants don't hold billing permissions — deciding a part is checked against who they
    // are, in the Service.
    [Theory]
    [InlineData(nameof(InvoiceDiscountRequestsController.Approve))]
    [InlineData(nameof(InvoiceDiscountRequestsController.Reject))]
    [InlineData(nameof(InvoiceDiscountRequestsController.GetMine))]
    [InlineData(nameof(InvoiceDiscountRequestsController.GetMinePendingCount))]
    public void ConsultantEndpoints_NeedNoBillingPermission(string methodName)
    {
        typeof(InvoiceDiscountRequestsController).GetMethod(methodName)!
            .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task GetMine_ForAConsultantLogin_ListsTheirOwnApprovals()
    {
        var consultantId = Guid.NewGuid();
        SetCaller("doctor", roleName: "Doctor / Consultant", consultantId: consultantId);
        var query = new InvoiceDiscountRequestListQuery { Status = InvoiceDiscountRequestStatus.Pending };

        var result = await _sut.GetMine(query, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _service.Received(1).GetPagedForConsultantAsync(consultantId, query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMine_ForAnyoneButAConsultant_IsForbidden()
    {
        SetCaller("superAdmin", roleName: "Super Admin");

        var result = await _sut.GetMine(new InvoiceDiscountRequestListQuery(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _service.DidNotReceive().GetPagedForConsultantAsync(Arg.Any<Guid>(), Arg.Any<InvoiceDiscountRequestListQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMinePendingCount_ForAConsultantLogin_CountsTheirOwn()
    {
        var consultantId = Guid.NewGuid();
        SetCaller("doctor", roleName: "Doctor / Consultant", consultantId: consultantId);
        _service.GetPendingCountForConsultantAsync(consultantId, Arg.Any<CancellationToken>()).Returns(3);

        var result = await _sut.GetMinePendingCount(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<ApiResponse<int>>().Which.Data.Should().Be(3);
    }

    [Fact]
    public async Task Create_PassesWhetherTheCallerIsASuperAdmin()
    {
        var userId = SetCaller("receptionist", "reception1", roleName: "Receptionist");
        var invoiceId = Guid.NewGuid();
        var request = new CreateInvoiceDiscountRequest { Reason = "Staff family" };

        var result = await _sut.Create(invoiceId, request, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        await _service.Received(1).CreateAsync(invoiceId, request, new BillingActor(userId, "reception1", IsSuperAdmin: false), Arg.Any<CancellationToken>());
    }

    // The front desk asks for discounts, and the Receptionist role has finance-billing.create but
    // not .edit — so raising one must only need create.
    [Fact]
    public void Create_RequiresOnlyTheBillCreatePermission()
    {
        var attribute = typeof(InvoiceDiscountRequestsController).GetMethod(nameof(InvoiceDiscountRequestsController.Create))!
            .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false)
            .Cast<RequirePermissionAttribute>()
            .Single();

        attribute.GetRequirements().Cast<PermissionRequirement>().Single().PermissionKey.Should().Be("finance-billing.create");
    }

    private Guid SetCaller(string loginType, string username = "user1", string? roleName = null, Guid? consultantId = null)
    {
        var userId = Guid.NewGuid();
        var claims = new List<Claim> { new("UserId", userId.ToString()), new("Username", username), new("LoginType", loginType) };
        if (roleName is not null) claims.Add(new Claim("RoleName", roleName));
        if (consultantId is not null) claims.Add(new Claim("ConsultantId", consultantId.Value.ToString()));
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) },
        };
        return userId;
    }
}
