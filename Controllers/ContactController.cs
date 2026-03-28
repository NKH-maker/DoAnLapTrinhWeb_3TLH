using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Threading.Tasks;
using TINH_FINAL_2256.Models;

namespace TINH_FINAL_2256.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Send(ContactFormModel model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            // Send email logic here - left simple: use MailMessage to send via SMTP if configured
            // For security, SMTP settings should be in configuration and not hard-coded.
            try
            {
                var msg = new MailMessage();
                msg.To.Add("3TLHPhone@gmail.com");
                msg.Subject = model.Subject ?? "Khách hàng liên h?";
                msg.Body = $"From: {model.Name} ({model.Email})\n\n{model.Message}";

                // Example using SmtpClient - requires config
                using (var smtp = new SmtpClient())
                {
                    await smtp.SendMailAsync(msg);
                }

                ViewBag.Message = "G?i thành công";
            }
            catch
            {
                ViewBag.Message = "G?i th?t b?i. Ki?m tra c?u hình SMTP";
            }

            return View("Index");
        }
    }
}
