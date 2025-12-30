using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Persistence.Configurations;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(t => t.CoreThesis)
            .WithMany(c => c.Topics)
            .HasForeignKey(t => t.CoreThesisId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Author)
            .WithMany(u => u.Topics)
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}