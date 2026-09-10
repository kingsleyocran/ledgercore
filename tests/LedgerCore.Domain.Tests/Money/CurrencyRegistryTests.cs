using FluentAssertions;
using LedgerCore.Domain.Money;

namespace LedgerCore.Domain.Tests.Money;

public class CurrencyRegistryTests : IDisposable
{
    public CurrencyRegistryTests()
    {
        CurrencyRegistry.Reset();
    }

    public void Dispose()
    {
        CurrencyRegistry.Reset();
    }

    [Fact]
    public void Get_BuiltInCurrency_ReturnsCurrency()
    {
        var ghs = CurrencyRegistry.Get("GHS");

        ghs.Should().Be(Currency.GHS);
    }

    [Fact]
    public void Get_CaseInsensitive_ReturnsCurrency()
    {
        var ghs = CurrencyRegistry.Get("ghs");

        ghs.Should().Be(Currency.GHS);
    }

    [Fact]
    public void Get_UnknownCode_ThrowsKeyNotFoundException()
    {
        var act = () => CurrencyRegistry.Get("XYZ");

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*XYZ*");
    }

    [Fact]
    public void TryGet_KnownCode_ReturnsTrueAndCurrency()
    {
        var found = CurrencyRegistry.TryGet("NGN", out var currency);

        found.Should().BeTrue();
        currency.Should().Be(Currency.NGN);
    }

    [Fact]
    public void TryGet_UnknownCode_ReturnsFalse()
    {
        var found = CurrencyRegistry.TryGet("XYZ", out var currency);

        found.Should().BeFalse();
        currency.Should().BeNull();
    }

    [Fact]
    public void Register_CustomCurrency_CanBeRetrieved()
    {
        var btc = new Currency("BTC", "Bitcoin", 8, "₿");

        CurrencyRegistry.Register(btc);

        CurrencyRegistry.Get("BTC").Should().Be(btc);
    }

    [Fact]
    public void Register_DuplicateCode_ThrowsInvalidOperationException()
    {
        var act = () => CurrencyRegistry.Register(
            new Currency("GHS", "Duplicate Cedi", 2, "₵"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*GHS*already registered*");
    }

    [Fact]
    public void All_ReturnsAllRegistered()
    {
        var all = CurrencyRegistry.All();

        all.Should().Contain(Currency.GHS);
        all.Should().Contain(Currency.NGN);
        all.Should().Contain(Currency.USD);
        all.Count.Should().Be(8);
    }

    [Fact]
    public void All_AfterRegister_IncludesCustom()
    {
        var btc = new Currency("BTC", "Bitcoin", 8, "₿");
        CurrencyRegistry.Register(btc);

        CurrencyRegistry.All().Should().Contain(btc);
        CurrencyRegistry.All().Count.Should().Be(9);
    }
}
