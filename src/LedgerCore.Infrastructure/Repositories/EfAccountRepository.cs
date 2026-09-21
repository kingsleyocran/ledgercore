using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace LedgerCore.Infrastructure.Repositories;

internal class EfAccountRepository : IAccountRepository
{
    private readonly LedgerDbContext _db;

    public EfAccountRepository(LedgerDbContext db) => _db = db;

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Account>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await _db.Accounts.Where(a => a.IsActive).ToListAsync(cancellationToken);
}
