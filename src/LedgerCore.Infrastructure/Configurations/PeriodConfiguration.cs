using LedgerCore.Domain.Periods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Infrastructure.Configurations;

internal class PeriodConfiguration : IEntityTypeConfiguration<Period>
{
    public void Configure(EntityTypeBuilder<Period> builder)
    {
        builder.ToTable("periods");

        builder.HasKey(p => new { p.Year, p.Month });
        builder.Property(p => p.Year).HasColumnName("year");
        builder.Property(p => p.Month).HasColumnName("month");
        builder.Property(p => p.IsClosed).HasColumnName("is_closed");
        builder.Property(p => p.ClosedAt).HasColumnName("closed_at");
    }
}
