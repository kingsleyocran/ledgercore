using LedgerCore.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Infrastructure.Configurations;

internal class JournalEntryEntityConfiguration : IEntityTypeConfiguration<JournalEntryEntity>
{
    public void Configure(EntityTypeBuilder<JournalEntryEntity> builder)
    {
        builder.ToTable("journal_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.EntryDate).HasColumnName("entry_date");
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Reference).HasColumnName("reference").HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Reference).IsUnique();

        builder.Property(e => e.Status).HasColumnName("status");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.PostedAt).HasColumnName("posted_at");
        builder.Property(e => e.VoidedAt).HasColumnName("voided_at");
        builder.Property(e => e.VoidReason).HasColumnName("void_reason").HasMaxLength(500);

        builder.HasMany(e => e.Lines)
            .WithOne(l => l.JournalEntry)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
