using RabbitMQ.Client;
using System.Text;
using DMS.Auth.Domain.Entities;
using DMS.Auth.Domain.Interfaces;
using System.Text.Json;

namespace DMS.Auth.Infrastructure.Audit
{
    public class RabbitMqAuditEventPublisher : IAuditEventPublisher
    {
        private readonly IConnection _connection;

        public RabbitMqAuditEventPublisher(IConnection connection)
        {
            _connection = connection;
        }

        public async Task PublishUserCreatedAsync(User user)
        {
            var payload = new
            {
                EventType = "UserCreated",
                UserId = user.Id,
                Username = user.Username,
                AgencyId = user.AgencyId,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync(payload);
        }

        public async Task PublishMfaEnabledAsync(User user)
        {
            var payload = new
            {
                EventType = "MfaEnabled",
                UserId = user.Id,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync(payload);
        }

        private async Task PublishMessageAsync(object messageObj)
        {
            using var channel = await _connection.CreateChannelAsync();
            channel.ExchangeDeclareAsync(exchange: "audit-exchange", type: "topic", durable: true);

            var json = JsonSerializer.Serialize(messageObj);
            var body = Encoding.UTF8.GetBytes(json);

            //channel.BasicPublishAsync(
            //    exchange: "audit-exchange",
            //    routingKey: "auth.events",
            //    mandatory: false,
            //    basicProperties: null,
            //    body: body
            //);
        }
    }
}
