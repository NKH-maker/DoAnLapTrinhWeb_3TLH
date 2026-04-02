using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TINH_FINAL_2256.Models;
using TINH_FINAL_2256.Services;

namespace TINH_FINAL_2256.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailTemplateService _emailTemplateService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailService emailService, ILogger<AccountController> logger, IEmailTemplateService emailTemplateService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _logger = logger;
            _emailTemplateService = emailTemplateService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var logins = await _userManager.GetLoginsAsync(user);
            var model = new ManageViewModel
            {
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                ExternalLogins = logins.Select(x => new ExternalLoginViewModel { LoginProvider = x.LoginProvider, ProviderDisplayName = x.ProviderDisplayName }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Update profile fields
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            // If email changed, send confirmation link
            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null, values: new { area = "Identity", userId = user.Id, code = token }, protocol: Request.Scheme);
                var body = _emailTemplateService.GetEmailConfirmationTemplate(user.FullName ?? user.Email, callbackUrl);

                await _emailService.SendEmailAsync(model.Email, "Xác nh?n email", body, isHtml: true);
                // Tentatively set new email and mark unconfirmed
                user.Email = model.Email;
                user.UserName = model.Email;
                user.EmailConfirmed = false;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} updated their profile.", user.Id);
                TempData["SuccessMessage"] = "C?p nh?t thông tin thành công.";
                return RedirectToAction("Manage");
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveExternalLogin(string loginProvider)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Remove the external login by provider name; provider key will be resolved by stored logins
            var logins = await _userManager.GetLoginsAsync(user);
            var login = logins.FirstOrDefault(l => l.LoginProvider == loginProvider);
            if (login == null)
            {
                TempData["ErrorMessage"] = "Liên k?t không t?n t?i.";
                return RedirectToAction("Manage");
            }

            var result = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} removed external login {Provider}", user.Id, loginProvider);
                TempData["SuccessMessage"] = $"?ã g? liên k?t {loginProvider}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không th? g? liên k?t. Vui lòng th? l?i.";
            }

            return RedirectToAction("Manage");
        }

        // Change Password
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "??i m?t kh?u thành công.";
                return RedirectToAction("Manage");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Delete Account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // If user has a password, require it
            var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
            if (hasPassword)
            {
                if (string.IsNullOrEmpty(model.Password))
                {
                    TempData["ErrorMessage"] = "Vui lòng nh?p m?t kh?u ?? xác nh?n xóa tài kho?n.";
                    return RedirectToAction("Manage");
                }

                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!passwordValid)
                {
                    TempData["ErrorMessage"] = "M?t kh?u không ?úng.";
                    return RedirectToAction("Manage");
                }
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User {UserId} deleted their account.", user.Id);
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "Không th? xóa tài kho?n. Vui lòng th? l?i sau.";
            return RedirectToAction("Manage");
        }
    }

    public class ManageViewModel
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public List<ExternalLoginViewModel>? ExternalLogins { get; set; }
    }

    public class ExternalLoginViewModel
    {
        public string? LoginProvider { get; set; }
        public string? ProviderDisplayName { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "M?t kh?u hi?n t?i")]
        public string OldPassword { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 6)]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "M?t kh?u m?i")]
        public string NewPassword { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Xác nh?n m?t kh?u m?i")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "M?t kh?u và xác nh?n không kh?p.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class DeleteAccountViewModel
    {
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        public string? Password { get; set; }
    }
}
