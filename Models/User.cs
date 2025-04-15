using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace _8lpets.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public int NeoPoints { get; set; } = 1000; // TODO: Rename to 8lPoints in the future

        public DateTime JoinDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Bio { get; set; }

        public string? FavoriteColor { get; set; }

        public DateTime? LastLoginDate { get; set; }

        public int TotalPetsAdopted { get; set; } = 0;

        public int TotalItemsPurchased { get; set; } = 0;

        public List<Pet> Pets { get; set; } = new List<Pet>();

        public List<Item> Inventory { get; set; } = new List<Item>();
    }
}
