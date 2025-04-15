using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;

namespace _8lpets.Pages
{
    public class InventoryModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;

        public InventoryModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        public List<Item> Items { get; set; } = new List<Item>();
        public List<Pet> Pets { get; set; } = new List<Pet>();

        [BindProperty]
        public int ItemId { get; set; }

        [BindProperty]
        public int PetId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Get all items owned by the current user
            Items = await _context.Items
                .Where(i => i.UserId == CurrentUser.Id)
                .ToListAsync();

            // Get all pets owned by the current user
            Pets = await _context.Pets
                .Where(p => p.UserId == CurrentUser.Id)
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

            // Feed the pet with the selected item
            var item = await _context.Items.FindAsync(ItemId);
            var pet = await _context.Pets.FindAsync(PetId);

            if (item == null || pet == null)
            {
                return RedirectToPage();
            }

            // Verify ownership
            if (item.UserId != CurrentUser.Id || pet.UserId != CurrentUser.Id)
            {
                return RedirectToPage();
            }

            // Increase the pet's hunger level
            pet.Hunger = Math.Min(100, pet.Hunger + 20);
            pet.LastFed = DateTime.Now;

            // Remove the item from inventory
            _context.Items.Remove(item);

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPlayWithPetAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            // Play with the pet using the selected toy
            var item = await _context.Items.FindAsync(ItemId);
            var pet = await _context.Pets.FindAsync(PetId);

            if (item == null || pet == null)
            {
                return RedirectToPage();
            }

            // Verify ownership
            if (item.UserId != CurrentUser.Id || pet.UserId != CurrentUser.Id)
            {
                return RedirectToPage();
            }

            // Increase the pet's happiness level
            pet.Happiness = Math.Min(100, pet.Happiness + 20);

            // Remove the item from inventory
            _context.Items.Remove(item);

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
