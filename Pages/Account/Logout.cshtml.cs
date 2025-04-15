using Microsoft.AspNetCore.Mvc;

namespace _8lpets.Pages.Account
{
    public class LogoutModel : BasePageModel
    {
        public IActionResult OnGet()
        {
            // Clear the session
            HttpContext.Session.Clear();

            return Page();
        }

        public IActionResult OnPost()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Add a success message
            TempData["SuccessMessage"] = "You have been logged out successfully.";

            return RedirectToPage("/Index");
        }
    }
}
