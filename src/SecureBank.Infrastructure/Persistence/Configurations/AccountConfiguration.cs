using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.AccountNumber)
            .IsRequired()
            .HasMaxLength(34);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.OwnsOne(
            x => x.Balance,
            money =>
            {
                money.Property(x => x.Amount)
                    .HasColumnName("balance_amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(x => x.Currency)
                    .HasColumnName("balance_currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.AccountNumber)
            .IsUnique();
        
        builder.Property(x => x.Version)
            .IsRowVersion();
    }
}
