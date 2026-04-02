using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using TINH_FINAL_2256.Services;
using System.Threading.Tasks;

namespace TINH_FINAL_2256.Examples
{
    /// <summary>
    /// Ví d? s? d?ng các services (xóa file này sau khi tham kh?o)
    /// </summary>
    public class ExampleUsageController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly IMessageQueueService _messageQueueService;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly IQRCodeService _qrCodeService;
        private readonly IExcelService _excelService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ExampleUsageController> _logger;

        public ExampleUsageController(
            IEmailService emailService,
            IMessageQueueService messageQueueService,
            IBackgroundJobService backgroundJobService,
            IQRCodeService qrCodeService,
            IExcelService excelService,
            IDistributedCache cache,
            ILogger<ExampleUsageController> logger)
        {
            _emailService = emailService;
            _messageQueueService = messageQueueService;
            _backgroundJobService = backgroundJobService;
            _qrCodeService = qrCodeService;
            _excelService = excelService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Ví d? 1: G?i email
        /// </summary>
        [HttpPost("example/send-email")]
        public async Task<IActionResult> SendEmailExample()
        {
            try
            {
                await _emailService.SendEmailAsync(
                    to: "customer@example.com",
                    subject: "Welcome to our store",
                    body: "<h1>Welcome!</h1><p>Thank you for your order.</p>",
                    isHtml: true);

                _logger.LogInformation("Email sent successfully");
                return Ok("Email sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ví d? 2: Publish message ??n RabbitMQ
        /// </summary>
        [HttpPost("example/publish-message")]
        public IActionResult PublishMessageExample()
        {
            try
            {
                _messageQueueService.PublishMessage("my-queue", new
                {
                    OrderId = 123,
                    Status = "Created",
                    Timestamp = DateTime.UtcNow
                });

                return Ok("Message published");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ví d? 3: Schedule background job
        /// </summary>
        [HttpPost("example/schedule-job")]
        public IActionResult ScheduleJobExample()
        {
            try
            {
                // Schedule email sau 5 phút
                var jobId = _backgroundJobService.ScheduleEmailJob(
                    email: "user@example.com",
                    subject: "Reminder",
                    body: "This is a delayed email",
                    delay: TimeSpan.FromMinutes(5));

                return Ok(new { jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling job");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ví d? 4: Generate QR code
        /// </summary>
        [HttpGet("example/qr-code")]
        public IActionResult GenerateQRCodeExample()
        {
            try
            {
                var qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(
                    text: "https://yourwebsite.com/order/123",
                    pixelsPerModule: 20);

                return Ok(new
                {
                    qrCode = $"data:image/png;base64,{qrCodeBase64}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ví d? 5: Cache d? li?u
        /// </summary>
        [HttpGet("example/cache")]
        public async Task<IActionResult> CacheExample()
        {
            try
            {
                string cacheKey = "sample_data";

                // Ki?m tra cache
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("Data found in cache");
                    return Ok(new { source = "cache", data = cachedData });
                }

                // N?u không có trong cache, t?o d? li?u m?i
                var data = "Sample data: " + DateTime.Now.ToString();
                
                // L?u vào cache v?i th?i gian s?ng 10 phút
                await _cache.SetStringAsync(cacheKey, data,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    });

                _logger.LogInformation("Data saved to cache");
                return Ok(new { source = "database", data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cache example");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ví d? 6: Export Excel
        /// </summary>
        [HttpGet("example/export-excel")]
        public async Task<IActionResult> ExportExcelExample()
        {
            try
            {
                // T?o sample data
                var sampleData = new List<dynamic>
                {
                    new { OrderId = 1, CustomerName = "John Doe", Total = 100 },
                    new { OrderId = 2, CustomerName = "Jane Smith", Total = 200 }
                };

                var excelBytes = await _excelService.ExportOrdersToExcelAsync(sampleData);

                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "orders.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ví d? 7: Logging
        /// </summary>
        [HttpGet("example/logging")]
        public IActionResult LoggingExample()
        {
            try
            {
                _logger.LogDebug("This is a debug message");
                _logger.LogInformation("This is an information message");
                _logger.LogWarning("This is a warning message");
                _logger.LogError("This is an error message");

                return Ok("Logs written");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error in logging example");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Ví d? 8: Clear cache
        /// </summary>
        [HttpPost("example/clear-cache")]
        public async Task<IActionResult> ClearCacheExample()
        {
            try
            {
                await _cache.RemoveAsync("sample_data");
                _logger.LogInformation("Cache cleared");
                return Ok("Cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return BadRequest(ex.Message);
            }
        }
    }
}
