using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using TINH_FINAL_2256.Models;

namespace TINH_FINAL_2256.Hubs
{
    /// <summary>
    /// SignalR Hub cho real-time notifications và chat support
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationHub(ILogger<NotificationHub> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{user.Id}");
                _logger.LogInformation($"User {user.Email} connected: {Context.ConnectionId}");
            }
            else
            {
                _logger.LogInformation($"Anonymous client connected: {Context.ConnectionId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null)
            {
                _logger.LogInformation($"User {user.Email} disconnected: {Context.ConnectionId}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // G?i thông báo ??n hàng
        public async Task SendOrderNotification(string orderId, string message)
        {
            await Clients.All.SendAsync("ReceiveOrderNotification", orderId, message);
            _logger.LogInformation($"Order notification sent: {orderId}");
        }

        // G?i thông báo cho ng??i dùng c? th?
        public async Task SendUserNotification(string userId, string message)
        {
            await Clients.Group($"user_{userId}").SendAsync("ReceiveUserNotification", message);
        }

        // G?i c?p nh?t gi? hàng
        public async Task SendCartUpdate(string userId, int itemCount, decimal total)
        {
            await Clients.Group($"user_{userId}").SendAsync("CartUpdated", itemCount, total);
        }

        // Broadcast message
        public async Task BroadcastMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        // Support chat: Nh?n tin nh?n t? user
        public async Task SendSupportMessage(string message)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null) return;

            var timestamp = DateTime.Now;
            
            // G?i tin nh?n c?a user t?i admin/support
            await Clients.Group("support_team").SendAsync("ReceiveMessage", user.FullName ?? user.Email, message, timestamp);
            
            // G?i confirmation t?i user
            await Clients.User(user.Id).SendAsync("ReceiveMessage", "You", message, timestamp);
            
            _logger.LogInformation($"Support message from {user.Email}: {message}");
        }

        // Admin/Support: G?i reply t?i user
        public async Task SendSupportReply(string userId, string message)
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null) return;

            // Ki?m tra xem user có ph?i admin/support không
            var isSupport = await _userManager.IsInRoleAsync(user, "Admin");
            if (!isSupport) return;

            var timestamp = DateTime.Now;
            
            // G?i reply t?i user
            await Clients.Group($"user_{userId}").SendAsync("ReceiveMessage", "Support", message, timestamp);
            
            _logger.LogInformation($"Support reply to {userId}: {message}");
        }

        // Admin: Thêm vào group support team
        public async Task JoinSupportTeam()
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "support_team");
                _logger.LogInformation($"Support member {user.Email} joined support team");
            }
        }
    }
}
