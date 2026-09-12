using FluentAssertions;
using LedgerCore.Domain.Accounts;
using LedgerCore.Domain.Exceptions;
using LedgerCore.Domain.Money;

namespace LedgerCore.Domain.Tests.Accounts;

public class AccountTests
{
    [Fact]
    public void Create_ValidInputs_StoresAllProperties()
    {
        var account = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);

        account.Name.Should().Be("Cash");
        account.AccountNumber.Should().Be("1000");
        account.Type.Should().Be(AccountType.Asset);
        account.Currency.Should().Be(Currency.GHS);
        account.ParentId.Should().BeNull();
        account.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var a = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);
        var b = Account.Create("Bank", "1001", AccountType.Asset, Currency.GHS);

        a.Id.Should().NotBe(Guid.Empty);
        b.Id.Should().NotBe(Guid.Empty);
        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void Create_SetsCreatedAtToNow()
    {
        var before = DateTimeOffset.UtcNow;
        var account = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);
        var after = DateTimeOffset.UtcNow;

        account.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidName_Throws(string? name)
    {
        var act = () => Account.Create(name!, "1000", AccountType.Asset, Currency.GHS);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidAccountNumber_Throws(string? number)
    {
        var act = () => Account.Create("Cash", number!, AccountType.Asset, Currency.GHS);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NullCurrency_Throws()
    {
        var act = () => Account.Create("Cash", "1000", AccountType.Asset, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithParentSameType_Succeeds()
    {
        var parentId = Guid.NewGuid();

        var account = Account.Create(
            "Cash - GHS", "1010", AccountType.Asset, Currency.GHS,
            parentId, AccountType.Asset);

        account.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void Create_WithParentDifferentType_ThrowsInvalidAccountHierarchy()
    {
        var parentId = Guid.NewGuid();

        var act = () => Account.Create(
            "Cash", "1010", AccountType.Asset, Currency.GHS,
            parentId, AccountType.Liability);

        act.Should().Throw<InvalidAccountHierarchyException>();
    }

    [Fact]
    public void Create_WithParentIdButNoParentType_Throws()
    {
        var parentId = Guid.NewGuid();

        var act = () => Account.Create(
            "Cash", "1010", AccountType.Asset, Currency.GHS,
            parentId, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithParentTypeButNoParentId_Throws()
    {
        var act = () => Account.Create(
            "Cash", "1010", AccountType.Asset, Currency.GHS,
            null, AccountType.Asset);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var account = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);

        account.Deactivate();

        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_CalledTwice_DoesNotThrow()
    {
        var account = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);

        account.Deactivate();
        var act = () => account.Deactivate();

        act.Should().NotThrow();
        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Equality_SameId_AreEqual()
    {
        var account = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);

        account.Should().Be(account);
    }

    [Fact]
    public void Equality_DifferentId_AreNotEqual()
    {
        var a = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);
        var b = Account.Create("Cash", "1000", AccountType.Asset, Currency.GHS);

        a.Should().NotBe(b);
    }
}
