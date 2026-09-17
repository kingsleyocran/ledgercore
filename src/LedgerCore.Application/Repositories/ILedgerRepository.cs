using LedgerCore.Domain.Entries;

namespace LedgerCore.Application.Repositories;

public interface ILedgerRepository
{
    Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<JournalEntry?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EntryLine>> GetPostedLinesForAccountAsync(Guid accountId, DateTimeOffset? asOfDate = null, CancellationToken cancellationToken = default);
}
