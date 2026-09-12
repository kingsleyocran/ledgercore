using LedgerCore.Domain.Accounts;

namespace LedgerCore.Domain.Exceptions;

public sealed class InvalidAccountHierarchyException : LedgerCoreException
{
    public InvalidAccountHierarchyException(AccountType childType, AccountType parentType)
        : base($"Cannot nest {childType} account under {parentType} account. Parent and child must be the same type.")
    {
        ChildType = childType;
        ParentType = parentType;
    }

    public AccountType ChildType { get; }
    public AccountType ParentType { get; }
}
