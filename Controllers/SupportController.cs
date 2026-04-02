using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TINH_FINAL_2256.Models;

namespace TINH_FINAL_2256.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SupportController> _logger;

        public SupportController(UserManager<ApplicationUser> userManager, ILogger<SupportController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            _logger.LogInformation("User {UserId} opened support chat", user.Id);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetChatHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Tr? v? chat history t? database/cache (hi?n t?i là mock)
            var messages = new List<object>();
            return Json(messages);
        }
    }
}
