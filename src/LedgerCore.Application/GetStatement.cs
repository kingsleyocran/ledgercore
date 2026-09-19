using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Application;

public record StatementLine(
    DateTimeOffset EntryDate,
    string Description,
    string Reference,
    DomainMoney? Debit,
    DomainMoney? Credit,
    DomainMoney RunningBalance);

public class GetStatementUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly ILedgerRepository _ledger;

    public GetStatementUseCase(IAccountRepository accounts, ILedgerRepository ledger)
    {
        _accounts = accounts;
        _ledger = ledger;
    }

    public async Task<IReadOnlyList<StatementLine>> ExecuteAsync(
        Guid accountId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new AccountNotFoundException(accountId);

        var isDebitNormal = account.Type.NormalBalance() == DebitOrCredit.Debit;

        var priorLines = await _ledger.GetPostedLinesForAccountAsync(accountId, startDate.AddTicks(-1), cancellationToken);
        var openingBalance = CalculateBalance(priorLines, account.Currency, isDebitNormal);

        var entryLines = await _ledger.GetPostedEntryLinesForAccountAsync(accountId, startDate, endDate, cancellationToken);

        var runningBalance = openingBalance;
        var result = new List<StatementLine>();

        foreach (var (entry, line) in entryLines)
        {
            if (line.Type == DebitOrCredit.Debit)
                runningBalance = isDebitNormal ? runningBalance + line.Amount : runningBalance - line.Amount;
            else
                runningBalance = isDebitNormal ? runningBalance - line.Amount : runningBalance + line.Amount;

            result.Add(new StatementLine(
                entry.EntryDate,
                entry.Description,
                entry.Reference,
                line.Type == DebitOrCredit.Debit ? line.Amount : null,
                line.Type == DebitOrCredit.Credit ? line.Amount : null,
                runningBalance));
        }

        return result.AsReadOnly();
    }

    private static DomainMoney CalculateBalance(IReadOnlyList<EntryLine> lines, Domain.Money.Currency currency, bool isDebitNormal)
    {
        var totalDebits = DomainMoney.Zero(currency);
        var totalCredits = DomainMoney.Zero(currency);

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
