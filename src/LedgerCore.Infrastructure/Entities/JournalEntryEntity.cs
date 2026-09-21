namespace LedgerCore.Infrastructure.Entities;

internal class JournalEntryEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset EntryDate { get; set; }
    public string Description { get; set; } = "";
    public string Reference { get; set; } = "";
    public int Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PostedAt { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public List<EntryLineEntity> Lines { get; set; } = new();
}
