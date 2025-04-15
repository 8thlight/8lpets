using Microsoft.AspNetCore.Mvc;
using _8lpets.Data;
using _8lpets.Models;

namespace _8lpets.Pages
{
    public class AdoptPetModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;

        public AdoptPetModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Pet Pet { get; set; } = new Pet();

        public IActionResult OnGet()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Set default values for the new pet
            Pet.UserId = CurrentUser.Id;
            Pet.Happiness = 50;
            Pet.Hunger = 50;
            Pet.Health = 100;
            Pet.CreatedDate = DateTime.Now;
            Pet.LastFed = DateTime.Now;

            _context.Pets.Add(Pet);

            // Update user's total pets adopted count
            CurrentUser.TotalPetsAdopted++;
            _context.Users.Update(CurrentUser);

            await _context.SaveChangesAsync();

            return RedirectToPage("/MyPets");
        }
    }
}
