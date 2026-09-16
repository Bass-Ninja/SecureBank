using FluentValidation;

namespace SecureBank.Application.Commands.Beneficiaries;

public sealed class AddBeneficiaryCommandValidator
    : AbstractValidator<AddBeneficiaryCommand>
{
    public AddBeneficiaryCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account is required.");

        RuleFor(x => x.Nickname)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Nickname is required and must not exceed 100 characters.");
    }
}
