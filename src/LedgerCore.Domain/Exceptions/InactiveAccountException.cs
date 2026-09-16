namespace LedgerCore.Domain.Exceptions;

public sealed class InactiveAccountException : LedgerCoreException
{
    public InactiveAccountException(Guid accountId, string accountName)
        : base($"Account '{accountName}' ({accountId}) is inactive and cannot receive entries.")
    {
        AccountId = accountId;
        AccountName = accountName;
    }

    public Guid AccountId { get; }
    public string AccountName { get; }
}
