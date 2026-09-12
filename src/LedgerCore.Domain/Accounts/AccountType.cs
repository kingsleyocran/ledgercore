namespace LedgerCore.Domain.Accounts;

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

public static class AccountTypeExtensions
{
    public static DebitOrCredit NormalBalance(this AccountType type) => type switch
    {
        AccountType.Asset => DebitOrCredit.Debit,
        AccountType.Expense => DebitOrCredit.Debit,
        AccountType.Liability => DebitOrCredit.Credit,
        AccountType.Equity => DebitOrCredit.Credit,
        AccountType.Revenue => DebitOrCredit.Credit,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown account type")
    };
}
