using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TINH_FINAL_2256.Models;
using TINH_FINAL_2256.Services;

namespace TINH_FINAL_2256.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _emailTemplateService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger,
            IEmailService emailService,
            IEmailTemplateService emailTemplateService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public string? ReturnUrl { get; set; }

        public List<SelectListItem>? RoleList { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Họ tên là bắt buộc")]
            [Display(Name = "Họ tên")]
            [StringLength(100, ErrorMessage = "Họ tên phải từ {2} đến {1} ký tự.", MinimumLength = 3)]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Display(Name = "Địa chỉ")]
            [StringLength(200)]
            public string? Address { get; set; }

            [Display(Name = "Tuổi")]
            [RegularExpression(@"^\d+$", ErrorMessage = "Tuổi phải là số")]
            public string? Age { get; set; }

            [Display(Name = "Vai trò")]
            public string? Role { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            await PopulateRoleListAsync();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            await PopulateRoleListAsync();

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra xem email đã tồn tại
                    var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("Cố gắng đăng ký với email đã tồn tại: {Email}", Input.Email);
                        ModelState.AddModelError("Input.Email", "Email này đã được đăng ký.");
                        return Page();
                    }

                    var user = new ApplicationUser
                    {
                        UserName = Input.Email,
                        Email = Input.Email,
                        FullName = Input.FullName,
                        Address = Input.Address,
                        Age = Input.Age
                    };

                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Người dùng {Email} đăng ký thành công", Input.Email);

                        // Gán vai trò mặc định
                        var roleToAssign = !string.IsNullOrEmpty(Input.Role) 
                            ? Input.Role 
                            : SD.Role_Customer;

                        await _userManager.AddToRoleAsync(user, roleToAssign);
                        _logger.LogInformation("Gán vai trò {Role} cho user {Email}", roleToAssign, Input.Email);

                        // Gửi email xác thực
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null, values: new { area = "Identity", userId = user.Id, code }, protocol: Request.Scheme);

                        var body = _emailTemplateService.GetEmailConfirmationTemplate(user.FullName, callbackUrl);

                        await _emailService.SendEmailAsync(user.Email, "Xác nhận email - 3TLH Phone", body, isHtml: true);

                        // Nếu muốn tự động đăng nhập, có thể gọi SignIn; hiện tại không auto-signin để yêu cầu xác thực
                        if (!(_userManager.Options.SignIn.RequireConfirmedAccount))
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            return LocalRedirect(returnUrl);
                        }

                        // Hiển thị thông báo kiểm tra email
                        TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.";
                        return RedirectToPage("./Register");
                    }

                    // Log chi tiết lỗi
                    foreach (var error in result.Errors)
                    {
                        _logger.LogWarning("Lỗi đăng ký cho {Email}: {Error}", Input.Email, error.Description);
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi đăng ký user {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Có lỗi xảy ra. Vui lòng thử lại sau.");
                }
            }

            return Page();
        }

        private async Task PopulateRoleListAsync()
        {
            try
            {
                var roles = await _roleManager.Roles.ToListAsync();
                RoleList = roles.Select(r => new SelectListItem
                {
                    Value = r.Name ?? "",
                    Text = r.Name ?? ""
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải danh sách vai trò");
                RoleList = new List<SelectListItem>();
            }
        }
    }
}
