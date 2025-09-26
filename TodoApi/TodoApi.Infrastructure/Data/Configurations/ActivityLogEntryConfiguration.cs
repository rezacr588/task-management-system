using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Data.Configurations
{
    public class ActivityLogEntryConfiguration : IEntityTypeConfiguration<ActivityLogEntry>
    {
        public void Configure(EntityTypeBuilder<ActivityLogEntry> builder)
        {
            builder.ToTable("ActivityLogEntries");

            builder.Property(a => a.Summary)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(a => a.Details)
                .HasMaxLength(4000);

            builder.HasOne(a => a.TodoItem)
                .WithMany(t => t.ActivityLogEntries)
                .HasForeignKey(a => a.TodoItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Actor)
                .WithMany(u => u.AuthoredActivityLogEntries)
                .HasForeignKey(a => a.ActorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(a => a.RelatedComment)
                .WithMany()
                .HasForeignKey(a => a.RelatedCommentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
