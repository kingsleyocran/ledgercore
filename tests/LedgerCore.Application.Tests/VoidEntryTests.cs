using FluentAssertions;
using LedgerCore.Application;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;
using Moq;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Application.Tests;

public class VoidEntryTests
{
    private readonly Mock<ILedgerRepository> _ledgerRepo = new();
    private readonly VoidEntryUseCase _useCase;

    private static readonly Guid CashId = Guid.NewGuid();
    private static readonly Guid RevenueId = Guid.NewGuid();

    public VoidEntryTests()
    {
        _useCase = new VoidEntryUseCase(_ledgerRepo.Object);
    }

    private static JournalEntry CreatePostedEntry(string reference = "INV-001")
    {
        var entry = JournalEntry.Create(
            DateTimeOffset.UtcNow,
            "Sale",
            reference,
            new[]
            {
                EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
                EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
            });
        entry.Post();
        return entry;
    }

    [Fact]
    public async Task Execute_PostedEntry_ReturnsVoidedEntryAndReversal()
    {
        var entry = CreatePostedEntry();
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var result = await _useCase.ExecuteAsync(new VoidEntryCommand(entry.Id, "Customer refund"));

        result.VoidedEntry.Status.Should().Be(EntryStatus.Void);
        result.VoidedEntry.VoidReason.Should().Be("Customer refund");
        result.VoidedEntry.VoidedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_ReversalHasSwappedDebitsAndCredits()
    {
        var entry = CreatePostedEntry();
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var result = await _useCase.ExecuteAsync(new VoidEntryCommand(entry.Id, "Refund"));

        var reversal = result.ReversalEntry;
        reversal.Lines.Should().HaveCount(2);

        var originalDebit = entry.Lines.First(l => l.Type == DebitOrCredit.Debit);
        var reversalCredit = reversal.Lines.First(l => l.AccountId == originalDebit.AccountId);
        reversalCredit.Type.Should().Be(DebitOrCredit.Credit);
        reversalCredit.Amount.Should().Be(originalDebit.Amount);

        var originalCredit = entry.Lines.First(l => l.Type == DebitOrCredit.Credit);
        var reversalDebit = reversal.Lines.First(l => l.AccountId == originalCredit.AccountId);
        reversalDebit.Type.Should().Be(DebitOrCredit.Debit);
        reversalDebit.Amount.Should().Be(originalCredit.Amount);
    }

    [Fact]
    public async Task Execute_ReversalIsAutoPosted()
    {
        var entry = CreatePostedEntry();
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var result = await _useCase.ExecuteAsync(new VoidEntryCommand(entry.Id, "Refund"));

        result.ReversalEntry.Status.Should().Be(EntryStatus.Posted);
        result.ReversalEntry.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_ReversalReferenceIsVoidPrefixed()
    {
        var entry = CreatePostedEntry("INV-042");
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var result = await _useCase.ExecuteAsync(new VoidEntryCommand(entry.Id, "Refund"));

        result.ReversalEntry.Reference.Should().Be("VOID-INV-042");
    }

    [Fact]
    public async Task Execute_PersistsBothEntries()
    {
        var entry = CreatePostedEntry();
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        await _useCase.ExecuteAsync(new VoidEntryCommand(entry.Id, "Refund"));

        _ledgerRepo.Verify(r => r.UpdateAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _ledgerRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_EntryNotFound_ThrowsEntryNotFoundException()
    {
        var unknownId = Guid.NewGuid();
        _ledgerRepo.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JournalEntry?)null);

        var act = () => _useCase.ExecuteAsync(new VoidEntryCommand(unknownId, "Refund"));

        var ex = await act.Should().ThrowAsync<EntryNotFoundException>();
        ex.Which.EntryId.Should().Be(unknownId);
    }

    [Fact]
    public async Task Execute_PendingEntry_ThrowsInvalidTransition()
    {
        var entry = JournalEntry.Create(
            DateTimeOffset.UtcNow, "Sale", "INV-001",
            new[]
            {
                EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
                EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
            });
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var act = () => _useCase.ExecuteAsync(new VoidEntryCommand(entry.Id, "Refund"));

        await act.Should().ThrowAsync<InvalidEntryStatusTransitionException>();
    }

    [Fact]
    public async Task Execute_AlreadyVoidEntry_ThrowsInvalidTransition()
    {
        var entry = CreatePostedEntry();
        entry.Void("First void");
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var act = () => _useCase.ExecuteAsync(new VoidEntryCommand(entry.Id, "Again"));

        await act.Should().ThrowAsync<InvalidEntryStatusTransitionException>();
    }
}
