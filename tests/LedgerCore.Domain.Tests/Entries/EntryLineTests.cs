using FluentAssertions;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Money;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Domain.Tests.Entries;

public class EntryLineTests
{
    private static readonly Guid CashAccountId = Guid.NewGuid();

    [Fact]
    public void Debit_ValidInputs_CreatesDebitLine()
    {
        var line = EntryLine.Debit(CashAccountId, DomainMoney.GHS(1000));

        line.AccountId.Should().Be(CashAccountId);
        line.Amount.Should().Be(DomainMoney.GHS(1000));
        line.Type.Should().Be(DebitOrCredit.Debit);
        line.FxRate.Should().BeNull();
    }

    [Fact]
    public void Credit_ValidInputs_CreatesCreditLine()
    {
        var line = EntryLine.Credit(CashAccountId, DomainMoney.GHS(1000));

        line.AccountId.Should().Be(CashAccountId);
        line.Amount.Should().Be(DomainMoney.GHS(1000));
        line.Type.Should().Be(DebitOrCredit.Credit);
        line.FxRate.Should().BeNull();
    }

    [Fact]
    public void Debit_WithFxRate_StoresFxRate()
    {
        var line = EntryLine.Debit(CashAccountId, DomainMoney.USD(500), 12.5m);

        line.FxRate.Should().Be(12.5m);
    }

    [Fact]
    public void Debit_ZeroAmount_Throws()
    {
        var act = () => EntryLine.Debit(CashAccountId, DomainMoney.Zero(Currency.GHS));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Debit_NegativeAmount_Throws()
    {
        var act = () => EntryLine.Debit(CashAccountId, DomainMoney.Of(-100, Currency.GHS));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Credit_ZeroAmount_Throws()
    {
        var act = () => EntryLine.Credit(CashAccountId, DomainMoney.Zero(Currency.GHS));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Debit_NullAmount_Throws()
    {
        var act = () => EntryLine.Debit(CashAccountId, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equality_SameProperties_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = EntryLine.Debit(id, DomainMoney.GHS(1000));
        var b = EntryLine.Debit(id, DomainMoney.GHS(1000));

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentType_AreNotEqual()
    {
        var id = Guid.NewGuid();
        var debit = EntryLine.Debit(id, DomainMoney.GHS(1000));
        var credit = EntryLine.Credit(id, DomainMoney.GHS(1000));

        debit.Should().NotBe(credit);
    }
}
