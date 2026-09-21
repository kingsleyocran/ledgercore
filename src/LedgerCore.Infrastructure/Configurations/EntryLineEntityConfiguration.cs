using LedgerCore.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Infrastructure.Configurations;

internal class EntryLineEntityConfiguration : IEntityTypeConfiguration<EntryLineEntity>
{
    public void Configure(EntityTypeBuilder<EntryLineEntity> builder)
    {
        builder.ToTable("entry_lines");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.JournalEntryId).HasColumnName("journal_entry_id");
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.AmountValue).HasColumnName("amount_value");
        builder.Property(e => e.AmountCurrencyCode).HasColumnName("amount_currency_code").HasMaxLength(3).IsRequired();
        builder.Property(e => e.Type).HasColumnName("type");
        builder.Property(e => e.FxRate).HasColumnName("fx_rate").HasPrecision(18, 8);

        builder.HasIndex(e => e.AccountId);
        builder.HasIndex(e => e.JournalEntryId);
    }
}
