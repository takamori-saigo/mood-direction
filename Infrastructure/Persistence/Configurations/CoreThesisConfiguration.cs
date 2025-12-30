using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Persistence.Configurations;

public class CoreThesisConfiguration : IEntityTypeConfiguration<CoreThesis>
{
    public void Configure(EntityTypeBuilder<CoreThesis> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.Order)
            .IsRequired();
    }
}