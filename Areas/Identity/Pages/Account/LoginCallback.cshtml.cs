using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TINH_FINAL_2256.Models;

namespace TINH_FINAL_2256.Areas.Identity.Pages.Account
{
    public class LoginCallbackModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginCallbackModel> _logger;

        public LoginCallbackModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginCallbackModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        public string ReturnUrl { get; set; } = "/";

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogWarning("External login info is null.");
                return RedirectToPage("./Login");
            }

            // Try to sign in the user with this external login provider
            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (signInResult.Succeeded)
            {
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return LocalRedirect(ReturnUrl);
            }

            // If the user does not have an account, create one
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                _logger.LogWarning("External login did not provide email.");
                return RedirectToPage("./Login");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogWarning("Failed to create user from external provider: {Errors}", string.Join(';', createResult.Errors.Select(e => e.Description)));
                    return RedirectToPage("./Login");
                }

                // Mark email as confirmed because it came from external provider
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            // Add the external login info to the user and sign in
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                _logger.LogWarning("Failed to add external login: {Errors}", string.Join(';', addLoginResult.Errors.Select(e => e.Description)));
                return RedirectToPage("./Login");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

            return LocalRedirect(ReturnUrl);
        }
    }
}
