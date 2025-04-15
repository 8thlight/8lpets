using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _8lpets.Data;
using _8lpets.Models;
using _8lpets.Services;
using System.IO;

namespace _8lpets.Pages.Account
{
    public class ProfileModel : BasePageModel
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;

        public ProfileModel(IUserService userService, IWebHostEnvironment environment)
        {
            _userService = userService;
            _environment = environment;
        }

        public bool HasAvatar { get; set; }
        public string? AvatarUrl { get; set; }

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

                // Check if user has an avatar
                string avatarPath = Path.Combine(_environment.WebRootPath, "images", "avatars", $"user_{user.Id}.jpg");
                if (System.IO.File.Exists(avatarPath))
                {
                    HasAvatar = true;
                    AvatarUrl = $"/images/avatars/user_{user.Id}.jpg?v={DateTime.Now.Ticks}"; // Add cache-busting parameter
                }
            }

            return Page();
        }
    }
}
