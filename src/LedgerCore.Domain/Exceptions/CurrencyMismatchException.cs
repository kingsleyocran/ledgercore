using LedgerCore.Domain.Money;

namespace LedgerCore.Domain.Exceptions;

public sealed class CurrencyMismatchException : LedgerCoreException
{
    public CurrencyMismatchException(Currency expected, Currency actual)
        : base($"Cannot operate on {expected.Code} with {actual.Code}. Currencies must match.")
    {
        Expected = expected;
        Actual = actual;
    }

    public Currency Expected { get; }
    public Currency Actual { get; }
}
