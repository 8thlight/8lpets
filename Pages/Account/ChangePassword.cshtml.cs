using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using _8lpets.Services;

namespace _8lpets.Pages.Account
{
    public class ChangePasswordModel : BasePageModel
    {
        private readonly IUserService _userService;

        public ChangePasswordModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            if (!IsAuthenticated)
            {
                return Page();
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

            // Verify current password
            if (!_userService.VerifyPassword(Input.CurrentPassword, CurrentUser.PasswordHash))
            {
                ModelState.AddModelError("Input.CurrentPassword", "The current password is incorrect.");
                return Page();
            }

            // Update password
            CurrentUser.PasswordHash = _userService.HashPassword(Input.NewPassword);

            // Save changes
            var result = await _userService.UpdateUserAsync(CurrentUser);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Error changing password. Please try again.");
                return Page();
            }

            // Redirect to profile page with success message
            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToPage("/Account/Profile");
        }
    }
}
