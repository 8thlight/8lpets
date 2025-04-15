using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _8lpets.Pages
{
    public class FreeFood
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int HungerBoost { get; set; }
        public int HappinessEffect { get; set; }
    }

    public class FeedPetModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;
        private readonly Random _random = new Random();

        public FeedPetModel(_8lpetsDbContext context)
        {
            _context = context;

            // Initialize free foods
            FreeFoods = new List<FreeFood>
            {
                new FreeFood
                {
                    Id = 1,
                    Name = "Basic Kibble",
                    Description = "Basic pet food, not very tasty but fills the stomach",
                    Icon = "fas fa-bone",
                    HungerBoost = 15,
                    HappinessEffect = -5
                },
                new FreeFood
                {
                    Id = 2,
                    Name = "Fresh Water",
                    Description = "Clean, refreshing water",
                    Icon = "fas fa-tint",
                    HungerBoost = 5,
                    HappinessEffect = 0
                },
                new FreeFood
                {
                    Id = 3,
                    Name = "Vegetable Scraps",
                    Description = "Leftover vegetables, somewhat nutritious",
                    Icon = "fas fa-carrot",
                    HungerBoost = 10,
                    HappinessEffect = -2
                },
                new FreeFood
                {
                    Id = 4,
                    Name = "Fruit Peels",
                    Description = "Sweet fruit peels, not very filling but tasty",
                    Icon = "fas fa-apple-alt",
                    HungerBoost = 8,
                    HappinessEffect = 2
                },
                new FreeFood
                {
                    Id = 5,
                    Name = "Stale Bread",
                    Description = "Day-old bread, still edible",
                    Icon = "fas fa-bread-slice",
                    HungerBoost = 12,
                    HappinessEffect = -3
                }
            };
        }

        public Pet? Pet { get; set; }
        public List<FreeFood> FreeFoods { get; set; }
        public List<Item> FoodItems { get; set; } = new List<Item>();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Get the pet
            Pet = await _context.Pets
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUser.Id);

            if (Pet == null)
            {
                ErrorMessage = "Pet not found or you don't have permission to access this pet.";
                return Page();
            }

            // Get food items from inventory
            FoodItems = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id && i.Type == "Food")
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, int foodId)
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Get the pet
            Pet = await _context.Pets
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUser.Id);

            if (Pet == null)
            {
                ErrorMessage = "Pet not found or you don't have permission to access this pet.";
                return RedirectToPage("/MyPets");
            }

            // Find the selected food
            var food = FreeFoods.FirstOrDefault(f => f.Id == foodId);
            if (food == null)
            {
                ErrorMessage = "Invalid food selected.";
                return await OnGetAsync(id);
            }

            // Increase hunger using the predefined hunger boost value
            int hungerBoost = food.HungerBoost;

            // Add a small random factor (same as food items)
            hungerBoost += _random.Next(-3, 4);

            // Ensure hunger doesn't exceed 100
            Pet.Hunger = Math.Min(100, Pet.Hunger + hungerBoost);

            // Apply happiness effect
            Pet.Happiness = Math.Max(0, Math.Min(100, Pet.Happiness + food.HappinessEffect));

            // Update last fed time
            Pet.LastFed = DateTime.Now;

            // Save changes
            await _context.SaveChangesAsync();

            // Set success message
            string happinessMessage = food.HappinessEffect != 0 ?
                $" and happiness by {food.HappinessEffect} points" : "";
            SuccessMessage = $"You fed {Pet.Name} with {food.Name}! Hunger increased by {hungerBoost} points{happinessMessage}.";

            // Get food items from inventory for the view
            FoodItems = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id && i.Type == "Food")
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUseFoodAsync(int id, int itemId)
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Get the pet
            Pet = await _context.Pets
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUser.Id);

            if (Pet == null)
            {
                ErrorMessage = "Pet not found or you don't have permission to access this pet.";
                return Page();
            }

            // Get the food item
            var foodItem = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == CurrentUser.Id && i.Type == "Food");

            if (foodItem == null)
            {
                ErrorMessage = "Food not found or you don't have permission to use this food.";
                return await OnGetAsync(id);
            }

            // Determine hunger boost based on the food item's price
            // More expensive foods provide better nutrition
            int hungerBoost = 20; // Base value

            // Add bonus based on price
            if (foodItem.Price >= 500)
            {
                hungerBoost += 15; // Premium food
            }
            else if (foodItem.Price >= 300)
            {
                hungerBoost += 10; // Mid-tier food
            }
            else if (foodItem.Price >= 100)
            {
                hungerBoost += 5; // Basic food
            }

            // Add a small random factor
            hungerBoost += _random.Next(-3, 4);

            // Ensure hunger doesn't exceed 100
            Pet.Hunger = Math.Min(100, Pet.Hunger + hungerBoost);

            // Premium foods also increase happiness
            int happinessBoost = 0;
            if (foodItem.Price >= 500)
            {
                happinessBoost = 10;
            }
            else if (foodItem.Price >= 300)
            {
                happinessBoost = 5;
            }
            else if (foodItem.Price >= 100)
            {
                happinessBoost = 2;
            }

            Pet.Happiness = Math.Min(100, Pet.Happiness + happinessBoost);

            // Update last fed time
            Pet.LastFed = DateTime.Now;

            // Remove the food from inventory
            _context.Items.Remove(foodItem);

            // Save changes
            await _context.SaveChangesAsync();

            // Set success message
            string happinessMessage = happinessBoost > 0 ? $" and happiness by {happinessBoost} points" : "";
            SuccessMessage = $"You fed {Pet.Name} with {foodItem.Name}! Hunger increased by {hungerBoost} points{happinessMessage}.";

            // Get remaining food items from inventory for the view
            FoodItems = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id && i.Type == "Food")
                .ToListAsync();

            return Page();
        }
    }
}
