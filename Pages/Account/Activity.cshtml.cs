using Microsoft.AspNetCore.Mvc;
using _8lpets.Services;

namespace _8lpets.Pages.Account
{
    public class ActivityModel : BasePageModel
    {
        private readonly IUserService _userService;

        public ActivityModel(IUserService userService)
        {
            _userService = userService;
        }

        public int DaysActive { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAuthenticated)
            {
                return Page();
            }

            // Get the full user data with pets and inventory
            var user = await _userService.GetUserByIdAsync(CurrentUser.Id);
            if (user != null)
            {
                // Update the CurrentUser with the full data
                HttpContext.Items["User"] = user;

                // Calculate days active
                DaysActive = (int)(DateTime.Now - user.JoinDate).TotalDays + 1;

                // Ensure CurrentUser is not null
                if (CurrentUser == null)
                {
                    return RedirectToPage("/Account/Login");
                }
            }

            return Page();
        }
    }
}
