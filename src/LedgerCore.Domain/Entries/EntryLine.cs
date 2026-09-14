using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Money;

namespace LedgerCore.Domain.Entries;

public sealed record EntryLine
{
    public Guid AccountId { get; }
    public Money.Money Amount { get; }
    public DebitOrCredit Type { get; }
    public decimal? FxRate { get; }

    private EntryLine(Guid accountId, Money.Money amount, DebitOrCredit type, decimal? fxRate)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account ID must not be empty.", nameof(accountId));
        ArgumentNullException.ThrowIfNull(amount);
        if (!amount.IsPositive)
            throw new ArgumentException("Entry line amount must be positive.", nameof(amount));

        AccountId = accountId;
        Amount = amount;
        Type = type;
        FxRate = fxRate;
    }

    public static EntryLine Debit(Guid accountId, Money.Money amount, decimal? fxRate = null)
        => new(accountId, amount, DebitOrCredit.Debit, fxRate);

    public static EntryLine Credit(Guid accountId, Money.Money amount, decimal? fxRate = null)
        => new(accountId, amount, DebitOrCredit.Credit, fxRate);
}
