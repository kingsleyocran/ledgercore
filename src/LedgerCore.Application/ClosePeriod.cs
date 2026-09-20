using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Periods;

namespace LedgerCore.Application;

public class ClosePeriodUseCase
{
    private readonly IPeriodRepository _periods;

    public ClosePeriodUseCase(IPeriodRepository periods)
    {
        _periods = periods;
    }

    public async Task<Period> ExecuteAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var period = await _periods.GetAsync(year, month, cancellationToken)
            ?? new Period(year, month);

        period.Close();

        await _periods.SaveAsync(period, cancellationToken);

        return period;
    }
}
