using Microsoft.AspNetCore.Mvc;

namespace TINH_FINAL_2256.Controllers
{
    // Simple redirect for /Admin to the Admin area's Product index
    [Route("Admin")]
    public class AdminController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Product", new { area = "Admin" });
        }
    }
}
