using SecureBank.Domain.Entities;
using SecureBank.Domain.Enums;
using SecureBank.Domain.ValueObjects;

namespace SecureBank.UnitTests.Domain;

public class AccountTests
{
    private static readonly Guid UserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_ShouldCreateActiveAccount()
    {
        var account = Account.Create(
            UserId,
            "SI56000000000000001",
            Money.Eur(100));

        Assert.NotEqual(Guid.Empty, account.Id);
        Assert.Equal(UserId, account.UserId);
        Assert.Equal("SI56000000000000001", account.AccountNumber);
        Assert.Equal(100, account.Balance.Amount);
        Assert.Equal("EUR", account.Balance.Currency);
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Credit_ShouldIncreaseBalance()
    {
        var account = CreateAccount(100);

        account.Credit(Money.Eur(50));

        Assert.Equal(150, account.Balance.Amount);
    }

    [Fact]
    public void Debit_ShouldDecreaseBalance()
    {
        var account = CreateAccount(100);

        account.Debit(Money.Eur(40));

        Assert.Equal(60, account.Balance.Amount);
    }

    [Fact]
    public void Debit_ShouldRejectInsufficientFunds()
    {
        var account = CreateAccount(100);

        var exception = Assert.Throws<InvalidOperationException>(
            () => account.Debit(Money.Eur(101)));

        Assert.Equal("Insufficient funds.", exception.Message);
    }

    [Fact]
    public void Credit_ShouldRejectDifferentCurrency()
    {
        var account = CreateAccount(100);

        var exception = Assert.Throws<InvalidOperationException>(
            () => account.Credit(
                Money.Create(50, "USD")));

        Assert.Contains("Currency mismatch", exception.Message);
    }

    [Fact]
    public void FrozenAccount_ShouldNotAllowDebit()
    {
        var account = CreateAccount(100);

        account.Freeze();

        Assert.Throws<InvalidOperationException>(
            () => account.Debit(Money.Eur(10)));
    }

    [Fact]
    public void FrozenAccount_ShouldNotAllowCredit()
    {
        var account = CreateAccount(100);

        account.Freeze();

        Assert.Throws<InvalidOperationException>(
            () => account.Credit(Money.Eur(10)));
    }

    [Fact]
    public void Unfreeze_ShouldMakeAccountActive()
    {
        var account = CreateAccount(100);

        account.Freeze();
        account.Unfreeze();

        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void Close_ShouldRejectAccountWithBalance()
    {
        var account = CreateAccount(100);

        var exception = Assert.Throws<InvalidOperationException>(
            () => account.Close());

        Assert.Equal(
            "An account with a non-zero balance cannot be closed.",
            exception.Message);
    }

    [Fact]
    public void Close_ShouldCloseZeroBalanceAccount()
    {
        var account = CreateAccount(100);

        account.Debit(Money.Eur(100));
        account.Close();

        Assert.Equal(AccountStatus.Closed, account.Status);
    }

    [Fact]
    public void ClosedAccount_ShouldNotAllowTransactions()
    {
        var account = CreateAccount(100);

        account.Debit(Money.Eur(100));
        account.Close();

        Assert.Throws<InvalidOperationException>(
            () => account.Credit(Money.Eur(10)));
    }

    private static Account CreateAccount(decimal balance)
    {
        return Account.Create(
            UserId,
            "SI56000000000000001",
            Money.Eur(balance));
    }
}