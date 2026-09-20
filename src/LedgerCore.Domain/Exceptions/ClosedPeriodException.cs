namespace LedgerCore.Domain.Exceptions;

public sealed class ClosedPeriodException : LedgerCoreException
{
    public ClosedPeriodException(int year, int month)
        : base($"Period {year}-{month:D2} is closed. Cannot post entries to a closed period.")
    {
        Year = year;
        Month = month;
    }

    public int Year { get; }
    public int Month { get; }
}
