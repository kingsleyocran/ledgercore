using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;

namespace LedgerCore.Application;

public class PostPendingEntryUseCase
{
    private readonly ILedgerRepository _ledger;

    public PostPendingEntryUseCase(ILedgerRepository ledger)
    {
        _ledger = ledger;
    }

    public async Task<JournalEntry> ExecuteAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _ledger.GetByIdAsync(entryId, cancellationToken)
            ?? throw new EntryNotFoundException(entryId);

        entry.Post();

        await _ledger.UpdateAsync(entry, cancellationToken);

        return entry;
    }
}
