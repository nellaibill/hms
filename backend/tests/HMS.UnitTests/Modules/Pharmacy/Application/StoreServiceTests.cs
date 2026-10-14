using FluentAssertions;
using HMS.Modules.Pharmacy.Application;
using HMS.Modules.Pharmacy.Application.Abstractions;
using HMS.Modules.Pharmacy.Contracts;
using HMS.Modules.Pharmacy.Domain;
using NSubstitute;
using Xunit;

namespace HMS.UnitTests.Modules.Pharmacy.Application;

public class StoreServiceTests
{
    private readonly IPharmacyStoreRepository _repository = Substitute.For<IPharmacyStoreRepository>();
    private readonly StoreService _sut;
    private readonly PharmacyStore _main = PharmacyStore.Create("MAIN", "Main Pharmacy", StoreType.Pharmacy, null);

    public StoreServiceTests()
    {
        _sut = new StoreService(_repository);

        _main.MakeDefault(null);
        _repository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(_main);
        _repository.GetByIdAsync(_main.Id, Arg.Any<CancellationToken>()).Returns(_main);
    }

    [Fact]
    public async Task CreateAsync_NormalisesTheCode_AndLeavesTheDefaultWhereItIs()
    {
        var result = await _sut.CreateAsync(new CreateStoreRequest { Code = " ip ", Name = "IP Pharmacy", StoreType = StoreType.Pharmacy }, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("IP");
        result.Value.IsDefault.Should().BeFalse();
        _main.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_AsDefault_TakesTheFlagFromTheStoreThatHeldIt()
    {
        var result = await _sut.CreateAsync(new CreateStoreRequest { Code = "OP", Name = "OP Pharmacy", IsDefault = true }, null, CancellationToken.None);

        result.Value!.IsDefault.Should().BeTrue();
        _main.IsDefault.Should().BeFalse();
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_TheVeryFirstStore_BecomesTheDefaultEvenIfNotAsked()
    {
        _repository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((PharmacyStore?)null);

        var result = await _sut.CreateAsync(new CreateStoreRequest { Code = "MAIN", Name = "Main Pharmacy" }, null, CancellationToken.None);

        result.Value!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithACodeAlreadyInUse_Fails()
    {
        _repository.ExistsByCodeAsync("MAIN", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new CreateStoreRequest { Code = "main", Name = "Another" }, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.DuplicateStoreCode);
        await _repository.DidNotReceive().AddAsync(Arg.Any<PharmacyStore>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_RefusesToDeactivateTheDefaultStore()
    {
        var result = await _sut.UpdateAsync(_main.Id, new UpdateStoreRequest { Name = "Main Pharmacy", StoreType = StoreType.Pharmacy, IsActive = false }, null, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(PharmacyErrorCodes.DefaultStoreRequired);
        _main.IsActive.Should().BeTrue();
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_MakingAnotherStoreDefault_MovesTheFlag()
    {
        var ward = PharmacyStore.Create("WARD3", "Ward 3", StoreType.SubStore, null);
        _repository.GetByIdAsync(ward.Id, Arg.Any<CancellationToken>()).Returns(ward);

        var result = await _sut.UpdateAsync(ward.Id, new UpdateStoreRequest { Name = "Ward 3", StoreType = StoreType.SubStore, IsActive = true, IsDefault = true }, null, CancellationToken.None);

        result.Value!.IsDefault.Should().BeTrue();
        _main.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithIsDefaultFalse_NeverLeavesTheHospitalWithoutADefault()
    {
        var result = await _sut.UpdateAsync(_main.Id, new UpdateStoreRequest { Name = "Main Pharmacy (renamed)", StoreType = StoreType.Pharmacy, IsActive = true, IsDefault = false }, null, CancellationToken.None);

        result.Value!.Name.Should().Be("Main Pharmacy (renamed)");
        result.Value.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ForAnUnknownStore_IsNotFound()
    {
        var result = await _sut.UpdateAsync(Guid.NewGuid(), new UpdateStoreRequest { Name = "X" }, null, CancellationToken.None);

        result.ErrorCode.Should().Be(PharmacyErrorCodes.NotFound);
    }

    [Fact]
    public async Task ResolveAsync_NamingNoStore_GivesTheDefault_AndAnInactiveOrUnknownStoreGivesNothing()
    {
        var closed = PharmacyStore.Create("OLD", "Old store", StoreType.SubStore, null);
        closed.Update("Old store", StoreType.SubStore, isActive: false, null);
        _repository.GetByIdAsync(closed.Id, Arg.Any<CancellationToken>()).Returns(closed);

        (await _repository.ResolveAsync(null, CancellationToken.None)).Should().BeSameAs(_main);
        (await _repository.ResolveAsync(_main.Id, CancellationToken.None)).Should().BeSameAs(_main);
        (await _repository.ResolveAsync(closed.Id, CancellationToken.None)).Should().BeNull();
        (await _repository.ResolveAsync(Guid.NewGuid(), CancellationToken.None)).Should().BeNull();
    }
}
