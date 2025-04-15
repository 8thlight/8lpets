using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _8lpets.Pages
{
    public class PlayActivity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int HappinessBoost { get; set; }
        public int EnergyUsed { get; set; }
    }

    public class PlayWithPetModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;
        private readonly Random _random = new Random();

        public PlayWithPetModel(_8lpetsDbContext context)
        {
            _context = context;

            // Initialize play activities
            PlayActivities = new List<PlayActivity>
            {
                new PlayActivity
                {
                    Id = 1,
                    Name = "Fetch",
                    Description = "Play a game of fetch with your pet",
                    Icon = "fas fa-baseball",
                    HappinessBoost = 10,
                    EnergyUsed = 5
                },
                new PlayActivity
                {
                    Id = 2,
                    Name = "Hide and Seek",
                    Description = "Play hide and seek around the house",
                    Icon = "fas fa-eye-slash",
                    HappinessBoost = 15,
                    EnergyUsed = 10
                },
                new PlayActivity
                {
                    Id = 3,
                    Name = "Tug of War",
                    Description = "Play tug of war with a rope toy",
                    Icon = "fas fa-hands",
                    HappinessBoost = 12,
                    EnergyUsed = 8
                },
                new PlayActivity
                {
                    Id = 4,
                    Name = "Belly Rubs",
                    Description = "Give your pet some belly rubs",
                    Icon = "fas fa-hand-sparkles",
                    HappinessBoost = 8,
                    EnergyUsed = 2
                },
                new PlayActivity
                {
                    Id = 5,
                    Name = "Obstacle Course",
                    Description = "Run through an obstacle course together",
                    Icon = "fas fa-running",
                    HappinessBoost = 20,
                    EnergyUsed = 15
                }
            };
        }

        public Pet? Pet { get; set; }
        public List<PlayActivity> PlayActivities { get; set; }
        public List<Item> ToyItems { get; set; } = new List<Item>();
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

            // Get toy items from inventory
            ToyItems = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id && i.Type == "Toy")
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, int activityId)
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

            // Find the selected activity
            var activity = PlayActivities.FirstOrDefault(a => a.Id == activityId);
            if (activity == null)
            {
                ErrorMessage = "Invalid activity selected.";
                return await OnGetAsync(id);
            }

            // Increase happiness
            int happinessBoost = activity.HappinessBoost;

            // Add a small random factor
            happinessBoost += _random.Next(-2, 3);

            // Ensure happiness doesn't exceed 100
            Pet.Happiness = Math.Min(100, Pet.Happiness + happinessBoost);

            // Decrease hunger slightly (playing makes pets hungry)
            Pet.Hunger = Math.Max(0, Pet.Hunger - activity.EnergyUsed / 2);

            // Save changes
            await _context.SaveChangesAsync();

            // Set success message
            SuccessMessage = $"You played {activity.Name} with {Pet.Name}! Happiness increased by {happinessBoost} points.";

            // Get toy items from inventory for the view
            ToyItems = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id && i.Type == "Toy")
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUseToyAsync(int id, int itemId)
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

            // Get the toy item
            var toyItem = await _context.Items
                .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == CurrentUser.Id && i.Type == "Toy");

            if (toyItem == null)
            {
                ErrorMessage = "Toy not found or you don't have permission to use this toy.";
                return await OnGetAsync(id);
            }

            // Increase happiness (toys give a bigger boost)
            int happinessBoost = 25;

            // Add a small random factor
            happinessBoost += _random.Next(-5, 6);

            // Ensure happiness doesn't exceed 100
            Pet.Happiness = Math.Min(100, Pet.Happiness + happinessBoost);

            // Remove the toy from inventory
            _context.Items.Remove(toyItem);

            // Save changes
            await _context.SaveChangesAsync();

            // Set success message
            SuccessMessage = $"You played with {Pet.Name} using the {toyItem.Name}! Happiness increased by {happinessBoost} points.";

            // Get remaining toy items from inventory for the view
            ToyItems = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id && i.Type == "Toy")
                .ToListAsync();

            return Page();
        }
    }
}
