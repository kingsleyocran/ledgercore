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

public class GetBalanceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ILedgerRepository> _ledgerRepo = new();
    private readonly GetBalanceUseCase _useCase;

    private static readonly Guid CashId = Guid.NewGuid();
    private static readonly Guid RevenueId = Guid.NewGuid();

    public GetBalanceTests()
    {
        _useCase = new GetBalanceUseCase(_accountRepo.Object, _ledgerRepo.Object);

        var cashAccount = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);
        var revenueAccount = Account.Create("Revenue", "4000", AccountType.Revenue, Currency.GHS);

        _accountRepo.Setup(r => r.GetByIdAsync(CashId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashAccount);
        _accountRepo.Setup(r => r.GetByIdAsync(RevenueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revenueAccount);

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntryLine>().AsReadOnly());
    }

    [Fact]
    public async Task Execute_DebitNormalAccount_ReturnsDebitsMinusCredits()
    {
        var lines = new List<EntryLine>
        {
            EntryLine.Debit(CashId, DomainMoney.GHS(5000)),
            EntryLine.Debit(CashId, DomainMoney.GHS(3000)),
            EntryLine.Credit(CashId, DomainMoney.GHS(2000))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(CashId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines);

        var balance = await _useCase.ExecuteAsync(CashId);

        balance.Should().Be(DomainMoney.GHS(6000));
    }

    [Fact]
    public async Task Execute_CreditNormalAccount_ReturnsCreditsMinusDebits()
    {
        var lines = new List<EntryLine>
        {
            EntryLine.Credit(RevenueId, DomainMoney.GHS(8000)),
            EntryLine.Debit(RevenueId, DomainMoney.GHS(1000))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(RevenueId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines);

        var balance = await _useCase.ExecuteAsync(RevenueId);

        balance.Should().Be(DomainMoney.GHS(7000));
    }

    [Fact]
    public async Task Execute_NoEntries_ReturnsZero()
    {
        var balance = await _useCase.ExecuteAsync(CashId);

        balance.Should().Be(DomainMoney.Zero(Currency.GHS));
    }

    [Fact]
    public async Task Execute_WithAsOfDate_PassesDateToRepository()
    {
        var asOf = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero);

        await _useCase.ExecuteAsync(CashId, asOf);

        _ledgerRepo.Verify(r => r.GetPostedLinesForAccountAsync(CashId, asOf, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_AccountNotFound_ThrowsAccountNotFoundException()
    {
        var unknownId = Guid.NewGuid();
        _accountRepo.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var act = () => _useCase.ExecuteAsync(unknownId);

        await act.Should().ThrowAsync<AccountNotFoundException>();
    }

    [Fact]
    public async Task Execute_OnlyDebits_ReturnsFullDebitTotal()
    {
        var lines = new List<EntryLine>
        {
            EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
            EntryLine.Debit(CashId, DomainMoney.GHS(2000))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(CashId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines);

        var balance = await _useCase.ExecuteAsync(CashId);

        balance.Should().Be(DomainMoney.GHS(3000));
    }
}
