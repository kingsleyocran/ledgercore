using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Exceptions;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Application;

public class GetBalanceUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly ILedgerRepository _ledger;

    public GetBalanceUseCase(IAccountRepository accounts, ILedgerRepository ledger)
    {
        _accounts = accounts;
        _ledger = ledger;
    }

    public async Task<DomainMoney> ExecuteAsync(
        Guid accountId,
        DateTimeOffset? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        var lines = await _ledger.GetPostedLinesForAccountAsync(accountId, asOfDate, cancellationToken);

        var totalDebits = DomainMoney.Zero(account.Currency);
        var totalCredits = DomainMoney.Zero(account.Currency);

        foreach (var line in lines)
        {
            if (line.Type == DebitOrCredit.Debit)
                totalDebits = totalDebits + line.Amount;
            else
                totalCredits = totalCredits + line.Amount;
        }

        return account.Type.NormalBalance() == DebitOrCredit.Debit
            ? totalDebits - totalCredits
            : totalCredits - totalDebits;
    }
}
