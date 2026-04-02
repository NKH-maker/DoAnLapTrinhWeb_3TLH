using TINH_FINAL_2256.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TINH_FINAL_2256.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<DataSeeder> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task SeedSampleOrdersAsync()
        {
            try
            {
                // Check if orders already exist
                if (await _context.Orders.AnyAsync())
                {
                    _logger.LogInformation("Orders already exist in database");
                    return;
                }

                // Get or create a test user
                var testUser = await _userManager.FindByNameAsync("testuser");
                if (testUser == null)
                {
                    testUser = new ApplicationUser
                    {
                        UserName = "testuser",
                        Email = "testuser@example.com",
                        FullName = "Ng??i Dùng Th? Nghi?m",
                        PhoneNumber = "0912345678",
                        Address = "123 ???ng Lê L?i, Qu?n 1, TP.HCM"
                    };

                    var result = await _userManager.CreateAsync(testUser, "Test@123456");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(testUser, "Customer");
                        _logger.LogInformation("Test user created successfully");
                    }
                    else
                    {
                        _logger.LogError("Failed to create test user");
                        return;
                    }
                }

                // Get products
                var products = await _context.Products.Take(3).ToListAsync();
                if (!products.Any())
                {
                    _logger.LogWarning("No products found in database. Cannot seed orders without products.");
                    return;
                }

                // Create sample orders
                var orders = new List<Order>
                {
                    new Order
                    {
                        UserId = testUser.Id,
                        OrderDate = DateTime.UtcNow.AddDays(-10),
                        Status = "Delivered",
                        ShippingAddress = testUser.Address ?? "123 ???ng Lê L?i, Qu?n 1, TP.HCM",
                        PhoneNumber = testUser.PhoneNumber ?? "0912345678",
                        Notes = "Giao bu?i sáng",
                        TotalPrice = 20000000, // 20M VND
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = products[0].Id,
                                Quantity = 1,
                                Price = 20000000
                            }
                        }
                    },
                    new Order
                    {
                        UserId = testUser.Id,
                        OrderDate = DateTime.UtcNow.AddDays(-7),
                        Status = "Processing",
                        ShippingAddress = testUser.Address ?? "123 ???ng Lê L?i, Qu?n 1, TP.HCM",
                        PhoneNumber = testUser.PhoneNumber ?? "0912345678",
                        Notes = "Giao lúc chi?u t?i",
                        TotalPrice = 18000000, // 18M VND
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = products[1].Id,
                                Quantity = 1,
                                Price = 18000000
                            }
                        }
                    },
                    new Order
                    {
                        UserId = testUser.Id,
                        OrderDate = DateTime.UtcNow.AddDays(-4),
                        Status = "Pending",
                        ShippingAddress = testUser.Address ?? "123 ???ng Lê L?i, Qu?n 1, TP.HCM",
                        PhoneNumber = testUser.PhoneNumber ?? "0912345678",
                        Notes = "",
                        TotalPrice = 12000000, // 12M VND
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = products[2].Id,
                                Quantity = 1,
                                Price = 12000000
                            }
                        }
                    },
                    new Order
                    {
                        UserId = testUser.Id,
                        OrderDate = DateTime.UtcNow.AddDays(-2),
                        Status = "Cancelled",
                        ShippingAddress = testUser.Address ?? "123 ???ng Lê L?i, Qu?n 1, TP.HCM",
                        PhoneNumber = testUser.PhoneNumber ?? "0912345678",
                        Notes = "Khách hàng h?y",
                        TotalPrice = 25000000, // 25M VND
                        OrderDetails = new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                ProductId = products[0].Id,
                                Quantity = 1,
                                Price = 25000000
                            }
                        }
                    }
                };

                _context.Orders.AddRange(orders);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully seeded {orders.Count} sample orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding sample orders");
            }
        }
    }
}
