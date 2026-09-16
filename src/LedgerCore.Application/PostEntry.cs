using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;

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

    public PostEntryUseCase(IAccountRepository accounts, ILedgerRepository ledger)
    {
        _accounts = accounts;
        _ledger = ledger;
    }

    public async Task<JournalEntry> ExecuteAsync(
        PostEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var existing = await _ledger.GetByReferenceAsync(command.Reference, cancellationToken);
        if (existing is not null)
            throw new DuplicateReferenceException(command.Reference);

        var accountIds = command.Lines.Select(l => l.AccountId).Distinct();
        foreach (var accountId in accountIds)
        {
            var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
            if (account is null)
                throw new AccountNotFoundException(accountId);
            if (!account.IsActive)
                throw new InactiveAccountException(accountId, account.Name);
        }

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
