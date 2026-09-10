using System.Globalization;
using LedgerCore.Domain.Exceptions;

namespace LedgerCore.Domain.Money;

public sealed record Money
{
    public long Amount { get; }
    public Currency Currency { get; }

    private Money(long amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(long amount, Currency currency) => new(amount, currency);
    public static Money Zero(Currency currency) => new(0, currency);

    public static Money GHS(long amount) => Of(amount, Currency.GHS);
    public static Money NGN(long amount) => Of(amount, Currency.NGN);
    public static Money KES(long amount) => Of(amount, Currency.KES);
    public static Money USD(long amount) => Of(amount, Currency.USD);
    public static Money EUR(long amount) => Of(amount, Currency.EUR);
    public static Money GBP(long amount) => Of(amount, Currency.GBP);

    public Money Negate() => new(checked(-Amount), Currency);

    public bool IsZero => Amount == 0;
    public bool IsPositive => Amount > 0;
    public bool IsNegative => Amount < 0;

    public decimal ToMajorUnit()
    {
        if (Currency.DecimalPlaces == 0)
            return Amount;

        var divisor = (decimal)Math.Pow(10, Currency.DecimalPlaces);
        return Amount / divisor;
    }

    public override string ToString()
    {
        var major = ToMajorUnit();
        var format = $"F{Currency.DecimalPlaces}";
        return $"{Currency.Symbol} {major.ToString(format, CultureInfo.InvariantCulture)}";
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new CurrencyMismatchException(Currency, other.Currency);
    }

    public static Money operator +(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return new Money(checked(left.Amount + right.Amount), left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        left.EnsureSameCurrency(right);
        return new Money(checked(left.Amount - right.Amount), left.Currency);
    }

    public static Money operator -(Money money)
        => money.Negate();

    public static Money operator *(Money money, int multiplier)
        => new(checked(money.Amount * multiplier), money.Currency);

    public static Money operator *(int multiplier, Money money)
        => money * multiplier;
}
