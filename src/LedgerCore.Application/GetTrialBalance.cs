using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Application;

public record TrialBalanceLine(Account Account, DomainMoney DebitBalance, DomainMoney CreditBalance);

public class GetTrialBalanceUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly ILedgerRepository _ledger;

    public GetTrialBalanceUseCase(IAccountRepository accounts, ILedgerRepository ledger)
    {
        _accounts = accounts;
        _ledger = ledger;
    }

    public async Task<IReadOnlyList<TrialBalanceLine>> ExecuteAsync(
        DateTimeOffset? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accounts.GetAllActiveAsync(cancellationToken);
        var result = new List<TrialBalanceLine>();

        foreach (var account in accounts)
        {
            var lines = await _ledger.GetPostedLinesForAccountAsync(account.Id, asOfDate, cancellationToken);
            var balance = CalculateBalance(lines, account);

            var zero = DomainMoney.Zero(account.Currency);

            if (balance.IsPositive || balance.IsZero)
            {
                result.Add(account.Type.NormalBalance() == DebitOrCredit.Debit
                    ? new TrialBalanceLine(account, balance, zero)
                    : new TrialBalanceLine(account, zero, balance));
            }
            else
            {
                var absBalance = balance.Negate();
                result.Add(account.Type.NormalBalance() == DebitOrCredit.Debit
                    ? new TrialBalanceLine(account, zero, absBalance)
                    : new TrialBalanceLine(account, absBalance, zero));
            }
        }

        return result.AsReadOnly();
    }

    private static DomainMoney CalculateBalance(IReadOnlyList<EntryLine> lines, Account account)
    {
        var isDebitNormal = account.Type.NormalBalance() == DebitOrCredit.Debit;
        var totalDebits = DomainMoney.Zero(account.Currency);
        var totalCredits = DomainMoney.Zero(account.Currency);

        foreach (var line in lines)
        {
            if (line.Type == DebitOrCredit.Debit)
                totalDebits = totalDebits + line.Amount;
            else
                totalCredits = totalCredits + line.Amount;
        }

        return isDebitNormal ? totalDebits - totalCredits : totalCredits - totalDebits;
    }
}
