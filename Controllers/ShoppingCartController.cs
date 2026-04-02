using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TINH_FINAL_2256.Extensions;
using TINH_FINAL_2256.Models;
using TINH_FINAL_2256.Repositories;
using TINH_FINAL_2256.Services;
using TINH_FINAL_2256.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Security.Claims;

namespace TINH_FINAL_2256.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IMessageQueueService _messageQueueService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IQRCodeService _qrCodeService;
        private readonly IExcelService _excelService;
        private readonly IDistributedCache _cache;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<ShoppingCartController> _logger;
        private readonly IEmailTemplateService _emailTemplateService;

        public ShoppingCartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IProductRepository productRepository,
            IEmailService emailService,
            IMessageQueueService messageQueueService,
            IBackgroundJobService backgroundJobService,
            IQRCodeService qrCodeService,
            IExcelService excelService,
            IDistributedCache cache,
            IHubContext<NotificationHub> hubContext,
            ILogger<ShoppingCartController> logger,
            IEmailTemplateService emailTemplateService)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _messageQueueService = messageQueueService;
            _backgroundJobService = backgroundJobService;
            _qrCodeService = qrCodeService;
            _excelService = excelService;
            _cache = cache;
            _hubContext = hubContext;
            _logger = logger;
            _emailTemplateService = emailTemplateService;
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            try
            {
                _logger.LogInformation("Adding product {ProductId} to cart with quantity {Quantity}", productId, quantity);

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

                // Clear cache khi cart thay ??i
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _cache.RemoveAsync($"user_cart_{user.Id}");
                }

                _logger.LogInformation("Product added to cart successfully");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // If current user is admin, show all placed orders instead of session cart
                if (User.IsInRole(SD.Role_Admin))
                {
                    _logger.LogInformation("Admin user accessing orders list");

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShoppingCart Index");
                return View(new ShoppingCart());
            }
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
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var order = new Order
            {
                PhoneNumber = user.PhoneNumber,
                ShippingAddress = user.Address ?? string.Empty
            };

            return View(order);
        }

        // POST: Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Checkout(Order order)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");

                if (cart == null || !cart.Items.Any())
                {
                    TempData["ErrorMessage"] = "Gi? hàng r?ng.";
                    return RedirectToAction("Index");
                }

                // Server-side validation
                if (!ModelState.IsValid)
                {
                    return View(order);
                }

                order.UserId = user.Id;
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

                _logger.LogInformation("Order created: {OrderId} by user {UserId}", order.Id, user?.Id);

                // G?i order confirmation email b?t ??ng b? qua Hangfire
                var subject = "Xác Nh?n ??n Hàng";
                var body = _emailTemplateService.GetOrderConfirmationTemplate(
                    user?.FullName ?? "Khách hàng",
                    order.Id,
                    order.TotalPrice,
                    order.OrderDate);

                _backgroundJobService.ScheduleEmailJob(
                    user?.Email ?? order.PhoneNumber,
                    subject,
                    body,
                    TimeSpan.FromSeconds(5));

                // Publish notifications (no-op if MQ disabled)
                _messageQueueService.PublishOrderNotification(
                    order.Id.ToString(),
                    $"Order {order.Id} created successfully - Total: {order.TotalPrice:C}");

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveOrderNotification",
                    order.Id.ToString(),
                    $"New Order #{order.Id} - {order.TotalPrice:C}");

                _logger.LogInformation("Order notifications sent for order {OrderId}", order.Id);

                // Clear cache và session
                await _cache.RemoveAsync($"user_orders_{user.Id}");
                HttpContext.Session.Remove("Cart");

                return RedirectToAction(nameof(Payment), new { id = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                ModelState.AddModelError("", "Có l?i x?y ra khi x? lý ??n hàng. Vui lòng th? l?i.");
                return View(order);
            }
        }

        public async Task<IActionResult> Payment(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment page for order {OrderId}", id);
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order != null)
                {
                    order.Status = "Paid";
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Payment confirmed for order {OrderId}", id);
                }

                HttpContext.Session.Remove("Cart");
                return RedirectToAction(nameof(OrderCompleted), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for order {OrderId}", id);
                return RedirectToAction(nameof(OrderCompleted), new { id });
            }
        }

        public async Task<IActionResult> OrderCompleted(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                // ?? T?o QR code cho order tracking
                var qrCodeData = $"{Request.Scheme}://{Request.Host}/ShoppingCart/OrderDetails/{order.Id}";
                var qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(qrCodeData);
                ViewData["OrderQRCode"] = qrCodeBase64;

                _logger.LogInformation("Order completed page loaded for order {OrderId}", id);

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order completed page for order {OrderId}", id);
                return NotFound();
            }
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (user == null && User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                    if (!string.IsNullOrEmpty(userId)) user = await _userManager.FindByIdAsync(userId);
                }

                // Only allow owner or admin to view
                if (!User.IsInRole(SD.Role_Admin))
                {
                    if (user == null || order.UserId != user.Id)
                    {
                        return Forbid();
                    }
                }

                // Create QR code for order
                var qrCodeData = $"{Request.Scheme}://{Request.Host}/ShoppingCart/OrderDetails/{order.Id}";
                var qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(qrCodeData);
                ViewData["OrderQRCode"] = qrCodeBase64;

                _logger.LogInformation("Order details loaded for order {OrderId}", id);

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for order {OrderId}", id);
                return NotFound();
            }
        }

        // View orders for current customer with caching
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null && User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                    if (!string.IsNullOrEmpty(userId)) user = await _userManager.FindByIdAsync(userId);
                }

                if (user == null) return Challenge();

                // Ki?m tra cache
                var cachedOrders = await _cache.GetStringAsync($"user_orders_{user.Id}");
                if (!string.IsNullOrEmpty(cachedOrders))
                {
                    _logger.LogInformation("User orders loaded from cache for user {UserId}", user.Id);
                    // Note: still fetch fresh data below to ensure details populated
                }

                var orders = await _context.Orders
                    .Where(o => o.UserId == user.Id)
                    .Include(o => o.ApplicationUser)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .AsNoTracking()
                    .ToListAsync();

                // Cache orders cho 10 phút (serialize minimal DTO if necessary)
                await _cache.SetStringAsync($"user_orders_{user.Id}", 
                    JsonSerializer.Serialize(orders),
                    new DistributedCacheEntryOptions 
                    { 
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) 
                    });

                _logger.LogInformation("User orders loaded for user {UserId}", user.Id);

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user orders");
                return View(new List<Order>());
            }
        }

        // Get recent orders for cart modal (AJAX endpoint)
        [Authorize]
        public async Task<IActionResult> GetRecentOrders()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null && User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                    if (!string.IsNullOrEmpty(userId)) user = await _userManager.FindByIdAsync(userId);
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recent orders");
                return PartialView("_RecentOrdersPartial", new List<Order>());
            }
        }

        // Cancel order (only if customer and not shipped/delivered)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
                
                if (order == null) return NotFound();

                if (order.Status == "Shipped" || order.Status == "Delivered")
                {
                    _logger.LogWarning("Attempt to cancel shipped/delivered order {OrderId}", id);
                    return BadRequest("Không th? h?y ??n ?ã v?n chuy?n ho?c giao hàng");
                }

                order.Status = "Cancelled";
                await _context.SaveChangesAsync();

                // ?? G?i notification
                await _hubContext.Clients.User(user.Id)
                    .SendAsync("OrderCancelled", order.Id);

                // ?? G?i email h?y ??n
                _backgroundJobService.ScheduleEmailJob(
                    user.Email ?? "",
                    "H?y ??n Hàng",
                    $"??n hàng #{order.Id} ?ã ???c h?y",
                    TimeSpan.FromSeconds(5));

                _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, user.Id);

                return RedirectToAction(nameof(MyOrders));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return BadRequest("Có l?i x?y ra khi h?y ??n hàng");
            }
        }

        // Update shipping address and phone for an order (customer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int id, string shippingAddress, string phoneNumber)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
                
                if (order == null) return NotFound();

                if (order.Status == "Shipped" || order.Status == "Delivered")
                {
                    _logger.LogWarning("Attempt to update shipped/delivered order {OrderId}", id);
                    return BadRequest("Không th? s?a ??n ?ã v?n chuy?n ho?c giao hàng");
                }

                order.ShippingAddress = shippingAddress ?? order.ShippingAddress;
                order.PhoneNumber = phoneNumber ?? order.PhoneNumber;
                await _context.SaveChangesAsync();

                // Clear cache
                await _cache.RemoveAsync($"user_orders_{user.Id}");

                _logger.LogInformation("Order {OrderId} updated by user {UserId}", id, user.Id);

                return RedirectToAction(nameof(OrderDetails), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId}", id);
                return BadRequest("Có l?i x?y ra khi c?p nh?t ??n hàng");
            }
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
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync();

                return PartialView("_AdminOrdersPartial", orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin orders");
                return PartialView("_AdminOrdersPartial", new List<Order>());
            }
        }

        // ?? Export orders to Excel
        [Authorize(Roles = "Admin")]
        [HttpGet("export-orders-excel")]
        public async Task<IActionResult> ExportOrdersToExcel()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.ApplicationUser)
                    .Include(o => o.OrderDetails)
                    .ToListAsync();

                // Transform to dynamic for Excel
                var exportData = orders.Select(o => new
                {
                    o.Id,
                    CustomerName = o.ApplicationUser?.UserName ?? "Unknown",
                    o.PhoneNumber,
                    o.ShippingAddress,
                    o.OrderDate,
                    o.TotalPrice,
                    o.Status,
                    ItemCount = o.OrderDetails?.Count ?? 0
                }).ToList();

                var excelBytes = _excelService.ExportToExcel(exportData, "Orders");

                _logger.LogInformation("Orders exported to Excel by admin");

                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Orders_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders to Excel");
                return BadRequest("Có l?i x?y ra khi xu?t Excel");
            }
        }
    }
}
