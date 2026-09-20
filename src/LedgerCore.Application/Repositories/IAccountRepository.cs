using LedgerCore.Domain.Accounts;

namespace LedgerCore.Application.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Account>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
