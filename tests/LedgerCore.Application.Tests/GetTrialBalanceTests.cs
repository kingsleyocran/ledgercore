using FluentAssertions;
using LedgerCore.Application;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Money;
using Moq;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Application.Tests;

public class GetTrialBalanceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ILedgerRepository> _ledgerRepo = new();
    private readonly GetTrialBalanceUseCase _useCase;

    private readonly Account _cash;
    private readonly Account _revenue;
    private readonly Account _expense;

    public GetTrialBalanceTests()
    {
        _useCase = new GetTrialBalanceUseCase(_accountRepo.Object, _ledgerRepo.Object);

        _cash = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);
        _revenue = Account.Create("Revenue", "4000", AccountType.Revenue, Currency.GHS);
        _expense = Account.Create("Rent", "5000", AccountType.Expense, Currency.GHS);

        _accountRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { _cash, _revenue, _expense }.AsReadOnly());

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EntryLine>().AsReadOnly());
    }

    [Fact]
    public async Task Execute_ReturnsLinePerActiveAccount()
    {
        var result = await _useCase.ExecuteAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Execute_DebitNormalAccountWithPositiveBalance_ShowsInDebitColumn()
    {
        var lines = new List<EntryLine>
        {
            EntryLine.Debit(_cash.Id, DomainMoney.GHS(5000)),
            EntryLine.Credit(_cash.Id, DomainMoney.GHS(2000))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(_cash.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines);

        var result = await _useCase.ExecuteAsync();
        var cashLine = result.First(l => l.Account.Id == _cash.Id);

        cashLine.DebitBalance.Should().Be(DomainMoney.GHS(3000));
        cashLine.CreditBalance.Should().Be(DomainMoney.Zero(Currency.GHS));
    }

    [Fact]
    public async Task Execute_CreditNormalAccountWithPositiveBalance_ShowsInCreditColumn()
    {
        var lines = new List<EntryLine>
        {
            EntryLine.Credit(_revenue.Id, DomainMoney.GHS(8000)),
            EntryLine.Debit(_revenue.Id, DomainMoney.GHS(1000))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(_revenue.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lines);

        var result = await _useCase.ExecuteAsync();
        var revenueLine = result.First(l => l.Account.Id == _revenue.Id);

        revenueLine.DebitBalance.Should().Be(DomainMoney.Zero(Currency.GHS));
        revenueLine.CreditBalance.Should().Be(DomainMoney.GHS(7000));
    }

    [Fact]
    public async Task Execute_NoEntries_AllBalancesZero()
    {
        var result = await _useCase.ExecuteAsync();

        foreach (var line in result)
        {
            line.DebitBalance.IsZero.Should().BeTrue();
            line.CreditBalance.IsZero.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Execute_TotalDebitsEqualTotalCredits()
    {
        var cashLines = new List<EntryLine>
        {
            EntryLine.Debit(_cash.Id, DomainMoney.GHS(10000))
        }.AsReadOnly();
        var expenseLines = new List<EntryLine>
        {
            EntryLine.Debit(_expense.Id, DomainMoney.GHS(3000))
        }.AsReadOnly();
        var revenueLines = new List<EntryLine>
        {
            EntryLine.Credit(_revenue.Id, DomainMoney.GHS(13000))
        }.AsReadOnly();

        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(_cash.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cashLines);
        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(_expense.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenseLines);
        _ledgerRepo.Setup(r => r.GetPostedLinesForAccountAsync(_revenue.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revenueLines);

        var result = await _useCase.ExecuteAsync();

        var totalDebits = result.Aggregate(DomainMoney.Zero(Currency.GHS), (sum, l) => sum + l.DebitBalance);
        var totalCredits = result.Aggregate(DomainMoney.Zero(Currency.GHS), (sum, l) => sum + l.CreditBalance);

        totalDebits.Should().Be(totalCredits);
    }

    [Fact]
    public async Task Execute_PassesAsOfDateToRepository()
    {
        var asOf = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero);

        await _useCase.ExecuteAsync(asOf);

        _ledgerRepo.Verify(r => r.GetPostedLinesForAccountAsync(It.IsAny<Guid>(), asOf, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}
