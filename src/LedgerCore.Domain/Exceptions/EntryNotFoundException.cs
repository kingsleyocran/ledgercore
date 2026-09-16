namespace LedgerCore.Domain.Exceptions;

public sealed class EntryNotFoundException : LedgerCoreException
{
    public EntryNotFoundException(Guid entryId)
        : base($"Journal entry '{entryId}' was not found.")
    {
        EntryId = entryId;
    }

    public Guid EntryId { get; }
}
