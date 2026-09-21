using LedgerCore.Api.Dtos;
using LedgerCore.Application;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Api.Controllers;

[ApiController]
[Route("api")]
public class BalancesController : ControllerBase
{
    private readonly GetBalanceUseCase _getBalance;
    private readonly GetStatementUseCase _getStatement;
    private readonly GetTrialBalanceUseCase _getTrialBalance;

    public BalancesController(
        GetBalanceUseCase getBalance,
        GetStatementUseCase getStatement,
        GetTrialBalanceUseCase getTrialBalance)
    {
        _getBalance = getBalance;
        _getStatement = getStatement;
        _getTrialBalance = getTrialBalance;
    }

    [HttpGet("accounts/{id:guid}/balance")]
    public async Task<ActionResult<BalanceResponse>> GetBalance(
        Guid id, [FromQuery] DateTimeOffset? asOfDate, CancellationToken cancellationToken)
    {
        var balance = await _getBalance.ExecuteAsync(id, asOfDate, cancellationToken);
        return Ok(new BalanceResponse(balance.Amount, balance.Currency.Code, balance.ToString()));
    }

    [HttpGet("accounts/{id:guid}/statement")]
    public async Task<ActionResult<IReadOnlyList<StatementLineResponse>>> GetStatement(
        Guid id, [FromQuery] DateTimeOffset startDate, [FromQuery] DateTimeOffset endDate,
        CancellationToken cancellationToken)
    {
        var lines = await _getStatement.ExecuteAsync(id, startDate, endDate, cancellationToken);
        return Ok(lines.Select(l => new StatementLineResponse(
            l.EntryDate, l.Description, l.Reference,
            l.Debit?.Amount, l.Credit?.Amount,
            l.RunningBalance.Currency.Code, l.RunningBalance.Amount)).ToList());
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<IReadOnlyList<TrialBalanceLineResponse>>> GetTrialBalance(
        [FromQuery] DateTimeOffset? asOfDate, CancellationToken cancellationToken)
    {
        var lines = await _getTrialBalance.ExecuteAsync(asOfDate, cancellationToken);
        return Ok(lines.Select(l => new TrialBalanceLineResponse(
            l.Account.Id, l.Account.Name, l.Account.AccountNumber, l.Account.Type.ToString(),
            l.DebitBalance.Amount, l.CreditBalance.Amount, l.Account.Currency.Code)).ToList());
    }
}
