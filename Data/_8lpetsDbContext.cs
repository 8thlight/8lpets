using Microsoft.EntityFrameworkCore;
using _8lpets.Models;

namespace _8lpets.Data
{
    public class _8lpetsDbContext : DbContext
    {
        public _8lpetsDbContext(DbContextOptions<_8lpetsDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Pet> Pets { get; set; } = null!;
        public DbSet<Item> Items { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships
            modelBuilder.Entity<Pet>()
                .HasOne(p => p.User)
                .WithMany(u => u.Pets)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<Item>()
                .HasOne(i => i.User)
                .WithMany(u => u.Inventory)
                .HasForeignKey(i => i.UserId);

            // Seed some initial data
            modelBuilder.Entity<Item>().HasData(
                new Item { Id = 1, Name = "Omelette", Description = "A tasty omelette to feed your pet", Price = 100, Type = "Food", ImageUrl = "/images/items/omelette.png" },
                new Item { Id = 2, Name = "Plushie", Description = "A cute plushie toy", Price = 300, Type = "Toy", ImageUrl = "/images/items/plushie.png" },
                new Item { Id = 3, Name = "Health Potion", Description = "Restores your pet's health", Price = 500, Type = "Medicine", ImageUrl = "/images/items/potion.png" }
            );
        }
    }
}
