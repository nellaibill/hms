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
    // Real file signatures — magic-byte validation (UserService.LooksLikeAnAllowedImageAsync)
    // rejects anything that doesn't start with these, so fake bytes like [1, 2, 3] no longer
    // pass as a "valid" upload.
    private static readonly byte[] ValidJpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] ValidPngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    private static readonly byte[] NotAnImageBytes = "this is plain text, not an image"u8.ToArray();

    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserFileStorage _fileStorage = Substitute.For<IUserFileStorage>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly UserService _sut;
    private readonly Guid _roleId = Guid.NewGuid();

    public UserServiceTests()
    {
        _sut = new UserService(_repository, _roleRepository, _fileStorage, _passwordHasher, NullLogger<UserService>.Instance);

        // Happy-path default: a valid role exists. Tests for the "role not found" failure
        // path override this per-test.
        var role = Role.Create("Nurse", null, false, 0, null);
        _roleRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(role);
    }

    [Fact]
    public async Task GetStaffDirectoryAsync_MapsActiveUsersWithRoleNames()
    {
        var role = Role.Create("Nurse", null, false, 0, null);
        var user = User.Create("j.doe", "Jane", "Doe", "jane@example.com", null, role.Id, null);
        _repository
            .GetPagedAsync(Arg.Any<UserListQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<User> { user }, 1));
        _roleRepository.GetManyByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns([role]);

        var directory = await _sut.GetStaffDirectoryAsync(search: null, CancellationToken.None);

        directory.Should().ContainSingle();
        directory[0].Id.Should().Be(user.Id);
        directory[0].FirstName.Should().Be("Jane");
        directory[0].RoleName.Should().Be("Nurse");
    }

    [Fact]
    public async Task CreateAsync_WithNewUsernameAndEmail_CreatesUserAndReturnsSuccess()
    {
        _repository.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new CreateUserRequest
        {
            Username = "ada.lovelace",
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            RoleId = _roleId,
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("ada.lovelace");
        result.Value.Email.Should().Be("ada@example.com");
        result.Value.IsActive.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUsername_ReturnsDuplicateUsernameFailure()
    {
        var existing = User.Create("ada.lovelace", "Grace", "Hopper", "grace@example.com", null, _roleId, null);
        _repository.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);

        var request = new CreateUserRequest
        {
            Username = "ada.lovelace",
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            RoleId = _roleId,
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.DuplicateUsername);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ReturnsDuplicateEmailFailure()
    {
        var existing = User.Create("grace.hopper", "Grace", "Hopper", "ada@example.com", null, _roleId, null);
        _repository.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(existing);

        var request = new CreateUserRequest
        {
            Username = "ada.lovelace",
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            RoleId = _roleId,
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.DuplicateEmail);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithUnknownRoleId_ReturnsInvalidRoleFailure()
    {
        _repository.GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _roleRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Role?)null);

        var request = new CreateUserRequest
        {
            Username = "ada.lovelace",
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            RoleId = Guid.NewGuid(),
        };

        var result = await _sut.CreateAsync(request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.InvalidRole);
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenUserNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var request = new UpdateUserRequest { Username = "grace.hopper", FirstName = "Grace", LastName = "Hopper", Email = "grace@example.com", RoleId = _roleId };

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WhenUsernameChangedToAnotherUsersUsername_ReturnsDuplicateUsernameFailure()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        var otherUser = User.Create("grace.hopper", "Grace", "Hopper", "grace@example.com", null, _roleId, null);

        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetByUsernameAsync("grace.hopper", Arg.Any<CancellationToken>()).Returns(otherUser);

        var request = new UpdateUserRequest { Username = "grace.hopper", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", RoleId = _roleId };

        var result = await _sut.UpdateAsync(user.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.DuplicateUsername);
    }

    [Fact]
    public async Task UpdateAsync_WhenEmailChangedToAnotherUsersEmail_ReturnsDuplicateEmailFailure()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        var otherUser = User.Create("grace.hopper", "Grace", "Hopper", "grace@example.com", null, _roleId, null);

        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetByEmailAsync("grace@example.com", Arg.Any<CancellationToken>()).Returns(otherUser);

        var request = new UpdateUserRequest { Username = "ada.lovelace", FirstName = "Ada", LastName = "Lovelace", Email = "grace@example.com", RoleId = _roleId };

        var result = await _sut.UpdateAsync(user.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.DuplicateEmail);
    }

    [Fact]
    public async Task UpdateAsync_WhenRoleChangedToUnknownRoleId_ReturnsInvalidRoleFailure()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var newRoleId = Guid.NewGuid();
        _roleRepository.GetByIdAsync(newRoleId, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var request = new UpdateUserRequest { Username = "ada.lovelace", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", RoleId = newRoleId };

        var result = await _sut.UpdateAsync(user.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.InvalidRole);
        user.RoleId.Should().Be(_roleId);
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesProfileAndReturnsSuccess()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateUserRequest { Username = "ada.lovelace", FirstName = "Ada Marie", LastName = "Lovelace", Email = "ada@example.com", RoleId = _roleId };

        var result = await _sut.UpdateAsync(user.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Ada Marie");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenRoleChangedToAnotherKnownRole_ChangesRole()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var newRoleId = Guid.NewGuid();
        var newRole = Role.Create("Doctor", null, false, 0, null);
        _roleRepository.GetByIdAsync(newRoleId, Arg.Any<CancellationToken>()).Returns(newRole);

        var request = new UpdateUserRequest { Username = "ada.lovelace", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", RoleId = newRoleId };

        var result = await _sut.UpdateAsync(user.Id, request, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.RoleId.Should().Be(newRoleId);
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
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
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
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.GetByIdAsync(user.Id, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var role = Role.Create("Nurse", null, false, 0, null);

        var users = new List<User>
        {
            User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, role.Id, null),
            User.Create("grace.hopper", "Grace", "Hopper", "grace@example.com", null, role.Id, null),
        };

        _repository.GetPagedAsync(Arg.Any<UserListQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)users, 2));

        _roleRepository.GetManyByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Role>)[role]);

        var query = new UserListQuery { Page = 1, PageSize = 20 };

        var result = await _sut.GetPagedAsync(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(u => u.Email).Should().Contain(new[] { "ada@example.com", "grace@example.com" });
        result.Items.Should().OnlyContain(u => u.RoleName == role.Name);
    }

    [Fact]
    public async Task ActivateAsync_WhenFound_ActivatesAndReturnsSuccess()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        user.Deactivate(null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.ActivateAsync(user.Id, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_WhenFound_DeactivatesAndReturnsSuccess()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
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

    [Fact]
    public async Task UploadProfilePhotoAsync_WhenUserNotFound_ReturnsNotFoundFailureAndSavesNothing()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        using var content = new MemoryStream(ValidJpegBytes);

        var result = await _sut.UploadProfilePhotoAsync(Guid.NewGuid(), content, "photo.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.NotFound);
        await _fileStorage.DidNotReceive().SaveProfilePhotoAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_WithValidJpeg_UploadsAndReturnsSuccess()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _fileStorage.SaveProfilePhotoAsync(user.Id, "photo.jpg", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/users/019fb880.jpg");

        using var content = new MemoryStream(ValidJpegBytes);

        var result = await _sut.UploadProfilePhotoAsync(user.Id, content, "photo.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProfilePhotoUrl.Should().Be("uploads/users/019fb880.jpg");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_WithDisallowedExtension_ReturnsInvalidFileFailure()
    {
        // A ".exe" file lying about its Content-Type — extension check must catch it even
        // when the declared MIME type looks fine.
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        using var content = new MemoryStream(ValidJpegBytes);

        var result = await _sut.UploadProfilePhotoAsync(user.Id, content, "malware.exe", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.InvalidFile);
        await _fileStorage.DidNotReceive().SaveProfilePhotoAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("photo.txt")]
    [InlineData("document.pdf")]
    [InlineData("archive.zip")]
    public async Task UploadProfilePhotoAsync_WithDisallowedContentType_ReturnsInvalidFileFailure(string fileName)
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        using var content = new MemoryStream(NotAnImageBytes);

        var result = await _sut.UploadProfilePhotoAsync(user.Id, content, fileName, "application/octet-stream", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.InvalidFile);
        await _fileStorage.DidNotReceive().SaveProfilePhotoAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_WithOversizedFile_ReturnsInvalidFileFailure()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        using var content = new MemoryStream(ValidJpegBytes);
        const long oversizedLength = 3 * 1024 * 1024; // 3MB, over the 2MB limit.

        var result = await _sut.UploadProfilePhotoAsync(user.Id, content, "photo.jpg", "image/jpeg", oversizedLength, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.InvalidFile);
        await _fileStorage.DidNotReceive().SaveProfilePhotoAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_WithCorruptedImage_ReturnsInvalidFileFailure()
    {
        // "virus.jpg" — the extension and declared Content-Type both claim JPEG, but the
        // bytes are plain text. Only the magic-byte check catches this.
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        using var content = new MemoryStream(NotAnImageBytes);

        var result = await _sut.UploadProfilePhotoAsync(user.Id, content, "virus.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.InvalidFile);
        await _fileStorage.DidNotReceive().SaveProfilePhotoAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_WhenReplacingWithADifferentExtension_OverwritesProfilePhotoUrl()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        user.SetProfilePhoto("uploads/users/old.jpg", null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _fileStorage.SaveProfilePhotoAsync(user.Id, "new-photo.png", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("uploads/users/new.png");

        using var content = new MemoryStream(ValidPngBytes);

        var result = await _sut.UploadProfilePhotoAsync(user.Id, content, "new-photo.png", "image/png", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProfilePhotoUrl.Should().Be("uploads/users/new.png");
        user.ProfilePhotoUrl.Should().Be("uploads/users/new.png");
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_WhenReplacingWithTheSameExtensionTwice_ResultsInExactlyOneProfilePhotoUrl()
    {
        // photo1.png then photo2.png for the same user — both resolve to the same stored
        // path (userId + extension), so the second upload overwrites the first in place.
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _fileStorage.SaveProfilePhotoAsync(user.Id, Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns($"uploads/users/{user.Id}.png");

        using var firstContent = new MemoryStream(ValidPngBytes);
        var firstResult = await _sut.UploadProfilePhotoAsync(user.Id, firstContent, "photo1.png", "image/png", firstContent.Length, actorId: null, CancellationToken.None);

        using var secondContent = new MemoryStream(ValidPngBytes);
        var secondResult = await _sut.UploadProfilePhotoAsync(user.Id, secondContent, "photo2.png", "image/png", secondContent.Length, actorId: null, CancellationToken.None);

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
        firstResult.Value!.ProfilePhotoUrl.Should().Be(secondResult.Value!.ProfilePhotoUrl);
        user.ProfilePhotoUrl.Should().Be($"uploads/users/{user.Id}.png");
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_WithExtremelyLongFileName_StillSucceeds()
    {
        // The stored file is named after the user's own id (see UserFileStorage), not the
        // client-supplied name, so an unreasonably long name shouldn't matter at all.
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var longFileName = new string('a', 200) + ".png";
        _fileStorage.SaveProfilePhotoAsync(user.Id, longFileName, Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns($"uploads/users/{user.Id}.png");

        using var content = new MemoryStream(ValidPngBytes);

        var result = await _sut.UploadProfilePhotoAsync(user.Id, content, longFileName, "image/png", content.Length, actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProfilePhotoUrl.Should().Be($"uploads/users/{user.Id}.png");
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_ForTwoDifferentUsersWithTheSameFileName_ProducesNoCollision()
    {
        var user1 = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        var user2 = User.Create("grace.hopper", "Grace", "Hopper", "grace@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user1.Id, Arg.Any<CancellationToken>()).Returns(user1);
        _repository.GetByIdAsync(user2.Id, Arg.Any<CancellationToken>()).Returns(user2);
        _fileStorage.SaveProfilePhotoAsync(user1.Id, "photo.png", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns($"uploads/users/{user1.Id}.png");
        _fileStorage.SaveProfilePhotoAsync(user2.Id, "photo.png", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns($"uploads/users/{user2.Id}.png");

        using var content1 = new MemoryStream(ValidPngBytes);
        var result1 = await _sut.UploadProfilePhotoAsync(user1.Id, content1, "photo.png", "image/png", content1.Length, actorId: null, CancellationToken.None);

        using var content2 = new MemoryStream(ValidPngBytes);
        var result2 = await _sut.UploadProfilePhotoAsync(user2.Id, content2, "photo.png", "image/png", content2.Length, actorId: null, CancellationToken.None);

        result1.Value!.ProfilePhotoUrl.Should().Be($"uploads/users/{user1.Id}.png");
        result2.Value!.ProfilePhotoUrl.Should().Be($"uploads/users/{user2.Id}.png");
        result1.Value.ProfilePhotoUrl.Should().NotBe(result2.Value.ProfilePhotoUrl);
    }

    [Fact]
    public async Task UploadProfilePhotoAsync_WhenDiskWriteFails_PropagatesAndSavesNothing()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _fileStorage.SaveProfilePhotoAsync(user.Id, "photo.jpg", Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new IOException("Simulated disk write failure."));

        using var content = new MemoryStream(ValidJpegBytes);

        var act = () => _sut.UploadProfilePhotoAsync(user.Id, content, "photo.jpg", "image/jpeg", content.Length, actorId: null, CancellationToken.None);

        await act.Should().ThrowAsync<IOException>();
        user.ProfilePhotoUrl.Should().BeNull();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPasswordAsync_WhenFound_HashesAndSetsPasswordAndReturnsSuccess()
    {
        var user = User.Create("ada.lovelace", "Ada", "Lovelace", "ada@example.com", null, _roleId, null);
        _repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.HashPassword("Sup3rSecret!").Returns("hashed-password");

        var result = await _sut.SetPasswordAsync(user.Id, "Sup3rSecret!", actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed-password");
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPasswordAsync_WhenUserNotFound_ReturnsNotFoundFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _sut.SetPasswordAsync(Guid.NewGuid(), "Sup3rSecret!", actorId: null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(UserErrorCodes.NotFound);
    }
}
