using FluentAssertions;
using HMS.Modules.Identity.Application;
using HMS.Modules.Identity.Application.Abstractions;
using HMS.Modules.Identity.Contracts;
using HMS.Modules.Identity.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Identity.Application;

public class UserServiceTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_repository, NullLogger<UserService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithNewEmail_CreatesUserAndReturnsSuccess()
    {
        _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new CreateUserRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("ada@example.com");
        result.Value.IsActive.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsDuplicateEmailFailure()
    {
        var existing = User.Create("Grace", "Hopper", "ada@example.com", null, null);
        _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);

        var request = new CreateUserRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.DuplicateEmail);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenUserNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new UpdateUserRequest { FirstName = "Grace", LastName = "Hopper", Email = "grace@example.com" };

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailChangedToAnotherUsersEmail_ReturnsDuplicateEmailFailure()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        var otherUser = User.Create("Grace", "Hopper", "grace@example.com", null, null);

        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetByEmailAsync("grace@example.com", Arg.Any<CancellationToken>()).Returns(otherUser);

        var request = new UpdateUserRequest { FirstName = "Ada", LastName = "Lovelace", Email = "grace@example.com" };

        var result = await _sut.UpdateAsync(user.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.DuplicateEmail);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesProfileAndReturnsSuccess()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateUserRequest { FirstName = "Ada Marie", LastName = "Lovelace", Email = "ada@example.com" };

        var result = await _sut.UpdateAsync(user.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Ada Marie");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenUserNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_SoftDeletesAndReturnsSuccess()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.DeleteAsync(user.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsDeleted.Should().BeTrue();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetByIdAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var users = new List<User>
        {
            User.Create("Ada", "Lovelace", "ada@example.com", null, null),
            User.Create("Grace", "Hopper", "grace@example.com", null, null),
        };

        _repository.GetPagedAsync(Arg.Any<UserListQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)users, 2));

        var query = new UserListQuery { Page = 1, PageSize = 20 };

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(u => u.Email).Should().Contain(new[] { "ada@example.com", "grace@example.com" });
    }

    [Fact]
    public async Task ActivateAsync_WhenFound_ActivatesAndReturnsSuccess()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        user.Deactivate(null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.ActivateAsync(user.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_WhenFound_DeactivatesAndReturnsSuccess()
    {
        var user = User.Create("Ada", "Lovelace", "ada@example.com", null, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.DeactivateAsync(user.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ActivateAsync_WhenNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.ActivateAsync(Guid.NewGuid(), actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.NotFound);
    }
}
