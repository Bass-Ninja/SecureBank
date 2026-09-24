using SecureBank.Application.Commands.Transfers;

namespace SecureBank.UnitTests.Application.Commands;

public sealed class TransferMoneyCommandValidatorTests
{
    private readonly TransferMoneyCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidAccountNumber_IsValid()
    {
        var command = CreateCommand("SI560000000000000002");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SI56-0000-0000-0000")]
    [InlineData("SI56_000000000000002")]
    public void Validate_WithInvalidAccountNumber_IsInvalid(string accountNumber)
    {
        var command = CreateCommand(accountNumber);

        var result = _validator.Validate(command);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(TransferMoneyCommand.DestinationAccountNumber));
    }

    [Fact]
    public void Validate_WithSpacedAccountNumber_IsValid()
    {
        var command = CreateCommand("SI56 0000 0000 0000 0002");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    private static TransferMoneyCommand CreateCommand(string destinationAccountNumber)
    {
        return new TransferMoneyCommand(
            Guid.NewGuid(),
            destinationAccountNumber,
            25m,
            "EUR",
            Guid.NewGuid().ToString());
    }
}
