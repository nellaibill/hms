using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HMS.Modules.Billing.Application;
using HMS.Modules.Billing.Contracts;
using HMS.Modules.Billing.Endpoints;
using HMS.Shared.Kernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Billing.Endpoints;

// Controller-level, same rationale as ProductsControllerTests (ADR-077): the permission check
// under test (ADR-080) needs the caller's ClaimsPrincipal, which lives in the Controller, not
// the Service.
public class InvoicesControllerTests
{
    private readonly IInvoiceService _service = Substitute.For<IInvoiceService>();
    private readonly IValidator<CreateInvoiceRequest> _createValidator = Substitute.For<IValidator<CreateInvoiceRequest>>();
    private readonly InvoicesController _sut;

    public InvoicesControllerTests()
    {
        _createValidator.ValidateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _sut = new InvoicesController(_service, _createValidator, Substitute.For<IValidator<VoidInvoiceRequest>>());
    }

    [Fact]
    public async Task Create_RejectsAnApprovedDiscountFromACallerWithoutTheApprovalPermission()
    {
        SetCaller(includingDiscountApprove: false);
        var request = BuildRequest(discountApproved: true);

        var result = await _sut.Create(request, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _service.DidNotReceive().CreateAsync(Arg.Any<CreateInvoiceRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_AllowsAnApprovedDiscountFromACallerWithTheApprovalPermission()
    {
        SetCaller(includingDiscountApprove: true);
        var request = BuildRequest(discountApproved: true);
        _service.CreateAsync(request, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Success(new InvoiceResponse { Id = Guid.NewGuid() }));

        var result = await _sut.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
        await _service.Received(1).CreateAsync(request, Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_AllowsAnUndiscountedInvoiceFromACallerWithoutTheApprovalPermission()
    {
        SetCaller(includingDiscountApprove: false);
        var request = BuildRequest(discountApproved: false);
        _service.CreateAsync(request, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<InvoiceResponse>.Success(new InvoiceResponse { Id = Guid.NewGuid() }));

        var result = await _sut.Create(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    private void SetCaller(bool includingDiscountApprove)
    {
        var claims = new List<Claim> { new("UserId", Guid.NewGuid().ToString()) };
        if (includingDiscountApprove)
        {
            claims.Add(new Claim("Permission", "finance-billing.discount-approve"));
        }

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) },
        };
    }

    private static CreateInvoiceRequest BuildRequest(bool discountApproved) => new()
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
                Discount = discountApproved ? 100 : 0,
                DiscountApproved = discountApproved,
                DiscountApprovedBy = discountApproved ? "Self" : null,
            },
        ],
    };
}
