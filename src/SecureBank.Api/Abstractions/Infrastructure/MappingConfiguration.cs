using Mapster;
using SecureBank.Api.Models.Accounts;
using SecureBank.Api.Models.Transfers;
using SecureBank.Application.Queries.Accounts.Results;
using SecureBank.Application.Queries.Transfers.Results;

namespace SecureBank.Api.Abstractions.Infrastructure;

public sealed class MappingConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TransferResult, TransferResponse>();
        config.NewConfig<AccountResult, AccountResponse>();
    }
}
