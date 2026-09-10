using FluentAssertions;
using LedgerCore.Domain.Exceptions;
using DomainMoney = LedgerCore.Domain.Money.Money;
using LedgerCore.Domain.Money;

namespace LedgerCore.Domain.Tests.Money;

public class MoneyTests
{
    // --- Creation & Factory Methods ---

    [Fact]
    public void Of_ValidInputs_CreatesMoneyWithCorrectProperties()
    {
        var money = DomainMoney.Of(1000, Currency.GHS);

        money.Amount.Should().Be(1000);
        money.Currency.Should().Be(Currency.GHS);
    }

    [Fact]
    public void Of_NullCurrency_Throws()
    {
        var act = () => DomainMoney.Of(1000, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Of_NegativeAmount_IsValid()
    {
        var money = DomainMoney.Of(-500, Currency.GHS);

        money.Amount.Should().Be(-500);
    }

    [Fact]
    public void Zero_CreatesMoneyWithZeroAmount()
    {
        var money = DomainMoney.Zero(Currency.GHS);

        money.Amount.Should().Be(0);
        money.Currency.Should().Be(Currency.GHS);
    }

    [Fact]
    public void GHS_Shorthand_CreatesGhsMoney()
    {
        var money = DomainMoney.GHS(1000);

        money.Amount.Should().Be(1000);
        money.Currency.Should().Be(Currency.GHS);
    }

    [Fact]
    public void NGN_Shorthand_CreatesNgnMoney()
    {
        var money = DomainMoney.NGN(5000);

        money.Amount.Should().Be(5000);
        money.Currency.Should().Be(Currency.NGN);
    }

    [Fact]
    public void USD_Shorthand_CreatesUsdMoney()
    {
        var money = DomainMoney.USD(250);

        money.Amount.Should().Be(250);
        money.Currency.Should().Be(Currency.USD);
    }

    // --- Equality ---

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = DomainMoney.GHS(1000);
        var b = DomainMoney.GHS(1000);

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentAmount_AreNotEqual()
    {
        var a = DomainMoney.GHS(1000);
        var b = DomainMoney.GHS(2000);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_DifferentCurrency_AreNotEqual()
    {
        var a = DomainMoney.GHS(1000);
        var b = DomainMoney.NGN(1000);

        a.Should().NotBe(b);
    }

    // --- Arithmetic ---

    [Fact]
    public void Add_SameCurrency_ReturnsSumAmount()
    {
        var a = DomainMoney.GHS(1000);
        var b = DomainMoney.GHS(500);

        var result = a + b;

        result.Amount.Should().Be(1500);
        result.Currency.Should().Be(Currency.GHS);
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsCurrencyMismatch()
    {
        var a = DomainMoney.GHS(1000);
        var b = DomainMoney.NGN(500);

        var act = () => a + b;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifferenceAmount()
    {
        var a = DomainMoney.GHS(1000);
        var b = DomainMoney.GHS(300);

        var result = a - b;

        result.Amount.Should().Be(700);
        result.Currency.Should().Be(Currency.GHS);
    }

    [Fact]
    public void Subtract_DifferentCurrency_ThrowsCurrencyMismatch()
    {
        var a = DomainMoney.GHS(1000);
        var b = DomainMoney.NGN(300);

        var act = () => a - b;

        act.Should().Throw<CurrencyMismatchException>();
    }

    [Fact]
    public void Multiply_ByInt_ReturnsScaledAmount()
    {
        var money = DomainMoney.GHS(500);

        var result = money * 3;

        result.Amount.Should().Be(1500);
        result.Currency.Should().Be(Currency.GHS);
    }

    [Fact]
    public void UnaryMinus_NegatesAmount()
    {
        var money = DomainMoney.GHS(1000);

        var result = -money;

        result.Amount.Should().Be(-1000);
        result.Currency.Should().Be(Currency.GHS);
    }

    [Fact]
    public void Negate_ReturnsOppositeSign()
    {
        var money = DomainMoney.GHS(1000);

        var result = money.Negate();

        result.Amount.Should().Be(-1000);
    }

    [Fact]
    public void Negate_NegativeAmount_ReturnsPositive()
    {
        var money = DomainMoney.Of(-1000, Currency.GHS);

        var result = money.Negate();

        result.Amount.Should().Be(1000);
    }

    [Fact]
    public void Multiply_IntTimesMoneyCommutative_ReturnsScaledAmount()
    {
        var money = DomainMoney.GHS(500);

        var result = 3 * money;

        result.Amount.Should().Be(1500);
        result.Currency.Should().Be(Currency.GHS);
    }

    [Fact]
    public void Add_Overflow_ThrowsOverflowException()
    {
        var a = DomainMoney.GHS(long.MaxValue);
        var b = DomainMoney.GHS(1);

        var act = () => a + b;

        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void Subtract_Overflow_ThrowsOverflowException()
    {
        var a = DomainMoney.GHS(long.MinValue);
        var b = DomainMoney.GHS(1);

        var act = () => a - b;

        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void Multiply_Overflow_ThrowsOverflowException()
    {
        var money = DomainMoney.GHS(long.MaxValue);

        var act = () => money * 2;

        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void Negate_LongMinValue_ThrowsOverflowException()
    {
        var money = DomainMoney.Of(long.MinValue, Currency.GHS);

        var act = () => money.Negate();

        act.Should().Throw<OverflowException>();
    }

    // --- Boolean Properties ---

    [Fact]
    public void IsZero_ZeroAmount_ReturnsTrue()
    {
        DomainMoney.Zero(Currency.GHS).IsZero.Should().BeTrue();
    }

    [Fact]
    public void IsZero_NonZeroAmount_ReturnsFalse()
    {
        DomainMoney.GHS(1).IsZero.Should().BeFalse();
    }

    [Fact]
    public void IsPositive_PositiveAmount_ReturnsTrue()
    {
        DomainMoney.GHS(100).IsPositive.Should().BeTrue();
    }

    [Fact]
    public void IsPositive_ZeroAmount_ReturnsFalse()
    {
        DomainMoney.Zero(Currency.GHS).IsPositive.Should().BeFalse();
    }

    [Fact]
    public void IsNegative_NegativeAmount_ReturnsTrue()
    {
        DomainMoney.Of(-100, Currency.GHS).IsNegative.Should().BeTrue();
    }

    [Fact]
    public void IsNegative_ZeroAmount_ReturnsFalse()
    {
        DomainMoney.Zero(Currency.GHS).IsNegative.Should().BeFalse();
    }

    // --- Display ---

    [Fact]
    public void ToMajorUnit_TwoDecimalPlaces_ConvertsPesewasToCedis()
    {
        var money = DomainMoney.GHS(1050);

        money.ToMajorUnit().Should().Be(10.50m);
    }

    [Fact]
    public void ToMajorUnit_ZeroDecimalPlaces_ReturnsWholeNumber()
    {
        var money = DomainMoney.Of(500, Currency.XOF);

        money.ToMajorUnit().Should().Be(500m);
    }

    [Fact]
    public void ToMajorUnit_Zero_ReturnsZero()
    {
        var money = DomainMoney.Zero(Currency.USD);

        money.ToMajorUnit().Should().Be(0m);
    }

    [Fact]
    public void ToString_FormatsWithSymbolAndMajorUnit()
    {
        var money = DomainMoney.GHS(1050);

        money.ToString().Should().Be("₵ 10.50");
    }

    [Fact]
    public void ToString_ZeroDecimalPlaces_NoDecimalPoint()
    {
        var money = DomainMoney.Of(500, Currency.XOF);

        money.ToString().Should().Be("CFA 500");
    }
}
