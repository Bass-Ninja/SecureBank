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
            query = query.Where(x => x.DestinationAccountId == request.AccountId);
        }

        return query;
    }
}
