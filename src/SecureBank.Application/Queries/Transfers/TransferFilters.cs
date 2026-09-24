using SecureBank.Domain.Entities;

namespace SecureBank.Application.Queries.Transfers;

public static class TransferFilters
{
    public static IQueryable<Transfer> Apply(
        this IQueryable<Transfer> query,
        GetTransfersQuery request)
    {
        if (request.Status is not null)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        if (request.AccountId is not null)
        {
            query = query.Where(x =>
                x.SourceAccountId == request.AccountId
                || x.DestinationAccountId == request.AccountId);
        }

        return query;
    }

    public static IOrderedQueryable<Transfer> ApplySorting(
        this IQueryable<Transfer> query,
        GetTransfersQuery request)
    {
        var descending = !string.Equals(
            request.SortDirection,
            "asc",
            StringComparison.OrdinalIgnoreCase);

        return request.SortBy?.ToLowerInvariant() switch
        {
            "amount" when descending => query
                .OrderByDescending(x => x.Amount.Amount)
                .ThenByDescending(x => x.Id),
            "amount" => query
                .OrderBy(x => x.Amount.Amount)
                .ThenBy(x => x.Id),
            "status" when descending => query
                .OrderByDescending(x => x.Status)
                .ThenByDescending(x => x.Id),
            "status" => query
                .OrderBy(x => x.Status)
                .ThenBy(x => x.Id),
            "createdat" when !descending => query
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id),
            null when !descending => query
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id),
            _ => query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
        };
    }
}
