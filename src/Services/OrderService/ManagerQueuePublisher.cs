using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace OrderService
{
    public sealed record OrderSubmittedMessage(
        string OrderId,
        string TenantId,
        DateTimeOffset CreatedAt
    );

    public interface IManagerQueuePublisher
    {
        Task PublishOrderSubmittedAsync(OrderSubmittedMessage msg, CancellationToken ct);
    }

    public sealed class ManagerQueuePublisher : IManagerQueuePublisher
    {
        private readonly ServiceBusSender _sender;

        public ManagerQueuePublisher(ServiceBusClient client, IConfiguration cfg)
        {
            var queueName = cfg["ServiceBus:ManagerQueueName"]!;
            _sender = client.CreateSender(queueName);
        }

        public async Task PublishOrderSubmittedAsync(OrderSubmittedMessage msg, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(msg);
            var sbMsg = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                MessageId = $"{msg.TenantId}:{msg.OrderId}",     // helpful for idempotency
                CorrelationId = msg.OrderId,
                Subject = "OrderSubmitted"
            };

            // If you want ordering-per-order, set session id to orderId and enable sessions on queue
            // sbMsg.SessionId = msg.OrderId;

            await _sender.SendMessageAsync(sbMsg, ct);
        }
    }

}
