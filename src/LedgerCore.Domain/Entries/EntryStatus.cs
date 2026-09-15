namespace LedgerCore.Domain.Entries;

public enum EntryStatus
{
    Pending,
    Posted,
    Void
}

public static class EntryStatusExtensions
{
    public static bool CanTransitionTo(this EntryStatus from, EntryStatus to) => (from, to) switch
    {
        (EntryStatus.Pending, EntryStatus.Posted) => true,
        (EntryStatus.Pending, EntryStatus.Void) => true,
        (EntryStatus.Posted, EntryStatus.Void) => true,
        _ => false
    };
}
