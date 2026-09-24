using FluentValidation;

namespace SecureBank.Application.Commands.Transfers;

public sealed class TransferMoneyCommandValidator
    : AbstractValidator<TransferMoneyCommand>
{
    public TransferMoneyCommandValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("Source account is required.");

        RuleFor(x => x.DestinationAccountNumber)
            .NotEmpty()
            .MaximumLength(34)
            .Matches("^[A-Za-z0-9 ]+$")
            .WithMessage("Destination account number must contain only letters, numbers, and spaces.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Transfer amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]+$")
            .WithMessage("Currency must be a valid 3-letter code.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage(
                "Idempotency key is required and must not exceed 100 characters.");
    }
}
