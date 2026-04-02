using System;

namespace TINH_FINAL_2256.Services
{
    /// <summary>
    /// Lightweight no-op message queue implementation used when RabbitMQ is not enabled.
    /// This prevents controllers from throwing if IMessageQueueService is injected but no external MQ is available.
    /// </summary>
    public class NullMessageQueueService : IMessageQueueService
    {
        private readonly ILogger<NullMessageQueueService> _logger;

        public NullMessageQueueService(ILogger<NullMessageQueueService> logger)
        {
            _logger = logger;
        }

        public void PublishMessage<T>(string queueName, T message)
        {
            // Intentionally do nothing in production-less setups. Log at debug level for diagnostics.
            _logger.LogDebug("NullMessageQueueService.PublishMessage called for queue '{QueueName}' - message omitted.", queueName);
        }

        public void PublishOrderNotification(string orderId, string message)
        {
            _logger.LogDebug("NullMessageQueueService.PublishOrderNotification called for order {OrderId} - message omitted.", orderId);
        }
    }
}
