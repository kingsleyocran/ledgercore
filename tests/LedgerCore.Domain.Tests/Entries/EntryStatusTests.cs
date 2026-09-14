using FluentAssertions;
using LedgerCore.Domain.Entries;

namespace LedgerCore.Domain.Tests.Entries;

public class EntryStatusTests
{
    [Fact]
    public void EntryStatus_HasThreeValues()
    {
        Enum.GetValues<EntryStatus>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(EntryStatus.Pending, EntryStatus.Posted, true)]
    [InlineData(EntryStatus.Pending, EntryStatus.Void, true)]
    [InlineData(EntryStatus.Posted, EntryStatus.Void, true)]
    [InlineData(EntryStatus.Posted, EntryStatus.Pending, false)]
    [InlineData(EntryStatus.Void, EntryStatus.Pending, false)]
    [InlineData(EntryStatus.Void, EntryStatus.Posted, false)]
    [InlineData(EntryStatus.Pending, EntryStatus.Pending, false)]
    [InlineData(EntryStatus.Posted, EntryStatus.Posted, false)]
    [InlineData(EntryStatus.Void, EntryStatus.Void, false)]
    public void CanTransitionTo_ReturnsExpectedResult(EntryStatus from, EntryStatus to, bool expected)
    {
        from.CanTransitionTo(to).Should().Be(expected);
    }
}
