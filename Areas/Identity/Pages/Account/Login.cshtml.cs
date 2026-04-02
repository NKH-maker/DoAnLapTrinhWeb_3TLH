using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TINH_FINAL_2256.Models;

namespace TINH_FINAL_2256.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Ghi nhớ đăng nhập")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                ViewData["ReturnUrl"] = returnUrl;
            }

            // Lấy các phương thức OAuth có sẵn
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        Input.Email, 
                        Input.Password, 
                        Input.RememberMe, 
                        lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Đăng nhập thành công cho user {Email}", Input.Email);
                        return LocalRedirect(returnUrl);
                    }
                    else if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Tài khoản {Email} bị khóa", Input.Email);
                        ModelState.AddModelError(string.Empty, "Tài khoản của bạn bị khóa. Vui lòng thử lại sau.");
                    }
                    else if (result.RequiresTwoFactor)
                    {
                        _logger.LogInformation("Yêu cầu 2FA cho user {Email}", Input.Email);
                        return RedirectToPage("./LoginWith2fa", new { returnUrl, rememberMe = Input.RememberMe });
                    }
                    else
                    {
                        _logger.LogWarning("Đăng nhập không thành công cho user {Email}", Input.Email);
                        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đăng nhập user {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại sau.");
                }
            }

            return Page();
        }

        // Handler for external login (e.g., Google, Facebook)
        public IActionResult OnPostExternalLogin(string provider, string? returnUrl = null)
        {
            // Đăng nhập bằng OAuth (Google, Facebook, v.v.)
            var redirectUrl = Url.Page("./LoginCallback", pageHandler: null, values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            
            _logger.LogInformation("Bắt đầu đăng nhập OAuth với provider {Provider}", provider);
            
            return new ChallengeResult(provider, properties);
        }
    }
}
