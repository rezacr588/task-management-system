using System.ComponentModel.DataAnnotations;

namespace TodoApi.Domain.Entities
{ 
    public class Tag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public required string Name { get; set; }

        public virtual ICollection<TodoItem> TodoItems { get; set; } = new HashSet<TodoItem>();
    }
}
