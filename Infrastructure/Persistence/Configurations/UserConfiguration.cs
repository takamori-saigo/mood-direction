using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Nickname)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.Age)
            .IsRequired();

        builder.Property(u => u.Gender)
            .IsRequired();

        builder.Property(u => u.IsAdmin)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
