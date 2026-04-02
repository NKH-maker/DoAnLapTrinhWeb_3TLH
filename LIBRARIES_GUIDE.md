# H??ng D?n S? D?ng Các Th? Vi?n ?ã Cài

## Danh Sách Th? Vi?n ?ã Cài ??t

### 1. **Redis Caching** - Microsoft.Extensions.Caching.StackExchangeRedis
Dùng ?? cache d? li?u và gi?m t?i c? s? d? li?u.

**C?u hình appsettings.json:**
```json
"ConnectionStrings": {
  "RedisConnection": "localhost:6379"
}
```

**Cách s? d?ng:**
```csharp
// Inject vào controller
public class ProductController : Controller
{
    private readonly IDistributedCache _cache;
    
    public ProductController(IDistributedCache cache)
    {
        _cache = cache;
    }
    
    public async Task<IActionResult> GetProduct(int id)
    {
        // Ki?m tra cache
        var cached = await _cache.GetStringAsync($"product_{id}");
        if (!string.IsNullOrEmpty(cached))
        {
            return Ok(JsonSerializer.Deserialize(cached));
        }
        
        // L?y t? DB
        var product = await _productService.GetByIdAsync(id);
        
        // L?u vào cache (5 phút)
        await _cache.SetStringAsync($"product_{id}", 
            JsonSerializer.Serialize(product),
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) 
            });
        
        return Ok(product);
    }
}
```

---

### 2. **JWT Authentication** - Microsoft.AspNetCore.Authentication.JwtBearer
Xác th?c API b?ng JWT token.

**C?u hình appsettings.json:**
```json
"JwtSettings": {
  "Key": "your-super-secret-key-min-32-chars",
  "Issuer": "YourApp",
  "Audience": "YourAppUsers",
  "ExpirationMinutes": 60
}
```

**T?o JWT Token:**
```csharp
public class AuthController : Controller
{
    private readonly IConfiguration _config;
    
    [HttpPost("login")]
    public IActionResult Login(LoginModel model)
    {
        // Ki?m tra user
        if (ValidateUser(model))
        {
            var token = GenerateJwtToken(model.Username);
            return Ok(new { token });
        }
        return Unauthorized();
    }
    
    private string GenerateJwtToken(string username)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            expires: DateTime.Now.AddMinutes(
                int.Parse(_config["JwtSettings:ExpirationMinutes"])),
            signingCredentials: creds);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

### 3. **OAuth 2.0 Google** - Microsoft.AspNetCore.Authentication.Google
Cho phép ??ng nh?p b?ng Google.

**C?u hình appsettings.json:**
```json
"Authentication": {
  "Google": {
    "ClientId": "xxx.apps.googleusercontent.com",
    "ClientSecret": "your-secret"
  }
}
```

**S? d?ng trong Razor Pages:**
```html
<a asp-page="/Account/ExternalLogin" 
   asp-route-provider="Google" 
   class="btn btn-primary">
   ??ng nh?p v?i Google
</a>
```

---

### 4. **RabbitMQ Message Queue** - RabbitMQ.Client
X? lý các tác v? b?t ??ng b? nh? g?i email, thông báo.

**C?u hình appsettings.json:**
```json
"RabbitMQ": {
  "HostName": "localhost",
  "Port": 5672,
  "UserName": "guest",
  "Password": "guest"
}
```

**Cách s? d?ng:**
```csharp
// Inject vào service
public class OrderService
{
    private readonly IMessageQueueService _messageQueue;
    
    public OrderService(IMessageQueueService messageQueue)
    {
        _messageQueue = messageQueue;
    }
    
    public async Task CreateOrderAsync(Order order)
    {
        // L?u ??n hàng
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        
        // G?i notification qua RabbitMQ
        _messageQueue.PublishOrderNotification(
            order.Id, 
            $"Order {order.Id} created successfully");
    }
}
```

---

### 5. **Hangfire Background Jobs** - Hangfire.AspNetCore
Lên l?ch các tác v? ch?y ng?m.

**Dashboard:** Truy c?p `/hangfire` ?? xem th?ng kê jobs.

**Cách s? d?ng:**
```csharp
// Inject BackgroundJobService
public class EmailController : Controller
{
    private readonly IBackgroundJobService _backgroundJobService;
    
    public EmailController(IBackgroundJobService backgroundJobService)
    {
        _backgroundJobService = backgroundJobService;
    }
    
