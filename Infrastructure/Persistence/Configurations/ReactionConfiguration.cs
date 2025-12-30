using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Persistence.Configurations;

public class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Value)
            .IsRequired();

        builder.Property(r => r.TargetType)
            .IsRequired();

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.UserId, r.TargetType, r.TargetId })
            .IsUnique();
    }
}