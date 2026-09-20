using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Periods;

namespace LedgerCore.Application;

public class PostPendingEntryUseCase
{
    private readonly ILedgerRepository _ledger;
    private readonly IPeriodRepository _periods;

    public PostPendingEntryUseCase(ILedgerRepository ledger, IPeriodRepository periods)
    {
        _ledger = ledger;
        _periods = periods;
    }

    public async Task<JournalEntry> ExecuteAsync(
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _ledger.GetByIdAsync(entryId, cancellationToken)
            ?? throw new EntryNotFoundException(entryId);

        var period = await _periods.GetAsync(entry.EntryDate.Year, entry.EntryDate.Month, cancellationToken);
        if (period is not null && period.IsClosed)
            throw new ClosedPeriodException(period.Year, period.Month);

        entry.Post();

        await _ledger.UpdateAsync(entry, cancellationToken);

        return entry;
    }
}
