using Microsoft.EntityFrameworkCore;
using _8lpets.Models;
using _8lpets.Services;
using System.Security.Cryptography;
using System.Text;

namespace _8lpets.Data
{
    public static class DbInitializer
    {
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public static void Initialize(_8lpetsDbContext context)
        {
            // Make sure the database is created
            context.Database.EnsureCreated();

            // Check if there are any users
            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            // Add a demo user
            var demoUser = new User
            {
                Username = "demo",
                Email = "demo@example.com",
                PasswordHash = HashPassword("password"),
                NeoPoints = 1000, // TODO: Rename to 8lPoints in the future
                JoinDate = DateTime.Now
            };

            context.Users.Add(demoUser);
            context.SaveChanges();

            // Add some demo pets
            var pets = new Pet[]
            {
                new Pet
                {
                    Name = "Fluffy",
                    Species = "Kacheek",
                    Color = "Blue",
                    Happiness = 70,
                    Hunger = 60,
                    Health = 100,
                    CreatedDate = DateTime.Now,
                    LastFed = DateTime.Now,
                    UserId = demoUser.Id
                },
                new Pet
                {
                    Name = "Spike",
                    Species = "Lupe",
                    Color = "Red",
                    Happiness = 80,
                    Hunger = 40,
                    Health = 90,
                    CreatedDate = DateTime.Now,
                    LastFed = DateTime.Now,
                    UserId = demoUser.Id
                }
            };

            context.Pets.AddRange(pets);
            context.SaveChanges();

            // Add some shop items
            var shopItems = new Item[]
            {
                new Item
                {
                    Name = "Omelette",
                    Description = "A tasty omelette to feed your pet",
                    Price = 100,
                    Type = "Food",
                    ImageUrl = "https://placehold.co/600x400/FFA500/FFFFFF/png?text=Omelette"
                },
                new Item
                {
                    Name = "Pizza",
                    Description = "A delicious pizza slice",
                    Price = 150,
                    Type = "Food",
                    ImageUrl = "https://placehold.co/600x400/FF0000/FFFFFF/png?text=Pizza"
                },
                new Item
                {
                    Name = "Plushie",
                    Description = "A cute plushie toy",
                    Price = 300,
                    Type = "Toy",
                    ImageUrl = "https://placehold.co/600x400/FFC0CB/FFFFFF/png?text=Plushie"
                },
                new Item
                {
                    Name = "Ball",
                    Description = "A bouncy ball for your pet to play with",
                    Price = 200,
                    Type = "Toy",
                    ImageUrl = "https://placehold.co/600x400/0000FF/FFFFFF/png?text=Ball"
                },
                new Item
                {
                    Name = "Health Potion",
                    Description = "Restores your pet's health",
                    Price = 500,
                    Type = "Medicine",
                    ImageUrl = "https://placehold.co/600x400/00FF00/FFFFFF/png?text=Health+Potion"
                }
            };

            context.Items.AddRange(shopItems);
            context.SaveChanges();

            // Add some inventory items for the demo user
            var inventoryItems = new Item[]
            {
                new Item
                {
                    Name = "Omelette",
                    Description = "A tasty omelette to feed your pet",
                    Price = 100,
                    Type = "Food",
                    ImageUrl = "https://placehold.co/600x400/FFA500/FFFFFF/png?text=Omelette",
                    UserId = demoUser.Id
                },
                new Item
                {
                    Name = "Plushie",
                    Description = "A cute plushie toy",
                    Price = 300,
                    Type = "Toy",
                    ImageUrl = "https://placehold.co/600x400/FFC0CB/FFFFFF/png?text=Plushie",
                    UserId = demoUser.Id
                }
            };

            context.Items.AddRange(inventoryItems);
            context.SaveChanges();
        }
    }
}
