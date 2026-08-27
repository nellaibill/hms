using FluentAssertions;
using HMS.Modules.Documents.Contracts;
using HMS.Modules.HR.Application.Abstractions;
using HMS.Modules.HR.Infrastructure;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.HR.Infrastructure;

public class StaffDocumentOwnerExistenceCheckerTests
{
    private readonly IEmployeeRepository _repository = Substitute.For<IEmployeeRepository>();
    private readonly StaffDocumentOwnerExistenceChecker _sut;

    public StaffDocumentOwnerExistenceCheckerTests() => _sut = new StaffDocumentOwnerExistenceChecker(_repository);

    [Fact]
    public void OwnerType_IsStaff()
    {
        _sut.OwnerType.Should().Be(DocumentOwnerType.Staff);
    }

    [Fact]
    public async Task ExistsAsync_DelegatesToEmployeeRepository()
    {
        var employeeId = Guid.NewGuid();
        _repository.ExistsAsync(employeeId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.ExistsAsync(employeeId, CancellationToken.None);

        result.Should().BeTrue();
        await _repository.Received(1).ExistsAsync(employeeId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_WhenEmployeeDoesNotExist_ReturnsFalse()
    {
        _repository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.ExistsAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeFalse();
    }
}
