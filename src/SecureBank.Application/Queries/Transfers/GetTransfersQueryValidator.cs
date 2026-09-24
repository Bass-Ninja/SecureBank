using FluentValidation;

namespace SecureBank.Application.Queries.Transfers;

public sealed class GetTransfersQueryValidator : AbstractValidator<GetTransfersQuery>
{
    private static readonly string[] SortableFields =
    [
        "createdAt",
        "amount",
        "status"
    ];

    public GetTransfersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy)
            .Must(sortBy => sortBy is null
                || SortableFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be one of: createdAt, amount, status.");

        RuleFor(x => x.SortDirection)
            .Must(direction => direction is null
                || direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be either asc or desc.");
    }
}
