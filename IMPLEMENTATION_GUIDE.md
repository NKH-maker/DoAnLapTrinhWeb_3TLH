# ?? H??ng D?n Tích H?p Các Th? Vi?n Vào D? Án

## ? Nh?ng Gì ?ã ???c Tích H?p

### 1. **ShoppingCartController** - Các Ch?c N?ng M?i

#### ?? Thông Báo Real-time (SignalR)
- Khi khách hàng t?o ??n hàng, t?t c? client ???c thông báo ngay l?p t?c
- Khi ??n hàng b? h?y, client t? ??ng reload page
- Real-time cart updates

**Code:**
```csharp
// G?i notification qua SignalR
await _hubContext.Clients.All.SendAsync(
    "ReceiveOrderNotification", 
    order.Id.ToString(), 
    $"New Order #{order.Id} - {order.TotalPrice:C}");
```

#### ?? Email Notifications (Hangfire)
- G?i email xác nh?n ??n hàng b?t ??ng b? (sau 5 giây)
- G?i email h?y ??n hàng
- Background Job dashboard: `/hangfire`

**Tính n?ng:**
```csharp
_backgroundJobService.ScheduleEmailJob(
    email, 
    "Xác Nh?n ??n Hàng",
    body, 
    TimeSpan.FromSeconds(5));
```

#### ?? Message Queue (RabbitMQ)
- Publish order notification vào queue
- Có th? tích h?p v?i h? th?ng khác

**Code:**
```csharp
_messageQueueService.PublishOrderNotification(
    order.Id.ToString(), 
    $"Order {order.Id} created");
```

#### ?? Caching (Redis)
- Cache ??n hàng c?a user (10 phút)
- Clear cache khi có thay ??i
- T?ng t?c ?? truy v?n

**Code:**
```csharp
await _cache.SetStringAsync($"user_orders_{user.Id}", 
    JsonSerializer.Serialize(orders),
    new DistributedCacheEntryOptions 
    { 
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) 
    });
```

#### ?? QR Code Tracking
- T?o mã QR cho m?i ??n hàng
- Dùng ?? tracking ??n hàng
- Hi?n th? trên trang `OrderCompleted` và `OrderDetails`

**Code:**
```csharp
var qrCodeData = $"{Request.Scheme}://{Request.Host}/ShoppingCart/OrderDetails/{order.Id}";
var qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(qrCodeData);
ViewData["OrderQRCode"] = qrCodeBase64;
```

#### ?? Excel Export
- Xu?t danh sách ??n hàng ra Excel
- Endpoint: `GET /export-orders-excel` (Admin only)
- File ???c t?i v? v?i timestamp

**Code:**
```csharp
[HttpGet("export-orders-excel")]
public async Task<IActionResult> ExportOrdersToExcel()
{
    // Export data to Excel
    return File(excelBytes, 
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"Orders_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
}
```

#### ?? Logging (Serilog)
- Log t?t c? các ho?t ??ng quan tr?ng
- Log l?i và exception
- File log: `logs/app-YYYY-MM-DD.txt`

**Các log ???c ghi:**
- Thêm s?n ph?m vào gi? hàng
- T?o ??n hàng
- Xác nh?n thanh toán
- H?y ??n hàng
- C?p nh?t ??n hàng
- L?i (t?t c? exception)

---

## ?? S? D?ng Các Tính N?ng

### 1?? G?i Email Xác Nh?n ??n Hàng

**T? ??ng khi checkout:**
- Email ???c g?i sau 5 giây (background job)
- Có th? thay ??i delay time
- Email có ch?a link ??n order details

### 2?? Real-time Order Notifications

**Hi?n th? trên trang OrderDetails:**
```html
<!-- Toast notification s? hi?n th? -->
<div id="notificationToast" class="toast">
    ...
</div>
```

**JavaScript s?:**
- K?t n?i t?i `/hub/notifications`
- L?ng nghe s? ki?n `ReceiveOrderNotification`
- Hi?n th? toast notification

### 3?? QR Code Tracking

**???c t?o t? ??ng:**
- Trên trang `OrderCompleted`
- Trên trang `OrderDetails`
- QR code link ??n order details page

**Cách s? d?ng:**
- Quét QR code ?? xem chi ti?t ??n hàng
- Dùng cho tracking system

### 4?? Export Excel

**Endpoint:**
```
GET /export-orders-excel
```

**Yêu c?u:**
- Ph?i là Admin
- Tr? v? file Excel v?i t?t c? ??n hàng

**D? li?u trong Excel:**
- ID, Customer Name, Phone, Address, Order Date, Total, Status, Item Count

### 5?? Caching

**T? ??ng cache:**
- Danh sách ??n hàng c?a user (10 phút)
- Clear cache khi c?p nh?t ??n hàng

**L?i ích:**
- T?ng t?c ?? t?i trang
- Gi?m t?i database

---

## ?? C?u Hình C?n Ch?nh

### 1. appsettings.json

