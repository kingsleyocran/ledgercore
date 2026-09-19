using FluentAssertions;
using LedgerCore.Application;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;
using Moq;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Application.Tests;

public class GetStatementTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ILedgerRepository> _ledgerRepo = new();
    private readonly GetStatementUseCase _useCase;

    private static readonly Guid CashId = Guid.NewGuid();
    private static readonly Guid RevenueId = Guid.NewGuid();
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 1, 31, 23, 59, 59, TimeSpan.Zero);

    public GetStatementTests()
    {
        _useCase = new GetStatementUseCase(_accountRepo.Object, _ledgerRepo.Object);

        var cashAccount = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);
        var revenueAccount = Account.Create("Revenue", "4000", AccountType.Revenue, Currency.GHS);

        _accountRepo.Setup(r => r.GetByIdAsync(CashId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(RevenueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revenueAccount);

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntryLine>().AsReadOnly());
        _ledgerRepo.Setup(r => r.GetPostedEntryLinesForAccountAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(JournalEntry, EntryLine)>().AsReadOnly());
    }

    private static JournalEntry CreatePostedEntry(string reference, DateTimeOffset date, Guid debitAccount, Guid creditAccount, long amount)
    {
        var entry = JournalEntry.Create(
            date, $"Transaction {reference}", reference,
            new[]
            {
                EntryLine.Debit(debitAccount, DomainMoney.GHS(amount)),
                EntryLine.Credit(creditAccount, DomainMoney.GHS(amount))
            });
        entry.Post();
        return entry;
    }

    [Fact]
    public async Task Execute_ReturnsStatementLinesWithCorrectFields()
    {
        var entry = CreatePostedEntry("INV-001", new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero), CashId, RevenueId, 5000);
        var debitLine = entry.Lines.First(l => l.Type == DebitOrCredit.Debit);

        _ledgerRepo.Setup(r => r.GetPostedEntryLinesForAccountAsync(CashId, Start, End, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(JournalEntry, EntryLine)> { (entry, debitLine) }.AsReadOnly());

        var result = await _useCase.ExecuteAsync(CashId, Start, End);

        result.Should().HaveCount(1);
        result[0].EntryDate.Should().Be(entry.EntryDate);
        result[0].Description.Should().Be("Transaction INV-001");
        result[0].Reference.Should().Be("INV-001");
        result[0].Debit.Should().Be(DomainMoney.GHS(5000));
        result[0].Credit.Should().BeNull();
    }

    [Fact]
    public async Task Execute_CreditLine_ShowsInCreditColumn()
    {
        var entry = CreatePostedEntry("INV-002", new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero), CashId, RevenueId, 3000);
        var creditLine = entry.Lines.First(l => l.Type == DebitOrCredit.Credit);

        _ledgerRepo.Setup(r => r.GetPostedEntryLinesForAccountAsync(RevenueId, Start, End, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(JournalEntry, EntryLine)> { (entry, creditLine) }.AsReadOnly());

        var result = await _useCase.ExecuteAsync(RevenueId, Start, End);

        result[0].Debit.Should().BeNull();
        result[0].Credit.Should().Be(DomainMoney.GHS(3000));
    }

    [Fact]
    public async Task Execute_DebitNormalAccount_RunningBalanceAccumulates()
    {
        var entry1 = CreatePostedEntry("INV-001", new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero), CashId, RevenueId, 5000);
        var entry2 = CreatePostedEntry("INV-002", new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero), CashId, RevenueId, 3000);

        var lines = new List<(JournalEntry, EntryLine)>
        {
            (entry1, entry1.Lines.First(l => l.AccountId == CashId)),
            (entry2, entry2.Lines.First(l => l.AccountId == CashId))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedEntryLinesForAccountAsync(CashId, Start, End, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines);

        var result = await _useCase.ExecuteAsync(CashId, Start, End);

        result.Should().HaveCount(2);
        result[0].RunningBalance.Should().Be(DomainMoney.GHS(5000));
        result[1].RunningBalance.Should().Be(DomainMoney.GHS(8000));
    }

    [Fact]
    public async Task Execute_WithOpeningBalance_RunningBalanceStartsFromIt()
    {
        var priorLines = new List<EntryLine>
        {
            EntryLine.Debit(CashId, DomainMoney.GHS(10000))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(CashId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(priorLines);

        var entry = CreatePostedEntry("INV-010", new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero), CashId, RevenueId, 2000);
        var lines = new List<(JournalEntry, EntryLine)>
        {
            (entry, entry.Lines.First(l => l.AccountId == CashId))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedEntryLinesForAccountAsync(CashId, Start, End, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines);

        var result = await _useCase.ExecuteAsync(CashId, Start, End);

        result[0].RunningBalance.Should().Be(DomainMoney.GHS(12000));
    }

    [Fact]
    public async Task Execute_EmptyDateRange_ReturnsEmptyList()
    {
        var result = await _useCase.ExecuteAsync(CashId, Start, End);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_AccountNotFound_ThrowsAccountNotFoundException()
    {
        var unknownId = Guid.NewGuid();
        _accountRepo.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _useCase.ExecuteAsync(unknownId, Start, End);

        await act.Should().ThrowAsync<AccountNotFoundException>();
    }

    [Fact]
    public async Task Execute_MixedDebitsAndCredits_RunningBalanceCorrect()
    {
        var entry1 = CreatePostedEntry("INV-001", new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero), CashId, RevenueId, 5000);
        var refundEntry = JournalEntry.Create(
            new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero), "Refund", "REF-001",
            new[]
            {
                EntryLine.Debit(RevenueId, DomainMoney.GHS(1000)),
                EntryLine.Credit(CashId, DomainMoney.GHS(1000))
            });
        refundEntry.Post();

        var lines = new List<(JournalEntry, EntryLine)>
        {
            (entry1, entry1.Lines.First(l => l.AccountId == CashId)),
            (refundEntry, refundEntry.Lines.First(l => l.AccountId == CashId))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedEntryLinesForAccountAsync(CashId, Start, End, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines);

        var result = await _useCase.ExecuteAsync(CashId, Start, End);

        result.Should().HaveCount(2);
        result[0].RunningBalance.Should().Be(DomainMoney.GHS(5000));
        result[1].RunningBalance.Should().Be(DomainMoney.GHS(4000));
    }
}
