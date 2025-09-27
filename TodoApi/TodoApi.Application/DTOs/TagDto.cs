using System.ComponentModel.DataAnnotations;

namespace TodoApi.Application.DTOs
{
    public class TagDto
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Name { get; set; } = string.Empty;
    }
}
