using LedgerCore.Domain.Entries;

namespace LedgerCore.Domain.Exceptions;

public sealed class InvalidEntryStatusTransitionException : LedgerCoreException
{
    public InvalidEntryStatusTransitionException(EntryStatus from, EntryStatus to)
        : base($"Cannot transition entry from {from} to {to}.")
    {
        From = from;
        To = to;
    }

    public EntryStatus From { get; }
    public EntryStatus To { get; }
}
