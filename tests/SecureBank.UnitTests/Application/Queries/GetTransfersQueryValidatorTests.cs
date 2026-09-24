using SecureBank.Application.Queries.Transfers;

namespace SecureBank.UnitTests.Application.Queries;

public sealed class GetTransfersQueryValidatorTests
{
    private readonly GetTransfersQueryValidator _validator = new();

    [Theory]
    [InlineData("createdAt")]
    [InlineData("amount")]
    [InlineData("status")]
    [InlineData("AMOUNT")]
    public void Validate_WithSupportedSortField_IsValid(string sortBy)
    {
        var query = CreateQuery(sortBy, "asc");

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithUnsupportedSortField_IsInvalid()
    {
        var query = CreateQuery("idempotencyKey", "asc");

        var result = _validator.Validate(query);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetTransfersQuery.SortBy));
    }

    [Fact]
    public void Validate_WithUnsupportedSortDirection_IsInvalid()
    {
        var query = CreateQuery("createdAt", "sideways");

        var result = _validator.Validate(query);

        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(GetTransfersQuery.SortDirection));
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Validate_WithInvalidPagination_IsInvalid(int page, int pageSize)
    {
        var query = new GetTransfersQuery(
            page,
            pageSize,
            null,
            null,
            null,
            null);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
    }

    private static GetTransfersQuery CreateQuery(
        string? sortBy,
        string? sortDirection)
    {
        return new GetTransfersQuery(
            1,
            25,
            sortBy,
            sortDirection,
            null,
            null);
    }
}
