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

// Controller-level, like InvoicesControllerTests: the Super Admin gate under test (ADR-106)
// reads the caller's LoginType claim, which lives in the Controller, not the Service.
public class InvoiceRevertRequestsControllerTests
{
    private readonly IInvoiceRevertService _service = Substitute.For<IInvoiceRevertService>();
    private readonly InvoiceRevertRequestsController _sut;

    public InvoiceRevertRequestsControllerTests()
    {
        var approveValidator = Substitute.For<IValidator<ApproveInvoiceRevertRequest>>();
        approveValidator.ValidateAsync(Arg.Any<ApproveInvoiceRevertRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        var rejectValidator = Substitute.For<IValidator<RejectInvoiceRevertRequest>>();
        rejectValidator.ValidateAsync(Arg.Any<RejectInvoiceRevertRequest>(), Arg.Any<CancellationToken>()).Returns(new ValidationResult());

        _sut = new InvoiceRevertRequestsController(_service, Substitute.For<IValidator<CreateInvoiceRevertRequest>>(), approveValidator, rejectValidator);
        _service.ApproveAsync(Arg.Any<Guid>(), Arg.Any<ApproveInvoiceRevertRequest>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceRevertRequestResponse>.Success(new InvoiceRevertRequestResponse()));
        _service.RejectAsync(Arg.Any<Guid>(), Arg.Any<RejectInvoiceRevertRequest>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceRevertRequestResponse>.Success(new InvoiceRevertRequestResponse()));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("receptionist")]
    [InlineData("accounts")]
    public async Task Approve_IsForbiddenForAnyoneButASuperAdmin(string loginType)
    {
        SetCaller(loginType);

        var result = await _sut.Approve(Guid.NewGuid(), new ApproveInvoiceRevertRequest(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _service.DidNotReceive().ApproveAsync(Arg.Any<Guid>(), Arg.Any<ApproveInvoiceRevertRequest>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_IsForbiddenForAnyoneButASuperAdmin()
    {
        SetCaller("admin");

        var result = await _sut.Reject(Guid.NewGuid(), new RejectInvoiceRevertRequest { Notes = "No" }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _service.DidNotReceive().RejectAsync(Arg.Any<Guid>(), Arg.Any<RejectInvoiceRevertRequest>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Approve_PassesASuperAdminsIdAndUsernameToTheService()
    {
        var userId = SetCaller("superAdmin", "superadmin");
        var id = Guid.NewGuid();

        var result = await _sut.Approve(id, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        await _service.Received(1).ApproveAsync(id, Arg.Any<ApproveInvoiceRevertRequest>(), userId, "superadmin", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reject_AllowsASuperAdmin()
    {
        SetCaller("superAdmin");

        var result = await _sut.Reject(Guid.NewGuid(), new RejectInvoiceRevertRequest { Notes = "Consultation was given" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
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

    // The front desk raises revert requests, and the Receptionist role has finance-billing.create
    // but not .edit — so raising one must only need create.
    [Fact]
    public void Create_RequiresOnlyTheBillCreatePermission()
    {
        var attribute = typeof(InvoiceRevertRequestsController).GetMethod(nameof(InvoiceRevertRequestsController.Create))!
            .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: false)
            .Cast<RequirePermissionAttribute>()
            .Single();

        attribute.GetRequirements().Cast<PermissionRequirement>().Single().PermissionKey.Should().Be("finance-billing.create");
    }
}
