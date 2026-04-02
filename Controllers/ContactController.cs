using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TINH_FINAL_2256.Models;
using TINH_FINAL_2256.Services;

namespace TINH_FINAL_2256.Controllers
{
    public class ContactController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailTemplateService _emailTemplateService;

        public ContactController(
            IEmailService emailService,
            ILogger<ContactController> logger,
            UserManager<ApplicationUser> userManager,
            IEmailTemplateService emailTemplateService)
        {
            _emailService = emailService;
            _logger = logger;
            _userManager = userManager;
            _emailTemplateService = emailTemplateService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(ContactFormModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Contact form validation failed");
                    return View("Index", model);
                }

                // ?? G?i email qu?n lý
                var adminSubject = $"[Contact] {model.Subject ?? "Khách hàng liên h?"}";
                var adminBody = $@"
                    <h2>Khách hàng m?i liên h?</h2>
                    <p><strong>Tên:</strong> {model.Name}</p>
                    <p><strong>Email:</strong> {model.Email}</p>
                    <p><strong>Ch? ??:</strong> {model.Subject}</p>
                    <p><strong>N?i dung:</strong></p>
                    <p>{model.Message?.Replace("\n", "<br>")}</p>
                    <hr>
                    <p>Th?i gian: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
                ";

                await _emailService.SendEmailAsync(
                    to: "3TLHPhone@gmail.com",
                    subject: adminSubject,
                    body: adminBody,
                    isHtml: true);

                // ?? G?i email xác nh?n cho khách hàng
                var customerBody = _emailTemplateService.GetContactReplyTemplate(model.Name, model.Subject, model.Message);

                await _emailService.SendEmailAsync(
                    to: model.Email,
                    subject: "Chúng tôi ?ã nh?n ???c thông báo c?a b?n",
                    body: customerBody,
                    isHtml: true);

                _logger.LogInformation("Contact form sent successfully from {Email}", model.Email);
                
                TempData["SuccessMessage"] = "? G?i thành công! Chúng tôi s? liên h? v?i b?n s?m.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending contact form from {Email}", model.Email);
                TempData["ErrorMessage"] = "? G?i th?t b?i. Vui lòng th? l?i sau.";
                return View("Index", model);
            }
        }
    }
}
