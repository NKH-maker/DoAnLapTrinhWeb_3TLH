using RabbitMQ.Client;
using System.Text;

namespace TINH_FINAL_2256.Services
{
    /// <summary>
    /// D?ch v? RabbitMQ Message Queue
    /// </summary>
    public interface IMessageQueueService
    {
        void PublishMessage<T>(string queueName, T message);
        void PublishOrderNotification(string orderId, string message);
    }

    public class RabbitMQService : IMessageQueueService
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConnectionFactory connectionFactory, ILogger<RabbitMQService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public void PublishMessage<T>(string queueName, T message)
        {
            try
            {
                using (var connection = _connectionFactory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

                    var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(jsonMessage);

                    channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
                    _logger.LogInformation($"Message published to queue '{queueName}'");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error publishing message to queue '{queueName}': {ex.Message}");
            }
        }

        public void PublishOrderNotification(string orderId, string message)
        {
            PublishMessage("order-notifications", new { OrderId = orderId, Message = message, Timestamp = DateTime.UtcNow });
        }
    }
}
