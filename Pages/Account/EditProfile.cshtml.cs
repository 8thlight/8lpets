using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using _8lpets.Services;
using _8lpets.Models;
using System.IO;

namespace _8lpets.Pages.Account
{
    public class EditProfileModel : BasePageModel
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;

        public EditProfileModel(IUserService userService, IWebHostEnvironment environment)
        {
            _userService = userService;
            _environment = environment;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [BindProperty]
        public IFormFile? AvatarUpload { get; set; }

        public string? CurrentAvatarUrl { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Username")]
            public string Username { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [StringLength(500, ErrorMessage = "The {0} must be at max {1} characters long.")]
            [Display(Name = "Bio")]
            public string? Bio { get; set; }

            [Display(Name = "Favorite Color")]
            public string? FavoriteColor { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsAuthenticated)
            {
                return Page();
            }

            // Get the full user data
            var user = await _userService.GetUserByIdAsync(CurrentUser.Id);
            if (user != null)
            {
                // Update the CurrentUser with the full data
                HttpContext.Items["User"] = user;

                // Populate the input model
                Input.Username = user.Username;
                Input.Email = user.Email;
                Input.Bio = user.Bio;
                Input.FavoriteColor = user.FavoriteColor;

                // Check if user has an avatar
                string avatarPath = Path.Combine(_environment.WebRootPath, "images", "avatars", $"user_{user.Id}.jpg");
                if (System.IO.File.Exists(avatarPath))
                {
                    CurrentAvatarUrl = $"/images/avatars/user_{user.Id}.jpg?v={DateTime.Now.Ticks}"; // Add cache-busting parameter
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if username is already taken by another user
            var existingUser = await _userService.GetUserByUsernameAsync(Input.Username);
            if (existingUser != null && existingUser.Id != CurrentUser.Id)
            {
                ModelState.AddModelError("Input.Username", "Username is already taken.");
                return Page();
            }

            // Check if email is already taken by another user
            existingUser = await _userService.GetUserByEmailAsync(Input.Email);
            if (existingUser != null && existingUser.Id != CurrentUser.Id)
            {
                ModelState.AddModelError("Input.Email", "Email is already taken.");
                return Page();
            }

            // Update user data
            CurrentUser.Username = Input.Username;
            CurrentUser.Email = Input.Email;
            CurrentUser.Bio = Input.Bio;
            CurrentUser.FavoriteColor = Input.FavoriteColor;

            // Handle avatar upload
            if (AvatarUpload != null && AvatarUpload.Length > 0)
            {
                // Validate file size (max 2MB)
                if (AvatarUpload.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("AvatarUpload", "The file is too large. Maximum size is 2MB.");
                    return Page();
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(AvatarUpload.ContentType.ToLower()))
                {
                    ModelState.AddModelError("AvatarUpload", "Only image files (JPG, PNG, GIF) are allowed.");
                    return Page();
                }

                // Ensure directory exists
                string avatarDirectory = Path.Combine(_environment.WebRootPath, "images", "avatars");
                if (!Directory.Exists(avatarDirectory))
                {
                    Directory.CreateDirectory(avatarDirectory);
                }

                // Save the file
                string filePath = Path.Combine(avatarDirectory, $"user_{CurrentUser.Id}.jpg");
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarUpload.CopyToAsync(stream);
                }

                // Update avatar URL for display
                CurrentAvatarUrl = $"/images/avatars/user_{CurrentUser.Id}.jpg?v={DateTime.Now.Ticks}";
            }

            // Save changes to database
            var result = await _userService.UpdateUserAsync(CurrentUser);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Error updating profile. Please try again.");
                return Page();
            }

            // Update the user in the session
            HttpContext.Items["User"] = CurrentUser;

            // Set success message
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";

            return RedirectToPage("/Account/Profile");
        }
    }
}
