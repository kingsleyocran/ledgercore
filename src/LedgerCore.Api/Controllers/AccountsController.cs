using LedgerCore.Api.Dtos;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountRepository _accounts;
    private readonly LedgerCore.Infrastructure.LedgerDbContext _db;

    public AccountsController(IAccountRepository accounts, LedgerCore.Infrastructure.LedgerDbContext db)
    {
        _accounts = accounts;
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var type = Enum.Parse<AccountType>(request.Type, ignoreCase: true);
        var currency = CurrencyRegistry.Get(request.CurrencyCode);

        AccountType? parentType = null;
        if (request.ParentId.HasValue)
        {
            var parent = await _accounts.GetByIdAsync(request.ParentId.Value, cancellationToken)
                ?? throw new AccountNotFoundException(request.ParentId.Value);
            parentType = parent.Type;
        }

        var account = Account.Create(request.Name, request.AccountNumber, type, currency, request.ParentId, parentType);

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, ToResponse(account));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(id, cancellationToken)
            ?? throw new AccountNotFoundException(id);

        return ToResponse(account);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var accounts = await _accounts.GetAllActiveAsync(cancellationToken);
        return Ok(accounts.Select(ToResponse).ToList());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(id, cancellationToken)
            ?? throw new AccountNotFoundException(id);

        account.Deactivate();
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static AccountResponse ToResponse(Account a) => new(
        a.Id, a.Name, a.AccountNumber, a.Type.ToString(), a.Currency.Code, a.ParentId, a.IsActive, a.CreatedAt);
}
