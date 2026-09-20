using FluentAssertions;
using LedgerCore.Application;
using LedgerCore.Application.Repositories;
using LedgerCore.Domain.Periods;
using Moq;

namespace LedgerCore.Application.Tests;

public class ReopenPeriodTests
{
    private readonly Mock<IPeriodRepository> _periodRepo = new();
    private readonly ReopenPeriodUseCase _useCase;

    public ReopenPeriodTests()
    {
        _useCase = new ReopenPeriodUseCase(_periodRepo.Object);
    }

    [Fact]
    public async Task Execute_ClosedPeriod_ReopensIt()
    {
        var period = new Period(2026, 6);
        period.Close();
        _periodRepo.Setup(r => r.GetAsync(2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        var result = await _useCase.ExecuteAsync(2026, 6);

        result.IsClosed.Should().BeFalse();
        result.ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task Execute_PersistsPeriod()
    {
        var period = new Period(2026, 6);
        period.Close();
        _periodRepo.Setup(r => r.GetAsync(2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        await _useCase.ExecuteAsync(2026, 6);

        _periodRepo.Verify(r => r.SaveAsync(It.IsAny<Period>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_NoPeriodFound_Throws()
    {
        _periodRepo.Setup(r => r.GetAsync(2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Period?)null);

        var act = () => _useCase.ExecuteAsync(2026, 6);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Execute_AlreadyOpen_Throws()
    {
        var period = new Period(2026, 6);
        _periodRepo.Setup(r => r.GetAsync(2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(period);

        var act = () => _useCase.ExecuteAsync(2026, 6);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
