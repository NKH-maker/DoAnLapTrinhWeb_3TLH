using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TINH_FINAL_2256.Models;

namespace TINH_FINAL_2256.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string? userId, string? code)
        {
            if (userId == null || code == null)
            {
                StatusMessage = "Liên k?t xác th?c không h?p l?.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "Không tìm th?y ng??i dùng.";
                return Page();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                StatusMessage = "Xác th?c email thành công. B?n có th? ??ng nh?p ngay bây gi?.";
                _logger.LogInformation("User {UserId} confirmed their email.", userId);
            }
            else
            {
                StatusMessage = "Xác th?c email th?t b?i.";
                _logger.LogWarning("Email confirmation failed for user {UserId}: {Errors}", userId, string.Join(';', result.Errors.Select(e => e.Description)));
            }

            return Page();
        }
    }
}
