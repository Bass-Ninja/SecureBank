using Mapster;
using SecureBank.Api.Models.Transfers;
using SecureBank.Application.Queries.Transfers.Results;

namespace SecureBank.Api.Abstractions.Infrastructure;

public sealed class MappingConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TransferResult, TransferResponse>();
    }
}
