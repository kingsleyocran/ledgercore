using FluentAssertions;
using LedgerCore.Domain.Periods;

namespace LedgerCore.Domain.Tests.Periods;

public class PeriodTests
{
    [Fact]
    public void Constructor_ValidInputs_StoresProperties()
    {
        var period = new Period(2026, 9);

        period.Year.Should().Be(2026);
        period.Month.Should().Be(9);
        period.IsClosed.Should().BeFalse();
        period.ClosedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Constructor_InvalidMonth_Throws(int month)
    {
        var act = () => new Period(2026, month);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_InvalidYear_Throws()
    {
        var act = () => new Period(0, 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Close_SetsIsClosedAndClosedAt()
    {
        var period = new Period(2026, 9);
        var before = DateTimeOffset.UtcNow;

        period.Close();

        period.IsClosed.Should().BeTrue();
        period.ClosedAt.Should().NotBeNull();
        period.ClosedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Close_AlreadyClosed_Throws()
    {
        var period = new Period(2026, 9);
        period.Close();

        var act = () => period.Close();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reopen_ClosedPeriod_SetsIsOpenAndClearsClosedAt()
    {
        var period = new Period(2026, 9);
        period.Close();

        period.Reopen();

        period.IsClosed.Should().BeFalse();
        period.ClosedAt.Should().BeNull();
    }

    [Fact]
    public void Reopen_AlreadyOpen_Throws()
    {
        var period = new Period(2026, 9);

        var act = () => period.Reopen();

        act.Should().Throw<InvalidOperationException>();
    }
}
