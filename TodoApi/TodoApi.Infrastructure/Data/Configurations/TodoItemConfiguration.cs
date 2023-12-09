using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApi.Domain.Entities;  // Adjust namespace to where your TodoItem entity is located

namespace TodoApi.Infrastructure.Data.Configurations
{
    public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
    {
        public void Configure(EntityTypeBuilder<TodoItem> builder)
        {
            builder.HasKey(ti => ti.Id);

            builder.Property(ti => ti.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ti => ti.Description)
                .HasMaxLength(500);

            builder.Property(ti => ti.IsComplete)
                .IsRequired();

            // You can add more configurations like relationships, indexes, etc.
        }
    }
}
