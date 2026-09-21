using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Money;
using LedgerCore.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Infrastructure.Repositories;

internal class EfLedgerRepository : ILedgerRepository
{
    private readonly LedgerDbContext _db;

    public EfLedgerRepository(LedgerDbContext db) => _db = db;

    public async Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<JournalEntry?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        var entity = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Reference == reference, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        _db.JournalEntries.Add(ToEntity(entry));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(JournalEntry entry, CancellationToken cancellationToken = default)
    {
        var entity = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstAsync(e => e.Id == entry.Id, cancellationToken);

        entity.Status = (int)entry.Status;
        entity.PostedAt = entry.PostedAt;
        entity.VoidedAt = entry.VoidedAt;
        entity.VoidReason = entry.VoidReason;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EntryLine>> GetPostedLinesForAccountAsync(
        Guid accountId, DateTimeOffset? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var query = _db.EntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId)
            .Where(l => l.JournalEntry.Status == (int)EntryStatus.Posted);

        if (asOfDate.HasValue)
            query = query.Where(l => l.JournalEntry.PostedAt <= asOfDate.Value);

        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(ToEntryLine).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<(JournalEntry Entry, EntryLine Line)>> GetPostedEntryLinesForAccountAsync(
        Guid accountId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        var entities = await _db.EntryLines
            .Include(l => l.JournalEntry)
            .Where(l => l.AccountId == accountId)
            .Where(l => l.JournalEntry.Status == (int)EntryStatus.Posted)
            .Where(l => l.JournalEntry.EntryDate >= startDate)
            .Where(l => l.JournalEntry.EntryDate <= endDate)
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ThenBy(l => l.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(e =>
        {
            var je = e.JournalEntry;
            var entryLines = je.Lines.Select(ToEntryLine).ToList().AsReadOnly();
            var entry = JournalEntry.Reconstitute(
                je.Id, je.EntryDate, je.Description, je.Reference,
                (EntryStatus)je.Status, entryLines, je.CreatedAt,
                je.PostedAt, je.VoidedAt, je.VoidReason);
            return (entry, ToEntryLine(e));
        }).ToList().AsReadOnly();
    }

    private static JournalEntry ToDomain(JournalEntryEntity entity)
    {
        var lines = entity.Lines.Select(ToEntryLine).ToList().AsReadOnly();
        return JournalEntry.Reconstitute(
            entity.Id, entity.EntryDate, entity.Description, entity.Reference,
            (EntryStatus)entity.Status, lines, entity.CreatedAt,
            entity.PostedAt, entity.VoidedAt, entity.VoidReason);
    }

    private static EntryLine ToEntryLine(EntryLineEntity e)
    {
        var money = DomainMoney.Of(e.AmountValue, CurrencyRegistry.Get(e.AmountCurrencyCode));
        return (DebitOrCredit)e.Type == DebitOrCredit.Debit
            ? EntryLine.Debit(e.AccountId, money, e.FxRate)
            : EntryLine.Credit(e.AccountId, money, e.FxRate);
    }

    private static JournalEntryEntity ToEntity(JournalEntry entry) => new()
    {
        Id = entry.Id,
        EntryDate = entry.EntryDate,
        Description = entry.Description,
        Reference = entry.Reference,
        Status = (int)entry.Status,
        CreatedAt = entry.CreatedAt,
        PostedAt = entry.PostedAt,
        VoidedAt = entry.VoidedAt,
        VoidReason = entry.VoidReason,
        Lines = entry.Lines.Select(l => new EntryLineEntity
        {
            AccountId = l.AccountId,
            AmountValue = l.Amount.Amount,
            AmountCurrencyCode = l.Amount.Currency.Code,
            Type = (int)l.Type,
            FxRate = l.FxRate
        }).ToList()
    };
}
