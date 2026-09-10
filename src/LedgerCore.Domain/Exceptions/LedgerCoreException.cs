namespace LedgerCore.Domain.Exceptions;

public abstract class LedgerCoreException : Exception
{
    protected LedgerCoreException(string message) : base(message) { }
    protected LedgerCoreException(string message, Exception innerException) : base(message, innerException) { }
}
