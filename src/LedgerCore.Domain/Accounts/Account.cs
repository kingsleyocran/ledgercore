using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;

namespace LedgerCore.Domain.Accounts;

public sealed class Account : IEquatable<Account>
{
    public Guid Id { get; }
    public string Name { get; }
    public string AccountNumber { get; }
    public AccountType Type { get; }
    public Currency Currency { get; }
    public Guid? ParentId { get; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private Account(
        Guid id,
        string name,
        string accountNumber,
        AccountType type,
        Currency currency,
        Guid? parentId,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        AccountNumber = accountNumber;
        Type = type;
        Currency = currency;
        ParentId = parentId;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public static Account Create(
        string name,
        string accountNumber,
        AccountType type,
        Currency currency,
        Guid? parentId = null,
        AccountType? parentType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);
        ArgumentNullException.ThrowIfNull(currency);

        if (parentId.HasValue && !parentType.HasValue)
            throw new ArgumentException("Parent type is required when parent ID is provided.", nameof(parentType));

        if (!parentId.HasValue && parentType.HasValue)
            throw new ArgumentException("Parent ID is required when parent type is provided.", nameof(parentId));

        if (parentId.HasValue && parentType.HasValue && parentType.Value != type)
            throw new InvalidAccountHierarchyException(type, parentType.Value);

        return new Account(
            Guid.NewGuid(),
            name,
            accountNumber,
            type,
            currency,
            parentId,
            DateTimeOffset.UtcNow);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public bool Equals(Account? other)
    {
        if (other is null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as Account);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Account? left, Account? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Account? left, Account? right)
        => !(left == right);
}
