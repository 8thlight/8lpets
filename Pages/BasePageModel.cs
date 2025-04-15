using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _8lpets.Models;

namespace _8lpets.Pages
{
    public class BasePageModel : PageModel
    {
        public User? CurrentUser => HttpContext.Items["User"] as User;

        public bool IsAuthenticated => CurrentUser != null;

        public IActionResult RequireAuthentication()
        {
            if (!IsAuthenticated)
            {
                return RedirectToPage("/Account/Login", new { returnUrl = Request.Path });
            }

            return Page();
        }
    }
}
