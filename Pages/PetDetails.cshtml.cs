using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using System.Threading.Tasks;

namespace _8lpets.Pages
{
    public class PetDetailsModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;

        public PetDetailsModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        public Pet? Pet { get; set; }

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
                return Page();
            }

            return Page();
        }
    }
}
