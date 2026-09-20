using FluentAssertions;
using LedgerCore.Application;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Periods;
using Moq;

namespace LedgerCore.Application.Tests;

public class ClosePeriodTests
{
    private readonly Mock<IPeriodRepository> _periodRepo = new();
    private readonly ClosePeriodUseCase _useCase;

    public ClosePeriodTests()
    {
        _useCase = new ClosePeriodUseCase(_periodRepo.Object);
    }

    [Fact]
    public async Task Execute_OpenPeriod_ClosesIt()
    {
        var period = new Period(2026, 6);
        _periodRepo.Setup(r => r.GetAsync(2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        var result = await _useCase.ExecuteAsync(2026, 6);

        result.IsClosed.Should().BeTrue();
        result.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Execute_NoPriorPeriod_CreatesAndClosesIt()
    {
        _periodRepo.Setup(r => r.GetAsync(2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Period?)null);

        var result = await _useCase.ExecuteAsync(2026, 6);

        result.Year.Should().Be(2026);
        result.Month.Should().Be(6);
        result.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task Execute_PersistsPeriod()
    {
        _periodRepo.Setup(r => r.GetAsync(2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Period?)null);

        await _useCase.ExecuteAsync(2026, 6);

        _periodRepo.Verify(r => r.SaveAsync(It.IsAny<Period>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_AlreadyClosed_Throws()
    {
        var period = new Period(2026, 6);
        period.Close();
        _periodRepo.Setup(r => r.GetAsync(2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        var act = () => _useCase.ExecuteAsync(2026, 6);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
