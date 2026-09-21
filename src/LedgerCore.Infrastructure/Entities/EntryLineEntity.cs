namespace LedgerCore.Infrastructure.Entities;

internal class EntryLineEntity
{
    public int Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public long AmountValue { get; set; }
    public string AmountCurrencyCode { get; set; } = "";
    public int Type { get; set; }
    public decimal? FxRate { get; set; }
    public JournalEntryEntity JournalEntry { get; set; } = null!;
}
