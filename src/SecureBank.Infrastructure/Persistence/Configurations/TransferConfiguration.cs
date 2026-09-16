using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBank.Domain.Entities;

namespace SecureBank.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration
    : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.SourceAccountId)
            .IsRequired();

        builder.Property(x => x.DestinationAccountId)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>();
        
        
        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(x => new
            {
                x.UserId,
                x.IdempotencyKey
            })
            .IsUnique();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.OwnsOne(
            x => x.Amount,
            money =>
            {
                money.Property(x => x.Amount)
                    .HasColumnName("amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                money.Property(x => x.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

        builder.HasIndex(x => x.SourceAccountId);
        builder.HasIndex(x => x.DestinationAccountId);
    }
}
