using System;
using System.ComponentModel.DataAnnotations;

namespace _8lpets.Models
{
    public class Pet
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Species { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Color { get; set; } = string.Empty;

        public int Happiness { get; set; } = 50;

        public int Hunger { get; set; } = 50;

        public int Health { get; set; } = 100;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastFed { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
