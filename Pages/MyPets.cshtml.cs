using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using _8lpets.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _8lpets.Pages
{
    public class MyPetsModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;
        private readonly IUserService _userService;

        public MyPetsModel(_8lpetsDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        private readonly Random _random = new Random();

        public List<Pet> Pets { get; set; } = new List<Pet>();
        public List<Item> FoodItems { get; set; } = new List<Item>();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public int QuickFedPetId { get; set; }

        public async Task<IActionResult> OnGetAsync(string? message = null)
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Set success message if provided
            if (!string.IsNullOrEmpty(message))
            {
                SuccessMessage = message;
            }

            // Get all pets for the current user
            Pets = await _context.Pets
                .Where(p => p.UserId == CurrentUser.Id)
                .ToListAsync();

            // Get food items from inventory
            FoodItems = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id && i.Type == "Food")
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostQuickFeedAsync(int petId)
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Get the pet
            var pet = await _context.Pets
                .FirstOrDefaultAsync(p => p.Id == petId && p.UserId == CurrentUser.Id);

            if (pet == null)
            {
                return RedirectToPage(new { message = "Pet not found or you don't have permission to access this pet." });
            }

            // Get food items from inventory
            var foodItems = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id && i.Type == "Food")
                .ToListAsync();

            if (foodItems.Any())
            {
                // Use the first food item in inventory
                var foodItem = foodItems.First();

                // Increase hunger (premium foods give a bigger boost)
                int hungerBoost = 30;

                // Add a small random factor
                hungerBoost += _random.Next(-5, 6);

                // Ensure hunger doesn't exceed 100
                pet.Hunger = Math.Min(100, pet.Hunger + hungerBoost);

                // Premium foods also increase happiness
                pet.Happiness = Math.Min(100, pet.Happiness + 5);

                // Update last fed time
                pet.LastFed = DateTime.Now;

                // Remove the food from inventory
                _context.Items.Remove(foodItem);

                // Save changes
                await _context.SaveChangesAsync();

                // Set success message
                return RedirectToPage(new { message = $"You fed {pet.Name} with {foodItem.Name}! Hunger increased by {hungerBoost} points." });
            }
            else
            {
                // No food items in inventory, use free food
                int hungerBoost = 15; // Basic kibble

                // Add a small random factor
                hungerBoost += _random.Next(-2, 3);

                // Ensure hunger doesn't exceed 100
                pet.Hunger = Math.Min(100, pet.Hunger + hungerBoost);

                // Free food might decrease happiness slightly
                pet.Happiness = Math.Max(0, Math.Min(100, pet.Happiness - 5));

                // Update last fed time
                pet.LastFed = DateTime.Now;

                // Save changes
                await _context.SaveChangesAsync();

                // Set success message
                return RedirectToPage(new { message = $"You fed {pet.Name} with Basic Kibble! Hunger increased by {hungerBoost} points." });
            }
        }
    }
}
