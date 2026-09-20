using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Periods;

namespace LedgerCore.Application;

public record PostEntryCommand(
    DateTimeOffset EntryDate,
    string Description,
    string Reference,
    IReadOnlyList<EntryLine> Lines,
    bool PostImmediately = false);

public class PostEntryUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly ILedgerRepository _ledger;
    private readonly IPeriodRepository _periods;

    public PostEntryUseCase(IAccountRepository accounts, ILedgerRepository ledger, IPeriodRepository periods)
    {
        _accounts = accounts;
        _ledger = ledger;
        _periods = periods;
    }

    public async Task<JournalEntry> ExecuteAsync(
        PostEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountIds = command.Lines.Select(l => l.AccountId).Distinct();
        foreach (var accountId in accountIds)
        {
            var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
                throw new AccountNotFoundException(accountId);
            if (!account.IsActive)
                throw new InactiveAccountException(accountId, account.Name);
        }

        var period = await _periods.GetAsync(command.EntryDate.Year, command.EntryDate.Month, cancellationToken);
        if (period is not null && period.IsClosed)
            throw new ClosedPeriodException(period.Year, period.Month);

        var existing = await _ledger.GetByReferenceAsync(command.Reference, cancellationToken);
        if (existing is not null)
            throw new DuplicateReferenceException(command.Reference);

        var entry = JournalEntry.Create(
            command.EntryDate,
            command.Description,
            command.Reference,
            command.Lines);

        if (command.PostImmediately)
            entry.Post();

        await _ledger.AddAsync(entry, cancellationToken);

        return entry;
    }
}
