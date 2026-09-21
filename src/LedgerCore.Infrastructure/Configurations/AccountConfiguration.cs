using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Money;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerCore.Infrastructure.Configurations;

internal class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(a => a.AccountNumber).HasColumnName("account_number").HasMaxLength(50).IsRequired();
        builder.HasIndex(a => a.AccountNumber).IsUnique();

        builder.Property(a => a.Type).HasColumnName("type").HasConversion<int>();

        builder.Property(a => a.Currency)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .HasConversion(
                c => c.Code,
                code => CurrencyRegistry.Get(code));

        builder.Property(a => a.ParentId).HasColumnName("parent_id");
        builder.Property(a => a.IsActive).HasColumnName("is_active");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at");
    }
}