```json
{
  "ConnectionStrings": {
    "RedisConnection": "localhost:6379"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password"  // Dùng Gmail App Password
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### 2. Gmail Setup

**N?u dùng Gmail ?? g?i email:**

1. B?t 2-factor authentication
2. T?o App Password t?i https://myaccount.google.com/apppasswords
3. Dùng App Password vào `SenderPassword` trong c?u hình

### 3. Redis & RabbitMQ (Tùy ch?n)

**Ch?y Docker:**
```bash
# Redis
docker run -d -p 6379:6379 redis:latest

# RabbitMQ
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:management
```

---

## ?? Các Endpoint M?i

| Method | Endpoint | Mô T? | Yêu C?u |
|--------|----------|-------|---------|
| POST | `/ShoppingCart/Checkout` | T?o ??n hàng | User logged in |
| POST | `/ShoppingCart/CancelOrder/{id}` | H?y ??n hàng | User owner |
| POST | `/ShoppingCart/UpdateOrder/{id}` | C?p nh?t ??n hàng | User owner |
| GET | `/ShoppingCart/OrderDetails/{id}` | Xem chi ti?t | User owner ho?c Admin |
| GET | `/ShoppingCart/MyOrders` | Danh sách ??n hàng c?a tôi | User logged in |
| GET | `/export-orders-excel` | Xu?t Excel | Admin |
| GET | `/hub/notifications` | SignalR Hub | WebSocket |

---

## ?? Các File ?ã ???c S?a

### 1. `Controllers/ShoppingCartController.cs`
- ? Thêm dependency injection cho t?t c? services
- ? Thêm logging cho t?t c? actions
- ? Thêm email notifications
- ? Thêm RabbitMQ message publishing
- ? Thêm SignalR real-time updates
- ? Thêm Redis caching
- ? Thêm QR code generation
- ? Thêm Excel export

### 2. `Views/ShoppingCart/OrderCompleted.cshtml`
- ? Hi?n th? QR code
- ? Thêm SignalR connection script

### 3. `Views/ShoppingCart/OrderDetails.cshtml`
- ? Hi?n th? QR code
- ? Hi?n th? status badge
- ? Thêm notification toast
- ? Thêm SignalR event listeners
- ? Responsive design

### 4. `Program.cs`
- ? ?ã c?u hình t?t c? services
- ? ?ã ??ng ký SignalR hub

---

## ?? Cách Test

### 1?? Test Email Notifications
```bash
# T?o ??n hàng m?i
1. ??ng nh?p
2. Thêm s?n ph?m vào gi?
3. Checkout
4. Ki?m tra email (có th? m?t 5 giây)
```

### 2?? Test Real-time Notifications
```bash
# M? 2 tab browser
1. Tab 1: ?ang xem OrderDetails
2. Tab 2: Admin h?y ??n hàng
3. Tab 1 s? t? ??ng reload
```

### 3?? Test QR Code
```bash
# Quét mã QR
1. Vào trang OrderCompleted ho?c OrderDetails
2. Quét QR code v?i ?i?n tho?i
3. S? redirect t?i order details page
```

### 4?? Test Excel Export
```bash
# Export ??n hàng
1. ??ng nh?p v?i Admin
2. Vào /export-orders-excel
3. File Excel s? t?i v?
```

### 5?? Test Hangfire Jobs
```bash
# Xem background jobs
1. Vào https://localhost:7xxx/hangfire
2. Xem t?t c? các email jobs
3. Ki?m tra status (Succeeded/Failed)
```

---

## ?? L?u Ý Quan Tr?ng

### B?o M?t
- ? Không commit email password vào Git
- ? Không commit JWT secret vào Git
- ? Dùng User Secrets cho development
- ? Dùng environment variables cho production

### Performance
- ? Cache ???c b?t cho danh sách ??n hàng
- ? Email g?i b?t ??ng b? (không ch?n request)
- ? RabbitMQ ?? x? lý b?t ??ng b?

### Troubleshooting

**Email không g?i:**
- Ki?m tra c?u hình SMTP
- Ki?m tra Gmail App Password
- Ki?m tra Hangfire dashboard

**Redis connection error:**
- Ch?y Redis: `docker run -d -p 6379:6379 redis:latest`
- Ki?m tra c?u hình connection string

**RabbitMQ connection error:**
- Ch?y RabbitMQ: `docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:management`
- Ki?m tra c?u hình hostname/port

**SignalR không ho?t ??ng:**
- Ki?m tra Hub route (`/hub/notifications`)
- Ki?m tra browser console có l?i không
- Ki?m tra HTTPS (SignalR WebSocket)

---

## ?? Tài Li?u Thêm

- **Serilog:** https://serilog.net/
- **Hangfire:** https://docs.hangfire.io/
- **SignalR:** https://learn.microsoft.com/en-us/aspnet/core/signalr/
- **Redis:** https://redis.io/docs/
- **RabbitMQ:** https://www.rabbitmq.com/documentation.html
- **ClosedXML:** https://github.com/ClosedXML/ClosedXML

---

**Phiên b?n:** 1.0  
**C?p nh?t:** 2024  
**Author:** Development Team
