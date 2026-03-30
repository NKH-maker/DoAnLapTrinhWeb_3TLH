using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TINH_FINAL_2256.Extensions;
using TINH_FINAL_2256.Models;
using TINH_FINAL_2256.Repositories;

namespace TINH_FINAL_2256.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProductRepository productRepository)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            var product = await GetProductFromDatabase(productId);

            var cartItem = new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl ?? "~/images/placeholder.png",
                CategoryName = product.Category?.Name ?? string.Empty
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart")
                       ?? new ShoppingCart();

            cart.AddItem(cartItem);

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            // If current user is admin, show all placed orders instead of session cart
            if (User.IsInRole(SD.Role_Admin))
            {
                var orders = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return View("AdminOrders", orders);
            }

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart")
                       ?? new ShoppingCart();

            return View(cart);
        }

        private async Task<Product> GetProductFromDatabase(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }

        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");

            if (cart != null)
            {
                cart.RemoveItem(productId);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }

            return RedirectToAction("Index");
        }

        public IActionResult IncreaseQuantity(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                cart.IncreaseQuantity(productId);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }

            return RedirectToAction("Index");
        }

        public IActionResult DecreaseQuantity(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                cart.DecreaseQuantity(productId);
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }

            return RedirectToAction("Index");
        }

        // GET: Checkout - prefill info for logged-in users
        public async Task<IActionResult> Checkout()
        {
            var order = new Order();
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                order.PhoneNumber = user.PhoneNumber;
                order.ShippingAddress = user.Address ?? string.Empty;
            }
            return View(order);
        }

        // POST: Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");

            if (cart == null || !cart.Items.Any())
            {
                return RedirectToAction("Index");
            }

            // Server-side validation
            if (!ModelState.IsValid)
            {
                return View(order);
            }

            var user = await _userManager.GetUserAsync(User);

            order.UserId = user?.Id ?? string.Empty;
            order.OrderDate = DateTime.UtcNow;
            order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);

            // Ensure non-null defaults
            order.PhoneNumber = order.PhoneNumber ?? user?.PhoneNumber ?? string.Empty;
            order.ShippingAddress = order.ShippingAddress ?? string.Empty;
            order.Notes = order.Notes ?? string.Empty;
            order.Status = order.Status ?? "Pending";

            order.OrderDetails = cart.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Payment), new { id = order.Id });
        }

        public async Task<IActionResult> Payment(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfirmPayment(int id)
        {
            HttpContext.Session.Remove("Cart");
            return RedirectToAction(nameof(OrderCompleted), new { id });
        }

        public async Task<IActionResult> OrderCompleted(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // New: View orders for current customer
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // New: Get recent orders for cart modal (AJAX endpoint)
        public async Task<IActionResult> GetRecentOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .Take(3)
                .ToListAsync();

            return PartialView("_RecentOrdersPartial", orders);
        }

        // New: Cancel order (only if customer and not shipped/delivered)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
            if (order == null) return NotFound();

            if (order.Status == "Shipped" || order.Status == "Delivered")
            {
                return BadRequest("Không th? h?y ??n ?ã v?n chuy?n ho?c giao hàng");
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyOrders));
        }

        // New: Update shipping address and phone for an order (customer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int id, string shippingAddress, string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
            if (order == null) return NotFound();

            if (order.Status == "Shipped" || order.Status == "Delivered")
            {
                return BadRequest("Không th? s?a ??n ?ã v?n chuy?n ho?c giao hàng");
            }

            order.ShippingAddress = shippingAddress ?? order.ShippingAddress;
            order.PhoneNumber = phoneNumber ?? order.PhoneNumber;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(OrderDetails), new { id = id });
        }

        // API to return cart count and items for header modal
        public IActionResult CartSummary()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            return PartialView("_CartSummaryPartial", cart);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminOrdersPartial()
        {
            var orders = await _context.Orders
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            return PartialView("_AdminOrdersPartial", orders);
        }
    }
}
