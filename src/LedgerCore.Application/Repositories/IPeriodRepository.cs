using LedgerCore.Domain.Periods;

namespace LedgerCore.Application.Repositories;

public interface IPeriodRepository
{
    Task<Period?> GetAsync(int year, int month, CancellationToken cancellationToken = default);
    Task SaveAsync(Period period, CancellationToken cancellationToken = default);
}
