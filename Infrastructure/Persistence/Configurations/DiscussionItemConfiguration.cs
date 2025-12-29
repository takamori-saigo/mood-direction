using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Persistence.Configurations;

public class DiscussionItemConfiguration : IEntityTypeConfiguration<DiscussionItem>
{
    public void Configure(EntityTypeBuilder<DiscussionItem> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Content)
            .IsRequired()
            .HasMaxLength(3000);

        builder.HasOne(d => d.Topic)
            .WithMany(t => t.DiscussionItems)
            .HasForeignKey(d => d.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Author)
            .WithMany()
            .HasForeignKey(d => d.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}