using FluentAssertions;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Money;
using LedgerCore.Infrastructure.Repositories;
using LedgerCore.Infrastructure.Tests.Fixtures;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Infrastructure.Tests;

[Collection("PostgreSql")]
public class LedgerRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    public LedgerRepositoryTests(PostgreSqlFixture fixture) => _fixture = fixture;

    private async Task<(Guid CashId, Guid RevenueId)> SeedAccountsAsync()
    {
        await using var context = _fixture.CreateContext();
        var cash = Account.Create("Cash", $"C-{Guid.NewGuid():N}", AccountType.Asset, Currency.GHS);
        var revenue = Account.Create("Revenue", $"R-{Guid.NewGuid():N}", AccountType.Revenue, Currency.GHS);
        context.Accounts.AddRange(cash, revenue);
        await context.SaveChangesAsync();
        return (cash.Id, revenue.Id);
    }

    [Fact]
    public async Task AddAndGetById_RoundTrips()
    {
        var (cashId, revenueId) = await SeedAccountsAsync();
        var entry = JournalEntry.Create(
            DateTimeOffset.UtcNow, "Test sale", $"REF-{Guid.NewGuid():N}",
            new[] { EntryLine.Debit(cashId, DomainMoney.GHS(1000)), EntryLine.Credit(revenueId, DomainMoney.GHS(1000)) });

        await using var writeCtx = _fixture.CreateContext();
        var repo = new EfLedgerRepository(writeCtx);
        await repo.AddAsync(entry);

        await using var readCtx = _fixture.CreateContext();
        var readRepo = new EfLedgerRepository(readCtx);
        var loaded = await readRepo.GetByIdAsync(entry.Id);

        loaded.Should().NotBeNull();
        loaded!.Description.Should().Be("Test sale");
        loaded.Reference.Should().Be(entry.Reference);
        loaded.Status.Should().Be(EntryStatus.Pending);
        loaded.Lines.Should().HaveCount(2);
        loaded.Lines.First(l => l.Type == DebitOrCredit.Debit).Amount.Should().Be(DomainMoney.GHS(1000));
    }

    [Fact]
    public async Task GetByReference_ReturnsMatchingEntry()
    {
        var (cashId, revenueId) = await SeedAccountsAsync();
        var reference = $"REF-{Guid.NewGuid():N}";
        var entry = JournalEntry.Create(
            DateTimeOffset.UtcNow, "Sale", reference,
            new[] { EntryLine.Debit(cashId, DomainMoney.GHS(500)), EntryLine.Credit(revenueId, DomainMoney.GHS(500)) });

        await using var writeCtx = _fixture.CreateContext();
        var repo = new EfLedgerRepository(writeCtx);
        await repo.AddAsync(entry);

        await using var readCtx = _fixture.CreateContext();
        var readRepo = new EfLedgerRepository(readCtx);
        var loaded = await readRepo.GetByReferenceAsync(reference);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(entry.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsStatusChange()
    {
        var (cashId, revenueId) = await SeedAccountsAsync();
        var entry = JournalEntry.Create(
            DateTimeOffset.UtcNow, "Sale", $"REF-{Guid.NewGuid():N}",
            new[] { EntryLine.Debit(cashId, DomainMoney.GHS(700)), EntryLine.Credit(revenueId, DomainMoney.GHS(700)) });

        await using var writeCtx = _fixture.CreateContext();
        await new EfLedgerRepository(writeCtx).AddAsync(entry);

        await using var updateCtx = _fixture.CreateContext();
        var updateRepo = new EfLedgerRepository(updateCtx);
        var toUpdate = await updateRepo.GetByIdAsync(entry.Id);
        toUpdate!.Post();
        await updateRepo.UpdateAsync(toUpdate);

        await using var readCtx = _fixture.CreateContext();
        var loaded = await new EfLedgerRepository(readCtx).GetByIdAsync(entry.Id);
        loaded!.Status.Should().Be(EntryStatus.Posted);
        loaded.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPostedLinesForAccount_ReturnsOnlyPostedLines()
    {
        var (cashId, revenueId) = await SeedAccountsAsync();
        var entry = JournalEntry.Create(
            DateTimeOffset.UtcNow, "Sale", $"REF-{Guid.NewGuid():N}",
            new[] { EntryLine.Debit(cashId, DomainMoney.GHS(2000)), EntryLine.Credit(revenueId, DomainMoney.GHS(2000)) });
        entry.Post();

        await using var writeCtx = _fixture.CreateContext();
        await new EfLedgerRepository(writeCtx).AddAsync(entry);

        await using var readCtx = _fixture.CreateContext();
        var repo = new EfLedgerRepository(readCtx);
        var lines = await repo.GetPostedLinesForAccountAsync(cashId);

        lines.Should().ContainSingle();
        lines[0].Amount.Should().Be(DomainMoney.GHS(2000));
        lines[0].Type.Should().Be(DebitOrCredit.Debit);
    }
}
