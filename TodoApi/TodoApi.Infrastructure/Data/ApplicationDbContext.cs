using Microsoft.EntityFrameworkCore;
using TodoApi.Infrastructure.Data.Configurations;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Services;

namespace TodoApi.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Domain entities
        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ActivityLogEntry> ActivityLogEntries { get; set; }

        // Event sourcing and infrastructure
        public DbSet<EventStoreEntry> EventStore { get; set; }
        public DbSet<SnapshotStoreEntry> SnapshotStore { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfiguration(new TodoItemConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new TagConfiguration());
            modelBuilder.ApplyConfiguration(new CommentConfiguration());
            modelBuilder.ApplyConfiguration(new ActivityLogEntryConfiguration());
            // Add configurations for other entities as needed
        }
    }
}
