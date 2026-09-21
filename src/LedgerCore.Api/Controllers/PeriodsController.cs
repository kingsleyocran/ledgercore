using LedgerCore.Application;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PeriodsController : ControllerBase
{
    private readonly ClosePeriodUseCase _closePeriod;
    private readonly ReopenPeriodUseCase _reopenPeriod;

    public PeriodsController(ClosePeriodUseCase closePeriod, ReopenPeriodUseCase reopenPeriod)
    {
        _closePeriod = closePeriod;
        _reopenPeriod = reopenPeriod;
    }

    [HttpPost("{year:int}/{month:int}/close")]
    public async Task<IActionResult> Close(int year, int month, CancellationToken cancellationToken)
    {
        var period = await _closePeriod.ExecuteAsync(year, month, cancellationToken);
        return Ok(new { period.Year, period.Month, period.IsClosed, period.ClosedAt });
    }

    [HttpPost("{year:int}/{month:int}/reopen")]
    public async Task<IActionResult> Reopen(int year, int month, CancellationToken cancellationToken)
    {
        var period = await _reopenPeriod.ExecuteAsync(year, month, cancellationToken);
        return Ok(new { period.Year, period.Month, period.IsClosed, period.ClosedAt });
    }
}
