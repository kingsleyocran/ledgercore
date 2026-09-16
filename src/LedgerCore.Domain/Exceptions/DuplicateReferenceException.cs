namespace LedgerCore.Domain.Exceptions;

public sealed class DuplicateReferenceException : LedgerCoreException
{
    public DuplicateReferenceException(string reference)
        : base($"A journal entry with reference '{reference}' already exists.")
    {
        Reference = reference;
    }

    public string Reference { get; }
}
