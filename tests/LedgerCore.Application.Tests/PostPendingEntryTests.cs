using FluentAssertions;
using LedgerCore.Application;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;
using LedgerCore.Domain.Periods;
using Moq;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Application.Tests;

public class PostPendingEntryTests
{
    private readonly Mock<ILedgerRepository> _ledgerRepo = new();
    private readonly Mock<IPeriodRepository> _periodRepo = new();
    private readonly PostPendingEntryUseCase _useCase;

    private static readonly Guid CashId = Guid.NewGuid();
    private static readonly Guid RevenueId = Guid.NewGuid();

    public PostPendingEntryTests()
    {
        _useCase = new PostPendingEntryUseCase(_ledgerRepo.Object, _periodRepo.Object);

        _periodRepo.Setup(r => r.GetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Period?)null);
    }

    private static JournalEntry CreatePendingEntry()
    {
        return JournalEntry.Create(
            DateTimeOffset.UtcNow,
            "Sale",
            "INV-001",
            new[]
            {
                EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
                EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
            });
    }

    [Fact]
    public async Task Execute_PendingEntry_PostsSuccessfully()
    {
        var entry = CreatePendingEntry();
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var result = await _useCase.ExecuteAsync(entry.Id);

        result.Status.Should().Be(EntryStatus.Posted);
        result.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_PersistsUpdatedEntry()
    {
        var entry = CreatePendingEntry();
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        await _useCase.ExecuteAsync(entry.Id);

        _ledgerRepo.Verify(r => r.UpdateAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_EntryNotFound_ThrowsEntryNotFoundException()
    {
        var unknownId = Guid.NewGuid();
        _ledgerRepo.Setup(r => r.GetByIdAsync(unknownId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JournalEntry?)null);

        var act = () => _useCase.ExecuteAsync(unknownId);

        await act.Should().ThrowAsync<EntryNotFoundException>();
    }

    [Fact]
    public async Task Execute_AlreadyPosted_ThrowsInvalidTransition()
    {
        var entry = CreatePendingEntry();
        entry.Post();
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var act = () => _useCase.ExecuteAsync(entry.Id);

        await act.Should().ThrowAsync<InvalidEntryStatusTransitionException>();
    }

    [Fact]
    public async Task Execute_VoidEntry_ThrowsInvalidTransition()
    {
        var entry = CreatePendingEntry();
        entry.Void("Cancelled");
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var act = () => _useCase.ExecuteAsync(entry.Id);

        await act.Should().ThrowAsync<InvalidEntryStatusTransitionException>();
    }

    [Fact]
    public async Task Execute_ClosedPeriod_ThrowsClosedPeriodException()
    {
        var entry = CreatePendingEntry();
        _ledgerRepo.Setup(r => r.GetByIdAsync(entry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var closedPeriod = new Period(entry.EntryDate.Year, entry.EntryDate.Month);
        closedPeriod.Close();
        _periodRepo.Setup(r => r.GetAsync(entry.EntryDate.Year, entry.EntryDate.Month, It.IsAny<CancellationToken>()))
            .ReturnsAsync(closedPeriod);

        var act = () => _useCase.ExecuteAsync(entry.Id);

        await act.Should().ThrowAsync<ClosedPeriodException>();
    }
}
