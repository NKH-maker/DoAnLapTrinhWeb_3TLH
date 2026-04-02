using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TINH_FINAL_2256.Models;
using TINH_FINAL_2256.Services;

namespace TINH_FINAL_2256.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForgotPasswordModel> _logger;
        private readonly IEmailTemplateService _emailTemplateService;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailService emailService, ILogger<ForgotPasswordModel> logger, IEmailTemplateService emailTemplateService)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
            _emailTemplateService = emailTemplateService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                StatusMessage = "N?u email t?n t?i, chúng tôi ?ã g?i liên k?t ??t l?i m?t kh?u.";
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Page("/Account/ResetPassword", pageHandler: null, values: new { area = "Identity", userId = user.Id, token }, protocol: Request.Scheme);

            var body = _emailTemplateService.GetPasswordResetTemplate(user.FullName ?? user.Email, callbackUrl);

            await _emailService.SendEmailAsync(user.Email, "Yêu c?u ??t l?i m?t kh?u", body, isHtml: true);

            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
            StatusMessage = "N?u email t?n t?i, chúng tôi ?ã g?i liên k?t ??t l?i m?t kh?u.";
            return Page();
        }
    }
}
