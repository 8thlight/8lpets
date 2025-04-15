using System.ComponentModel.DataAnnotations;

namespace _8lpets.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public int Price { get; set; }

        public string Type { get; set; } = string.Empty; // Food, Toy, etc.

        public string ImageUrl { get; set; } = string.Empty;

        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
