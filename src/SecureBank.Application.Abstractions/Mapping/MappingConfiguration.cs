using Mapster;
using SecureBank.Application.Abstractions.Queries.Accounts.Results;
using SecureBank.Application.Abstractions.Queries.Beneficiaries.Results;
using SecureBank.Application.Abstractions.Queries.Transfers.Results;
using SecureBank.Domain.Entities;

namespace SecureBank.Application.Abstractions.Mapping;

public sealed class MappingConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Account, AccountResult>()
            .Map(dest => dest.Balance, src => src.Balance.Amount)
            .Map(dest => dest.Currency, src => src.Balance.Currency)
            .Map(dest => dest.Status, src => src.Status.ToString());

        config.NewConfig<Transfer, TransferResult>()
            .Map(dest => dest.Amount, src => src.Amount.Amount)
            .Map(dest => dest.Currency, src => src.Amount.Currency)
            .Map(dest => dest.Status, src => src.Status.ToString());

        config.NewConfig<Beneficiary, BeneficiaryResult>();
    }
}
