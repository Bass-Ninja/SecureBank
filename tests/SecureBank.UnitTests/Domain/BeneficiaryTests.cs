using SecureBank.Domain.Entities;

namespace SecureBank.UnitTests.Domain;

public class BeneficiaryTests
{
    private static readonly Guid UserId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid AccountId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Create_ShouldCreateBeneficiary()
    {
        var beneficiary = Beneficiary.Create(UserId, AccountId, "Mom");

        Assert.NotEqual(Guid.Empty, beneficiary.Id);
        Assert.Equal(UserId, beneficiary.UserId);
        Assert.Equal(AccountId, beneficiary.AccountId);
        Assert.Equal("Mom", beneficiary.Nickname);
    }

    [Fact]
    public void Create_ShouldRejectEmptyUserId()
    {
        Assert.Throws<ArgumentException>(
            () => Beneficiary.Create(Guid.Empty, AccountId, "Mom"));
    }

    [Fact]
    public void Create_ShouldRejectEmptyAccountId()
    {
        Assert.Throws<ArgumentException>(
            () => Beneficiary.Create(UserId, Guid.Empty, "Mom"));
    }

    [Fact]
    public void Create_ShouldRejectBlankNickname()
    {
        Assert.Throws<ArgumentException>(
            () => Beneficiary.Create(UserId, AccountId, "   "));
    }
}
