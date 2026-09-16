namespace LedgerCore.Domain.Exceptions;

public sealed class AccountNotFoundException : LedgerCoreException
{
    public AccountNotFoundException(Guid accountId)
        : base($"Account '{accountId}' was not found.")
    {
        AccountId = accountId;
    }

    public Guid AccountId { get; }
}
