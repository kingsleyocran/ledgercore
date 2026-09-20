using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Periods;

namespace LedgerCore.Application;

public class ReopenPeriodUseCase
{
    private readonly IPeriodRepository _periods;

    public ReopenPeriodUseCase(IPeriodRepository periods)
    {
        _periods = periods;
    }

    public async Task<Period> ExecuteAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var period = await _periods.GetAsync(year, month, cancellationToken)
            ?? throw new InvalidOperationException($"Period {year}-{month:D2} does not exist.");

        period.Reopen();

        await _periods.SaveAsync(period, cancellationToken);

        return period;
    }
}
