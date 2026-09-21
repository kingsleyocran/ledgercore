using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Periods;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Infrastructure.Repositories;

internal class EfPeriodRepository : IPeriodRepository
{
    private readonly LedgerDbContext _db;

    public EfPeriodRepository(LedgerDbContext db) => _db = db;

    public async Task<Period?> GetAsync(int year, int month, CancellationToken cancellationToken = default)
        => await _db.Periods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month, cancellationToken);

    public async Task SaveAsync(Period period, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Periods.AnyAsync(p => p.Year == period.Year && p.Month == period.Month, cancellationToken);
        if (!exists)
            _db.Periods.Add(period);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
