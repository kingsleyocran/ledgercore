using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Periods;
using LedgerCore.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Infrastructure;

public class LedgerDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Period> Periods => Set<Period>();
    internal DbSet<JournalEntryEntity> JournalEntries => Set<JournalEntryEntity>();
    internal DbSet<EntryLineEntity> EntryLines => Set<EntryLineEntity>();

    public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LedgerDbContext).Assembly);
    }
}
