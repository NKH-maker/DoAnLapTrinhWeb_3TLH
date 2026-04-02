using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TINH_FINAL_2256.Models;

namespace TINH_FINAL_2256.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public class InputModel
        {
            public string? Token { get; set; }

            [Required]
            public string UserId { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "M?t kh?u và xác nh?n m?t kh?u không kh?p.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? userId, string? token)
        {
            Input.UserId = userId ?? string.Empty;
            Input.Token = token;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user == null)
            {
                StatusMessage = "Không tìm th?y ng??i dùng.";
                return Page();
            }

            var result = await _userManager.ResetPasswordAsync(user, Input.Token ?? string.Empty, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for user {UserId}", Input.UserId);
                StatusMessage = "??t l?i m?t kh?u thành công. B?n có th? ??ng nh?p ngay bây gi?.";
                return RedirectToPage("/Account/Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
