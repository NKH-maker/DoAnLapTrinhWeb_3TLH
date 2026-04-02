using Microsoft.AspNetCore.SignalR;

namespace TINH_FINAL_2256.Hubs
{
    /// <summary>
    /// SignalR Hub cho real-time notifications
    /// </summary>
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
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
            await Clients.User(userId).SendAsync("ReceiveUserNotification", message);
        }

        // G?i c?p nh?t gi? hàng
        public async Task SendCartUpdate(string userId, int itemCount, decimal total)
        {
            await Clients.User(userId).SendAsync("CartUpdated", itemCount, total);
        }

        // Broadcast message
        public async Task BroadcastMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
