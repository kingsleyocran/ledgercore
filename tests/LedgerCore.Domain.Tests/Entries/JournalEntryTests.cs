using FluentAssertions;
using LedgerCore.Domain.Entries;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;
using DomainMoney = LedgerCore.Domain.Money.Money;

namespace LedgerCore.Domain.Tests.Entries;

public class JournalEntryTests
{
    private static readonly Guid CashId = Guid.NewGuid();
    private static readonly Guid RevenueId = Guid.NewGuid();

    private static EntryLine[] BalancedLines(long amount = 1000) =>
    [
        EntryLine.Debit(CashId, DomainMoney.GHS(amount)),
        EntryLine.Credit(RevenueId, DomainMoney.GHS(amount))
    ];

    // --- Creation ---

    [Fact]
    public void Create_BalancedLines_StoresAllProperties()
    {
        var date = DateTimeOffset.UtcNow;
        var entry = JournalEntry.Create(date, "Sale", "INV-001", BalancedLines());

        entry.Id.Should().NotBe(Guid.Empty);
        entry.EntryDate.Should().Be(date);
        entry.Description.Should().Be("Sale");
        entry.Reference.Should().Be("INV-001");
        entry.Status.Should().Be(EntryStatus.Pending);
        entry.Lines.Should().HaveCount(2);
        entry.PostedAt.Should().BeNull();
        entry.VoidedAt.Should().BeNull();
        entry.VoidReason.Should().BeNull();
    }

    [Fact]
    public void Create_SetsCreatedAt()
    {
        var before = DateTimeOffset.UtcNow;
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());
        var after = DateTimeOffset.UtcNow;

        entry.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var a = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());
        var b = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-002", BalancedLines());

        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void Create_LinesAreImmutable()
    {
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());

        entry.Lines.Should().BeAssignableTo<IReadOnlyList<EntryLine>>();
    }

    // --- Validation ---

    [Fact]
    public void Create_UnbalancedLines_ThrowsUnbalancedEntryException()
    {
        var lines = new[]
        {
            EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
            EntryLine.Credit(RevenueId, DomainMoney.GHS(500))
        };

        var act = () => JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", lines);

        act.Should().Throw<UnbalancedEntryException>();
    }

    [Fact]
    public void Create_SingleLine_Throws()
    {
        var lines = new[] { EntryLine.Debit(CashId, DomainMoney.GHS(1000)) };

        var act = () => JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", lines);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyLines_Throws()
    {
        var act = () => JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", Array.Empty<EntryLine>());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidReference_Throws(string? reference)
    {
        var act = () => JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", reference!, BalancedLines());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidDescription_Throws(string? description)
    {
        var act = () => JournalEntry.Create(DateTimeOffset.UtcNow, description!, "INV-001", BalancedLines());

        act.Should().Throw<ArgumentException>();
    }

    // --- Multi-line balanced ---

    [Fact]
    public void Create_MultipleDebitsOneCredit_Balanced_Succeeds()
    {
        var expenseId = Guid.NewGuid();
        var lines = new[]
        {
            EntryLine.Debit(CashId, DomainMoney.GHS(600)),
            EntryLine.Debit(expenseId, DomainMoney.GHS(400)),
            EntryLine.Credit(RevenueId, DomainMoney.GHS(1000))
        };

        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Split", "INV-002", lines);

        entry.Lines.Should().HaveCount(3);
    }

    // --- Post ---

    [Fact]
    public void Post_FromPending_SetsStatusToPosted()
    {
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());

        entry.Post();

        entry.Status.Should().Be(EntryStatus.Posted);
        entry.PostedAt.Should().NotBeNull();
    }

    [Fact]
    public void Post_FromPosted_ThrowsInvalidTransition()
    {
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());
        entry.Post();

        var act = () => entry.Post();

        act.Should().Throw<InvalidEntryStatusTransitionException>();
    }

    [Fact]
    public void Post_FromVoid_ThrowsInvalidTransition()
    {
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());
        entry.Void("Mistake");

        var act = () => entry.Post();

        act.Should().Throw<InvalidEntryStatusTransitionException>();
    }

    // --- Void ---

    [Fact]
    public void Void_FromPending_SetsStatusToVoid()
    {
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());

        entry.Void("Entered in error");

        entry.Status.Should().Be(EntryStatus.Void);
        entry.VoidedAt.Should().NotBeNull();
        entry.VoidReason.Should().Be("Entered in error");
    }

    [Fact]
    public void Void_FromPosted_SetsStatusToVoid()
    {
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());
        entry.Post();

        entry.Void("Customer refund");

        entry.Status.Should().Be(EntryStatus.Void);
        entry.VoidedAt.Should().NotBeNull();
        entry.VoidReason.Should().Be("Customer refund");
    }

    [Fact]
    public void Void_FromVoid_ThrowsInvalidTransition()
    {
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());
        entry.Void("Mistake");

        var act = () => entry.Void("Again");

        act.Should().Throw<InvalidEntryStatusTransitionException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Void_InvalidReason_Throws(string? reason)
    {
        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", BalancedLines());

        var act = () => entry.Void(reason!);

        act.Should().Throw<ArgumentException>();
    }

    // --- Multi-currency ---

    [Fact]
    public void Create_MultiCurrency_EachCurrencyBalanced_Succeeds()
    {
        var usdReceivableId = Guid.NewGuid();
        var lines = new[]
        {
            EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
            EntryLine.Credit(RevenueId, DomainMoney.GHS(1000)),
            EntryLine.Debit(usdReceivableId, DomainMoney.USD(200)),
            EntryLine.Credit(RevenueId, DomainMoney.USD(200))
        };

        var entry = JournalEntry.Create(DateTimeOffset.UtcNow, "Multi-currency sale", "INV-003", lines);

        entry.Lines.Should().HaveCount(4);
    }

    [Fact]
    public void Create_MultiCurrency_OneCurrencyUnbalanced_Throws()
    {
        var usdReceivableId = Guid.NewGuid();
        var lines = new[]
        {
            EntryLine.Debit(CashId, DomainMoney.GHS(1000)),
            EntryLine.Credit(RevenueId, DomainMoney.GHS(1000)),
            EntryLine.Debit(usdReceivableId, DomainMoney.USD(200)),
            EntryLine.Credit(RevenueId, DomainMoney.USD(100))
        };

        var act = () => JournalEntry.Create(DateTimeOffset.UtcNow, "Multi-currency sale", "INV-004", lines);

        act.Should().Throw<UnbalancedEntryException>();
    }

    // --- Null lines ---

    [Fact]
    public void Create_NullLines_ThrowsArgumentNullException()
    {
        var act = () => JournalEntry.Create(DateTimeOffset.UtcNow, "Sale", "INV-001", null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
