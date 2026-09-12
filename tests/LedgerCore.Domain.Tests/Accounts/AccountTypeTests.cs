using FluentAssertions;
using LedgerCore.Domain.Accounts;

namespace LedgerCore.Domain.Tests.Accounts;

public class AccountTypeTests
{
    [Theory]
    [InlineData(AccountType.Asset, DebitOrCredit.Debit)]
    [InlineData(AccountType.Expense, DebitOrCredit.Debit)]
    public void NormalBalance_DebitNormalTypes_ReturnsDebit(AccountType type, DebitOrCredit expected)
    {
        type.NormalBalance().Should().Be(expected);
    }

    [Theory]
    [InlineData(AccountType.Liability, DebitOrCredit.Credit)]
    [InlineData(AccountType.Equity, DebitOrCredit.Credit)]
    [InlineData(AccountType.Revenue, DebitOrCredit.Credit)]
    public void NormalBalance_CreditNormalTypes_ReturnsCredit(AccountType type, DebitOrCredit expected)
    {
        type.NormalBalance().Should().Be(expected);
    }

    [Fact]
    public void AccountType_HasFiveValues()
    {
        Enum.GetValues<AccountType>().Should().HaveCount(5);
    }

    [Fact]
    public void DebitOrCredit_HasTwoValues()
    {
        Enum.GetValues<DebitOrCredit>().Should().HaveCount(2);
    }
}
