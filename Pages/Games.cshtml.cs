using Microsoft.AspNetCore.Mvc;
using _8lpets.Data;

namespace _8lpets.Pages
{
    public class GamesModel : BasePageModel
    {
        private readonly _8lpetsDbContext _context;

        public GamesModel(_8lpetsDbContext context)
        {
            _context = context;
        }

        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            // Require authentication
            if (!IsAuthenticated)
            {
                return RequireAuthentication();
            }

            return Page();
        }
    }
}
