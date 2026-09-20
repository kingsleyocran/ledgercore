using FluentAssertions;
using LedgerCore.Application;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;
using LedgerCore.Domain.Periods;
using Moq;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Application.Tests;

public class PostEntryTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ILedgerRepository> _ledgerRepo = new();
    private readonly Mock<IPeriodRepository> _periodRepo = new();
    private readonly PostEntryUseCase _useCase;

    private static readonly Guid CashId = Guid.NewGuid();
    private static readonly Guid RevenueId = Guid.NewGuid();

    public PostEntryTests()
    {
        _useCase = new PostEntryUseCase(_accountRepo.Object, _ledgerRepo.Object, _periodRepo.Object);

        var cashAccount = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);
        var revenueAccount = Account.Create("Revenue", "4000", AccountType.Revenue, Currency.GHS);

        _accountRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        _accountRepo.Setup(r => r.GetByIdAsync(CashId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(RevenueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revenueAccount);

        _ledgerRepo.Setup(r => r.GetByReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JournalEntry?)null);

        _periodRepo.Setup(r => r.GetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Period?)null);
    }

    private PostEntryCommand BalancedCommand(string reference = "INV-001") => new(
        EntryDate: DateTimeOffset.UtcNow,
        Description: "Customer payment",
        Reference: reference,
        Lines: new[]
        {
            EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
            EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
        });

    [Fact]
    public async Task Execute_ValidEntry_CreatesPendingEntry()
    {
        var result = await _useCase.ExecuteAsync(BalancedCommand());

        result.Status.Should().Be(EntryStatus.Pending);
        result.Reference.Should().Be("INV-001");
        result.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task Execute_PostImmediately_CreatesPostedEntry()
    {
        var command = BalancedCommand() with { PostImmediately = true };

        var result = await _useCase.ExecuteAsync(command);

        result.Status.Should().Be(EntryStatus.Posted);
        result.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_ValidEntry_PersistsViaRepository()
    {
        await _useCase.ExecuteAsync(BalancedCommand());

        _ledgerRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_DuplicateReference_ThrowsDuplicateReferenceException()
    {
        var existingEntry = JournalEntry.Create(
            DateTimeOffset.UtcNow, "Old", "INV-001",
            new[] { EntryLine.Debit(CashId, DomainMoney.GHS(500)), EntryLine.Credit(RevenueId, DomainMoney.GHS(500)) });

        _ledgerRepo.Setup(r => r.GetByReferenceAsync("INV-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntry);

        var act = () => _useCase.ExecuteAsync(BalancedCommand("INV-001"));

        await act.Should().ThrowAsync<DuplicateReferenceException>();
    }

    [Fact]
    public async Task Execute_AccountNotFound_ThrowsAccountNotFoundException()
    {
        var unknownId = Guid.NewGuid();
        var command = new PostEntryCommand(
            EntryDate: DateTimeOffset.UtcNow,
            Description: "Bad",
            Reference: "INV-002",
            Lines: new[]
            {
                EntryLine.Debit(unknownId, DomainMoney.GHS(1000)),
                EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
            });

        var act = () => _useCase.ExecuteAsync(command);

        var ex = await act.Should().ThrowAsync<AccountNotFoundException>();
        ex.Which.AccountId.Should().Be(unknownId);
    }

    [Fact]
    public async Task Execute_InactiveAccount_ThrowsInactiveAccountException()
    {
        var inactiveId = Guid.NewGuid();
        var inactiveAccount = Account.Create("Closed Cash", "1099", AccountType.Asset, Currency.GHS);
        inactiveAccount.Deactivate();

        _accountRepo.Setup(r => r.GetByIdAsync(inactiveId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveAccount);

        var command = new PostEntryCommand(
            EntryDate: DateTimeOffset.UtcNow,
            Description: "Bad",
            Reference: "INV-003",
            Lines: new[]
            {
                EntryLine.Debit(inactiveId, DomainMoney.GHS(1000)),
                EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
            });

        var act = () => _useCase.ExecuteAsync(command);

        await act.Should().ThrowAsync<InactiveAccountException>();
    }

    [Fact]
    public async Task Execute_UnbalancedEntry_ThrowsUnbalancedEntryException()
    {
        var command = new PostEntryCommand(
            EntryDate: DateTimeOffset.UtcNow,
            Description: "Unbalanced",
            Reference: "INV-004",
            Lines: new[]
            {
                EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
                EntryLine.Credit(RevenueId, DomainMoney.GHS(500))
            });

        var act = () => _useCase.ExecuteAsync(command);

        await act.Should().ThrowAsync<UnbalancedEntryException>();
    }

    [Fact]
    public async Task Execute_DoesNotPersistOnValidationFailure()
    {
        var unknownId = Guid.NewGuid();
        var command = new PostEntryCommand(
            EntryDate: DateTimeOffset.UtcNow,
            Description: "Bad",
            Reference: "INV-005",
            Lines: new[]
            {
                EntryLine.Debit(unknownId, DomainMoney.GHS(1000)),
                EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
            });

        try { await _useCase.ExecuteAsync(command); } catch { }

        _ledgerRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ClosedPeriod_ThrowsClosedPeriodException()
    {
        var closedPeriod = new Period(2026, 1);
        closedPeriod.Close();
        _periodRepo.Setup(r => r.GetAsync(2026, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(closedPeriod);

        var command = new PostEntryCommand(
            EntryDate: new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Description: "Sale",
            Reference: "INV-100",
            Lines: new[]
            {
                EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
                EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
            });

        var act = () => _useCase.ExecuteAsync(command);

        await act.Should().ThrowAsync<ClosedPeriodException>();
    }
}
