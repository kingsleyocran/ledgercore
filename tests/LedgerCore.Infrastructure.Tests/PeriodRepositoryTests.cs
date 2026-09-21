using FluentAssertions;
using LedgerCore.Domain.Periods;
using LedgerCore.Infrastructure.Repositories;
using LedgerCore.Infrastructure.Tests.Fixtures;

namespace LedgerCore.Infrastructure.Tests;

[Collection("PostgreSql")]
public class PeriodRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    public PeriodRepositoryTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SaveAndGet_RoundTrips()
    {
        var period = new Period(2099, 1);
        period.Close();

        await using var writeCtx = _fixture.CreateContext();
        var repo = new EfPeriodRepository(writeCtx);
        await repo.SaveAsync(period);

        await using var readCtx = _fixture.CreateContext();
        var readRepo = new EfPeriodRepository(readCtx);
        var loaded = await readRepo.GetAsync(2099, 1);

        loaded.Should().NotBeNull();
        loaded!.IsClosed.Should().BeTrue();
        loaded.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_NotFound_ReturnsNull()
    {
        await using var context = _fixture.CreateContext();
        var repo = new EfPeriodRepository(context);

        var result = await repo.GetAsync(9999, 12);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingPeriod()
    {
        var period = new Period(2098, 6);
        period.Close();

        await using var writeCtx = _fixture.CreateContext();
        await new EfPeriodRepository(writeCtx).SaveAsync(period);

        await using var updateCtx = _fixture.CreateContext();
        var updateRepo = new EfPeriodRepository(updateCtx);
        var loaded = await updateRepo.GetAsync(2098, 6);
        loaded!.Reopen();
        await updateRepo.SaveAsync(loaded);

        await using var readCtx = _fixture.CreateContext();
        var final = await new EfPeriodRepository(readCtx).GetAsync(2098, 6);
        final!.IsClosed.Should().BeFalse();
    }
}
