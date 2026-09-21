using LedgerCore.Api.Dtos;
using LedgerCore.Application;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;
using Microsoft.AspNetCore.Mvc;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EntriesController : ControllerBase
{
    private readonly PostEntryUseCase _postEntry;
    private readonly VoidEntryUseCase _voidEntry;
    private readonly PostPendingEntryUseCase _postPending;
    private readonly ILedgerRepository _ledger;

    public EntriesController(
        PostEntryUseCase postEntry,
        VoidEntryUseCase voidEntry,
        PostPendingEntryUseCase postPending,
        ILedgerRepository ledger)
    {
        _postEntry = postEntry;
        _voidEntry = voidEntry;
        _postPending = postPending;
        _ledger = ledger;
    }

    [HttpPost]
    public async Task<ActionResult<EntryResponse>> Post(
        PostEntryRequest request,
        CancellationToken cancellationToken)
    {
        var lines = request.Lines.Select(l =>
        {
            var money = DomainMoney.Of(l.Amount, CurrencyRegistry.Get(l.CurrencyCode));
            var type = Enum.Parse<DebitOrCredit>(l.Type, ignoreCase: true);
            return type == DebitOrCredit.Debit
                ? EntryLine.Debit(l.AccountId, money, l.FxRate)
                : EntryLine.Credit(l.AccountId, money, l.FxRate);
        }).ToList();

        var command = new PostEntryCommand(
            request.EntryDate, request.Description, request.Reference, lines, request.PostImmediately);

        var entry = await _postEntry.ExecuteAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, ToResponse(entry));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EntryResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _ledger.GetByIdAsync(id, cancellationToken)
            ?? throw new EntryNotFoundException(id);

        return ToResponse(entry);
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<EntryResponse>> PostPending(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _postPending.ExecuteAsync(id, cancellationToken);
        return Ok(ToResponse(entry));
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<object>> Void(Guid id, VoidEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await _voidEntry.ExecuteAsync(new VoidEntryCommand(id, request.Reason), cancellationToken);
        return Ok(new
        {
            voided_entry = ToResponse(result.VoidedEntry),
            reversal_entry = ToResponse(result.ReversalEntry)
        });
    }

    private static EntryResponse ToResponse(JournalEntry e) => new(
        e.Id, e.EntryDate, e.Description, e.Reference, e.Status.ToString(),
        e.Lines.Select(l => new EntryLineResponse(
            l.AccountId, l.Amount.Amount, l.Amount.Currency.Code, l.Type.ToString(), l.FxRate)).ToList(),
        e.CreatedAt, e.PostedAt, e.VoidedAt, e.VoidReason);
}
