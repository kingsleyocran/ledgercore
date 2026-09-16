using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;

namespace LedgerCore.Application;

public record VoidEntryCommand(Guid EntryId, string Reason);

public record VoidEntryResult(JournalEntry VoidedEntry, JournalEntry ReversalEntry);

public class VoidEntryUseCase
{
    private readonly ILedgerRepository _ledger;

    public VoidEntryUseCase(ILedgerRepository ledger)
    {
        _ledger = ledger;
    }

    public async Task<VoidEntryResult> ExecuteAsync(
        VoidEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var entry = await _ledger.GetByIdAsync(command.EntryId, cancellationToken)
            ?? throw new EntryNotFoundException(command.EntryId);

        if (entry.Status != EntryStatus.Posted)
            throw new InvalidEntryStatusTransitionException(entry.Status, EntryStatus.Void);

        entry.Void(command.Reason);

        var reversalLines = entry.Lines.Select(line =>
            line.Type == DebitOrCredit.Debit
                ? EntryLine.Credit(line.AccountId, line.Amount, line.FxRate)
                : EntryLine.Debit(line.AccountId, line.Amount, line.FxRate));

        var reversal = JournalEntry.Create(
            entry.EntryDate,
            $"Reversal of: {entry.Description}",
            $"VOID-{entry.Reference}",
            reversalLines);
        reversal.Post();

        await _ledger.UpdateAsync(entry, cancellationToken);
        await _ledger.AddAsync(reversal, cancellationToken);

        return new VoidEntryResult(entry, reversal);
    }
}
