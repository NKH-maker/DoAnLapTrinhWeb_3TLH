# ?? Tài Li?u Cài ??t - Các Th? Vi?n ?ã Tích H?p

## ?? Danh Sách Th? Vi?n ?ã Cài

| Th? T? | Tên Th? Vi?n | Ch?c N?ng | Phiên B?n |
|--------|-------------|----------|----------|
| 1 | **Redis** | Caching Distributed | 8.0.11 |
| 2 | **JWT** | Xác th?c API | 8.0.11 |
| 3 | **OAuth Google** | ??ng nh?p Social | 8.0.11 |
| 4 | **RabbitMQ** | Message Queue | 6.8.1 |
| 5 | **Hangfire** | Background Jobs | 1.8.14 |
| 6 | **SignalR** | Real-time Communication | 1.1.0 |
| 7 | **Serilog** | Logging & Monitoring | 8.0.0 |
| 8 | **ClosedXML** | Excel Export | 0.102.2 |
| 9 | **QRCoder** | QR Code Generation | 1.4.3 |
| 10 | **TusDotNet** | Large File Upload | 2.11.1 |

---

## ?? C?u Hình C?n Thi?t

### 1?? **appsettings.json**
C?p nh?t các thông tin c?u hình:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;...",
    "RedisConnection": "localhost:6379"
  },
  "JwtSettings": {
    "Key": "your-32-char-secret-key-here",
    "ExpirationMinutes": 60
  },
  "Authentication": {
    "Google": {
      "ClientId": "xxx.apps.googleusercontent.com",
      "ClientSecret": "your-secret"
    }
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672
  }
}
```

### 2?? **Kh?i ??ng Redis (n?u dùng)**
```bash
# Docker
docker run -d -p 6379:6379 redis:latest

# ho?c cài tr?c ti?p t? https://redis.io
```

### 3?? **Kh?i ??ng RabbitMQ (n?u dùng)**
```bash
# Docker
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:management

# Dashboard: http://localhost:15672 (admin/admin)
```

---

## ?? S? D?ng Các Service

### ?? **JWT Authentication**
```csharp
public class AuthController : Controller
{
    [HttpPost("login")]
    public IActionResult Login(LoginModel model)
    {
        // Validation...
        var token = GenerateJwtToken(model.Username);
        return Ok(new { token });
    }
}
```

### ?? **Redis Caching**
```csharp
// Inject IDistributedCache
public async Task<Product> GetProductAsync(int id)
{
    var cached = await _cache.GetStringAsync($"product_{id}");
    if (cached != null) return JsonSerializer.Deserialize<Product>(cached);
    
    var product = await _db.Products.FindAsync(id);
    await _cache.SetStringAsync($"product_{id}", 
        JsonSerializer.Serialize(product),
        new DistributedCacheEntryOptions 
        { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
    
    return product;
}
```

### ?? **Email Service**
```csharp
// Inject IEmailService
await _emailService.SendEmailAsync(
    to: "customer@example.com",
    subject: "Order Confirmation",
    body: "<h1>Thank you for your order!</h1>",
    isHtml: true);
```

### ?? **RabbitMQ Message Queue**
```csharp
// Inject IMessageQueueService
_messageQueueService.PublishOrderNotification(orderId, "Order created");
```

### ? **Background Jobs (Hangfire)**
```csharp
// Inject IBackgroundJobService
var jobId = _backgroundJobService.ScheduleEmailJob(
    email: "user@example.com",
    subject: "Reminder",
    body: "Your reminder",
    delay: TimeSpan.FromMinutes(5));

// Dashboard: https://localhost:7xxx/hangfire
```

### ?? **Real-time SignalR**
```javascript
// Client side
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub/notifications")
    .build();

connection.on("ReceiveOrderNotification", (orderId, message) => {
    console.log(`Order ${orderId}: ${message}`);
});

connection.start();
```

### ?? **Excel Export**
```csharp
// Inject IExcelService
var excelBytes = _excelService.ExportToExcel(products, "Products");
return File(excelBytes, 
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "products.xlsx");
```

### ?? **QR Code**
```csharp
// Inject IQRCodeService
var qrBase64 = _qrCodeService.GenerateQRCodeBase64("https://example.com/order/123");
// Hi?n th?: <img src="data:image/png;base64,@qrBase64" />
```

### ?? **Logging (Serilog)**
```csharp
// Inject ILogger<T>
_logger.LogInformation("Order created: {OrderId}", order.Id);
_logger.LogError(ex, "Error creating order");

// Logs saved to: logs/app-2024-01-01.txt
```

---

## ?? Các Endpoint Ví D?

?? test nhanh, truy c?p các endpoint ví d?:

```
POST   /example/send-email           - G?i email
POST   /example/publish-message      - Publish RabbitMQ
POST   /example/schedule-job         - Schedule Hangfire job
GET    /example/qr-code              - T?o QR code
GET    /example/cache                - Test caching
GET    /example/export-excel         - Export Excel
GET    /example/logging              - Test logging
POST   /example/clear-cache          - Clear cache
```

> ?? Xóa file `Examples/ExampleUsageController.cs` sau khi s? d?ng xong.

---

## ?? Các Thông Tin B?o M?t

### ? **KHÔNG** commit nh?ng thông tin sau:
- Google Client Secret
- JWT Secret Key
- Email Password
- Database Connection String (n?u dùng password)
- RabbitMQ Credentials

### ? **Dùng User Secrets cho Development**
```bash
# Set secret
dotnet user-secrets set "JwtSettings:Key" "your-secret"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-secret"

# Xem secrets
dotnet user-secrets list
```

---

## ?? Dashboard & Monitoring

| Service | URL | Credentials |
|---------|-----|-------------|
| **Hangfire** | `https://localhost:7xxx/hangfire` | Public |
| **RabbitMQ** | `http://localhost:15672` | admin/admin |
| **Redis** (redis-cli) | `localhost:6379` | - |
| **Logs** | `logs/` folder | - |

---

## ?? Checklist Deployment

- [ ] C?p nh?t t?t c? secrets trong `appsettings.Production.json`
- [ ] Redis ???c kh?i ??ng và accessible
- [ ] RabbitMQ ???c kh?i ??ng (n?u dùng)
- [ ] Email credentials ???c verify
- [ ] Google OAuth credentials ???c verify
- [ ] Database migration ?ã ch?y
- [ ] Hangfire tables ???c t?o trong SQL Server

---

## ?? Troubleshooting

### Redis Connection Error
```
? Error: Could not connect to redis server
? Solution: Ch?y `docker run -d -p 6379:6379 redis:latest`
```

### RabbitMQ Connection Error
```
? Error: AMQP connection error
? Solution: Ch?y `docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:management`
```

### JWT Token Validation Error
```
? Error: The token was invalid
? Solution: Ki?m tra JWT Key trong appsettings.json
```

### Email Send Failed
```
? Error: SMTP authentication failed
? Solution: Dùng Gmail App Password, không ph?i Gmail password
```

---

## ?? Tài Li?u Thêm

- [Redis Docs](https://redis.io/docs/)
- [RabbitMQ Docs](https://www.rabbitmq.com/documentation.html)
- [Hangfire Docs](https://docs.hangfire.io/)
- [SignalR Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [Serilog Docs](https://serilog.net/)
- [ClosedXML Docs](https://github.com/ClosedXML/ClosedXML)
- [QRCoder Docs](https://github.com/codebude/QRCoder)

---

**Phiên b?n:** 1.0  
**C?p nh?t l?n cu?i:** 2024  
**H? tr?:** Xem LIBRARIES_GUIDE.md ?? chi ti?t t?ng th? vi?n
