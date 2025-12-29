using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Persistence;

public class MoralCompassDbContext : DbContext
{
    public MoralCompassDbContext(DbContextOptions<MoralCompassDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<CoreThesis> CoreTheses => Set<CoreThesis>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<DiscussionItem> DiscussionItems => Set<DiscussionItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MoralCompassDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}