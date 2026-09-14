namespace LedgerCore.Domain.Exceptions;

public sealed class UnbalancedEntryException : LedgerCoreException
{
    public UnbalancedEntryException(string currencyCode, long totalDebits, long totalCredits)
        : base($"Entry is unbalanced for {currencyCode}: total debits ({totalDebits}) != total credits ({totalCredits}).")
    {
        CurrencyCode = currencyCode;
        TotalDebits = totalDebits;
        TotalCredits = totalCredits;
    }

    public string CurrencyCode { get; }
    public long TotalDebits { get; }
    public long TotalCredits { get; }
}
