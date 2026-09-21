using FluentAssertions;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Money;
using LedgerCore.Infrastructure.Repositories;
using LedgerCore.Infrastructure.Tests.Fixtures;

namespace LedgerCore.Infrastructure.Tests;

[Collection("PostgreSql")]
public class AccountRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    public AccountRepositoryTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AddAndGetById_RoundTrips()
    {
        await using var context = _fixture.CreateContext();
        var account = Account.Create("Cash", $"ACCT-{Guid.NewGuid():N}", AccountType.Asset, Currency.GHS);
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        await using var readContext = _fixture.CreateContext();
        var repo = new EfAccountRepository(readContext);
        var loaded = await repo.GetByIdAsync(account.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Cash");
        loaded.AccountNumber.Should().Be(account.AccountNumber);
        loaded.Type.Should().Be(AccountType.Asset);
        loaded.Currency.Should().Be(Currency.GHS);
        loaded.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllActive_ExcludesInactive()
    {
        await using var context = _fixture.CreateContext();
        var active = Account.Create("Active", $"ACT-{Guid.NewGuid():N}", AccountType.Asset, Currency.GHS);
        var inactive = Account.Create("Inactive", $"INACT-{Guid.NewGuid():N}", AccountType.Asset, Currency.GHS);
        inactive.Deactivate();
        context.Accounts.AddRange(active, inactive);
        await context.SaveChangesAsync();

        await using var readContext = _fixture.CreateContext();
        var repo = new EfAccountRepository(readContext);
        var all = await repo.GetAllActiveAsync();

        all.Should().Contain(a => a.Id == active.Id);
        all.Should().NotContain(a => a.Id == inactive.Id);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        await using var context = _fixture.CreateContext();
        var repo = new EfAccountRepository(context);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}
