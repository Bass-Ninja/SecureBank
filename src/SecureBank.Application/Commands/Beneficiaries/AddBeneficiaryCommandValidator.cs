using FluentValidation;

namespace SecureBank.Application.Commands.Beneficiaries;

public sealed class AddBeneficiaryCommandValidator
    : AbstractValidator<AddBeneficiaryCommand>
{
    public AddBeneficiaryCommandValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .MaximumLength(34)
            .Matches("^[A-Za-z0-9 ]+$")
            .WithMessage("Account number must contain only letters, numbers, and spaces.");


        RuleFor(x => x.Nickname)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Nickname is required and must not exceed 100 characters.");
    }
}