    [HttpPost("send-email")]
    public IActionResult SendEmailLater(string email, string subject, string body)
    {
        // G?i email sau 1 phút
        var jobId = _backgroundJobService.ScheduleEmailJob(
            email, subject, body, TimeSpan.FromMinutes(1));
        
        return Ok(new { jobId });
    }
}
```

---

### 6. **SignalR Real-time** - Microsoft.AspNetCore.SignalR
T??ng tác real-time v?i client.

**Server (C#):**
```csharp
// ?ã t?o NotificationHub
public class NotificationHub : Hub
{
    public async Task SendOrderNotification(string orderId, string message)
    {
        await Clients.All.SendAsync("ReceiveOrderNotification", orderId, message);
    }
}
```

**Client (JavaScript):**
```html
<script src="~/lib/signalr/signalr.js"></script>
<script>
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub/notifications")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveOrderNotification", (orderId, message) => {
    console.log(`Order ${orderId}: ${message}`);
    // C?p nh?t UI
});

connection.start().catch(err => console.error(err));
</script>
```

---

### 7. **Serilog Logging** - Serilog.AspNetCore
Ghi log chi ti?t vào file và console.

**Log s? ???c l?u vào:** `logs/app-2024-01-01.txt`

**Cách s? d?ng:**
```csharp
public class ProductService
{
    private readonly ILogger<ProductService> _logger;
    
    public ProductService(ILogger<ProductService> logger)
    {
        _logger = logger;
    }
    
    public async Task<Product> GetProductAsync(int id)
    {
        try
        {
            _logger.LogInformation("Getting product {ProductId}", id);
            var product = await _context.Products.FindAsync(id);
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", id);
            throw;
        }
    }
}
```

---

### 8. **Email Notification** - IEmailService
G?i email thông báo.

**C?u hình appsettings.json:**
```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SenderEmail": "your-email@gmail.com",
  "SenderPassword": "your-app-password"
}
```

**Cách s? d?ng:**
```csharp
public class OrderService
{
    private readonly IEmailService _emailService;
    
    public OrderService(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task SendOrderConfirmationAsync(string email, Order order)
    {
        var subject = "Order Confirmation";
        var body = $"Your order {order.Id} has been created.";
        await _emailService.SendEmailAsync(email, subject, body);
    }
}
```

---

### 9. **QR Code Generation** - QRCoder
T?o mã QR.

**Cách s? d?ng:**
```csharp
public class QRController : Controller
{
    private readonly IQRCodeService _qrService;
    
    public QRController(IQRCodeService qrService)
    {
        _qrService = qrService;
    }
    
    [HttpGet("generate-qr")]
    public IActionResult GenerateQR(string data)
    {
        var qrCodeBase64 = _qrService.GenerateQRCodeBase64(data);
        return Ok(new { qrCode = $"data:image/png;base64,{qrCodeBase64}" });
    }
}
```

---

### 10. **Excel Export** - ClosedXML
Xu?t d? li?u ra Excel.

**Cách s? d?ng:**
```csharp
public class ReportController : Controller
{
    private readonly IExcelService _excelService;
    
    public ReportController(IExcelService excelService)
    {
        _excelService = excelService;
    }
    
    [HttpGet("export-products")]
    public async Task<IActionResult> ExportProducts()
    {
        var products = await _context.Products.ToListAsync();
        var excelBytes = _excelService.ExportToExcel(products, "Products");
        
        return File(excelBytes, 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "products.xlsx");
    }
}
```

---

### 11. **Large File Upload** - TusDotNet
T?i lên các t?p l?n.

**C?u hình Program.cs:**
```csharp
// app.UseTus(options => { ... });
```

---

## Các Cài ??t B? Sung C?n Thi?t

### Ch?y Redis (tùy ch?n)
```bash
docker run -d -p 6379:6379 redis:latest
```

### Ch?y RabbitMQ (tùy ch?n)
```bash
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:management
```

### Hangfire Dashboard
- Truy c?p: `https://localhost:7xxx/hangfire`
- Xem t?t c? background jobs

---

## Quy ??nh S? D?ng

? **Nên làm:**
- Cache các d? li?u thay ??i ít
- G?i email b?t ??ng b? qua RabbitMQ ho?c Hangfire
- Log t?t c? exception và các s? ki?n quan tr?ng
- S? d?ng JWT cho API

? **Không nên:**
- Cache d? li?u nh?y c?m (password, token)
- G?i email ??ng b? trong request
- B? qua logging

---

**C?n h? tr?? Tham kh?o tài li?u chính th?c c?a t?ng th? vi?n.**
