namespace LedgerCore.Domain.Periods;

public sealed class Period
{
    public int Year { get; }
    public int Month { get; }
    public bool IsClosed { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public Period(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);

        Year = year;
        Month = month;
    }

    public void Close()
    {
        if (IsClosed)
            throw new InvalidOperationException($"Period {Year}-{Month:D2} is already closed.");

        IsClosed = true;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    public void Reopen()
    {
        if (!IsClosed)
            throw new InvalidOperationException($"Period {Year}-{Month:D2} is already open.");

        IsClosed = false;
        ClosedAt = null;
    }
}
