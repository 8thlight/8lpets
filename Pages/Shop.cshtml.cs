using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using _8lpets.Services;

namespace _8lpets.Pages
{
    public class ShopModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;

        public ShopModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        private string GetTimeUntilNextRestock()
        {
            // Calculate time until next restock (10 minutes after last restock)
            var nextRestock = ShopRestockService.LastRestockTime.AddMinutes(10);
            var timeUntil = nextRestock - DateTime.Now;

            // If the time is negative, the restock should happen very soon
            if (timeUntil.TotalSeconds <= 0)
            {
                return "any moment now";
            }

            // Format the time remaining in a user-friendly way
            if (timeUntil.TotalMinutes >= 1)
            {
                return $"{Math.Floor(timeUntil.TotalMinutes)} minutes and {timeUntil.Seconds} seconds";
            }
            else
            {
                return $"{timeUntil.Seconds} seconds";
            }
        }

        public List<Item> Items { get; set; } = new List<Item>();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime LastRestockTime => ShopRestockService.LastRestockTime;
        public string TimeUntilNextRestock => GetTimeUntilNextRestock();

        [BindProperty]
        public int ItemId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Get all items that are available for purchase (not owned by any user)
            Items = await _context.Items
                .Where(i => i.UserId == null)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Get all items for the view
            Items = await _context.Items
                .Where(i => i.UserId == null)
                .ToListAsync();

            var item = await _context.Items.FindAsync(ItemId);
            if (item == null)
            {
                ErrorMessage = "Item not found.";
                return Page();
            }

            if (CurrentUser.NeoPoints < item.Price)
            {
                ErrorMessage = "You don't have enough 8lPoints to buy this item.";
                return Page();
            }

            // Update the item to be owned by the user
            item.UserId = CurrentUser.Id;

            // Deduct the price from the user's NeoPoints
            CurrentUser.NeoPoints -= item.Price;

            // Update user's total items purchased count
            CurrentUser.TotalItemsPurchased++;

            await _context.SaveChangesAsync();

            SuccessMessage = $"You have successfully purchased {item.Name}!";

            return RedirectToPage();
        }
    }
}
