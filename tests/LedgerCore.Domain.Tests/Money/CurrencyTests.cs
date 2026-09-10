using FluentAssertions;
using LedgerCore.Domain.Money;

namespace LedgerCore.Domain.Tests.Money;

public class CurrencyTests
{
    [Fact]
    public void Constructor_ValidInputs_StoresProperties()
    {
        var currency = new Currency("GHS", "Ghana Cedi", 2, "₵");

        currency.Code.Should().Be("GHS");
        currency.Name.Should().Be("Ghana Cedi");
        currency.DecimalPlaces.Should().Be(2);
        currency.Symbol.Should().Be("₵");
    }

    [Fact]
    public void Constructor_LowercaseCode_NormalizesToUppercase()
    {
        var currency = new Currency("ghs", "Ghana Cedi", 2, "₵");

        currency.Code.Should().Be("GHS");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidCode_Throws(string? code)
    {
        var act = () => new Currency(code!, "Ghana Cedi", 2, "₵");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidName_Throws(string? name)
    {
        var act = () => new Currency("GHS", name!, 2, "₵");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NegativeDecimalPlaces_Throws()
    {
        var act = () => new Currency("GHS", "Ghana Cedi", -1, "₵");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidSymbol_Throws(string? symbol)
    {
        var act = () => new Currency("GHS", "Ghana Cedi", 2, symbol!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameProperties_AreEqual()
    {
        var a = new Currency("GHS", "Ghana Cedi", 2, "₵");
        var b = new Currency("GHS", "Ghana Cedi", 2, "₵");

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentCode_AreNotEqual()
    {
        var a = new Currency("GHS", "Ghana Cedi", 2, "₵");
        var b = new Currency("NGN", "Nigerian Naira", 2, "₦");

        a.Should().NotBe(b);
    }

    [Fact]
    public void BuiltIn_GHS_HasCorrectProperties()
    {
        Currency.GHS.Code.Should().Be("GHS");
        Currency.GHS.Name.Should().Be("Ghana Cedi");
        Currency.GHS.DecimalPlaces.Should().Be(2);
        Currency.GHS.Symbol.Should().Be("₵");
    }

    [Fact]
    public void BuiltIn_NGN_HasCorrectProperties()
    {
        Currency.NGN.Code.Should().Be("NGN");
        Currency.NGN.Name.Should().Be("Nigerian Naira");
        Currency.NGN.DecimalPlaces.Should().Be(2);
        Currency.NGN.Symbol.Should().Be("₦");
    }

    [Fact]
    public void BuiltIn_XOF_HasZeroDecimalPlaces()
    {
        Currency.XOF.DecimalPlaces.Should().Be(0);
    }

    [Fact]
    public void Constructor_ZeroDecimalPlaces_IsValid()
    {
        var currency = new Currency("XOF", "West African CFA", 0, "CFA");

        currency.DecimalPlaces.Should().Be(0);
    }
}
