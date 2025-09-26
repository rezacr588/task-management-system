using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Data.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.ToTable("Comments");

            builder.Property(c => c.Content)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(c => c.AuthorDisplayName)
                .HasMaxLength(200);

            builder.Property(c => c.MetadataJson)
                .HasMaxLength(2000);

            builder.HasOne(c => c.TodoItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TodoItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Author)
                .WithMany(u => u.AuthoredComments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
