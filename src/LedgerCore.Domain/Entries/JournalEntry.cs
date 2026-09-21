using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Exceptions;

namespace LedgerCore.Domain.Entries;

public sealed class JournalEntry
{
    public Guid Id { get; }
    public DateTimeOffset EntryDate { get; }
    public string Description { get; }
    public string Reference { get; }
    public EntryStatus Status { get; private set; }
    public IReadOnlyList<EntryLine> Lines { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? PostedAt { get; private set; }
    public DateTimeOffset? VoidedAt { get; private set; }
    public string? VoidReason { get; private set; }

    private JournalEntry(
        Guid id,
        DateTimeOffset entryDate,
        string description,
        string reference,
        IReadOnlyList<EntryLine> lines,
        DateTimeOffset createdAt)
    {
        Id = id;
        EntryDate = entryDate;
        Description = description;
        Reference = reference;
        Status = EntryStatus.Pending;
        Lines = lines;
        CreatedAt = createdAt;
    }

    public static JournalEntry Create(
        DateTimeOffset entryDate,
        string description,
        string reference,
        IEnumerable<EntryLine> lines)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentNullException.ThrowIfNull(lines);

        var lineList = lines.ToList().AsReadOnly();

        if (lineList.Count < 2)
            throw new ArgumentException("A journal entry must have at least 2 lines.", nameof(lines));

        ValidateBalance(lineList);

        return new JournalEntry(
            Guid.NewGuid(),
            entryDate,
            description,
            reference,
            lineList,
            DateTimeOffset.UtcNow);
    }

    internal static JournalEntry Reconstitute(
        Guid id,
        DateTimeOffset entryDate,
        string description,
        string reference,
        EntryStatus status,
        IReadOnlyList<EntryLine> lines,
        DateTimeOffset createdAt,
        DateTimeOffset? postedAt,
        DateTimeOffset? voidedAt,
        string? voidReason)
    {
        var entry = new JournalEntry(id, entryDate, description, reference, lines, createdAt);
        entry.Status = status;
        entry.PostedAt = postedAt;
        entry.VoidedAt = voidedAt;
        entry.VoidReason = voidReason;
        return entry;
    }

    public void Post()
    {
        EnsureTransition(EntryStatus.Posted);
        Status = EntryStatus.Posted;
        PostedAt = DateTimeOffset.UtcNow;
    }

    public void Void(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        EnsureTransition(EntryStatus.Void);
        Status = EntryStatus.Void;
        VoidedAt = DateTimeOffset.UtcNow;
        VoidReason = reason;
    }

    private void EnsureTransition(EntryStatus to)
    {
        if (!Status.CanTransitionTo(to))
            throw new InvalidEntryStatusTransitionException(Status, to);
    }

    private static void ValidateBalance(IReadOnlyList<EntryLine> lines)
    {
        var byCurrency = lines.GroupBy(l => l.Amount.Currency);

        foreach (var group in byCurrency)
        {
            var totalDebits = group
                .Where(l => l.Type == DebitOrCredit.Debit)
                .Sum(l => l.Amount.Amount);

            var totalCredits = group
                .Where(l => l.Type == DebitOrCredit.Credit)
                .Sum(l => l.Amount.Amount);

            if (totalDebits != totalCredits)
                throw new UnbalancedEntryException(group.Key.Code, totalDebits, totalCredits);
        }
    }
}
