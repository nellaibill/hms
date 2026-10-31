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

// Controller-level, like InvoiceRevertRequestsControllerTests: the Super Admin gate under test
// (ADR-109) reads the caller's LoginType claim, which lives in the Controller, not the Service.
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
        _service.ApproveAsync(Arg.Any<Guid>(), Arg.Any<ApproveInvoiceDiscountRequest>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceDiscountRequestResponse>.Success(new InvoiceDiscountRequestResponse()));
        _service.RejectAsync(Arg.Any<Guid>(), Arg.Any<RejectInvoiceDiscountRequest>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceDiscountRequestResponse>.Success(new InvoiceDiscountRequestResponse()));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("receptionist")]
    [InlineData("accounts")]
    public async Task Approve_IsForbiddenForAnyoneButASuperAdmin(string loginType)
    {
        SetCaller(loginType);

        var result = await _sut.Approve(Guid.NewGuid(), new ApproveInvoiceDiscountRequest(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _service.DidNotReceive().ApproveAsync(Arg.Any<Guid>(), Arg.Any<ApproveInvoiceDiscountRequest>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_IsForbiddenForAnyoneButASuperAdmin()
    {
        SetCaller("admin");

        var result = await _sut.Reject(Guid.NewGuid(), new RejectInvoiceDiscountRequest { Notes = "No" }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _service.DidNotReceive().RejectAsync(Arg.Any<Guid>(), Arg.Any<RejectInvoiceDiscountRequest>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_PassesASuperAdminsIdAndUsernameToTheService()
    {
        var userId = SetCaller("superAdmin", "superadmin");
        var id = Guid.NewGuid();

        var result = await _sut.Approve(id, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _service.Received(1).ApproveAsync(id, Arg.Any<ApproveInvoiceDiscountRequest>(), userId, "superadmin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_PassesWhetherTheCallerIsASuperAdmin()
    {
        var userId = SetCaller("receptionist", "reception1");
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

    private Guid SetCaller(string loginType, string username = "user1")
    {
        var userId = Guid.NewGuid();
        var claims = new List<Claim> { new("UserId", userId.ToString()), new("Username", username), new("LoginType", loginType) };
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) },
        };
        return userId;
    }
}
