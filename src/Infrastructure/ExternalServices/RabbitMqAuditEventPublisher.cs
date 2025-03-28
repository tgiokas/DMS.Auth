using System.Text;
using System.Text.Json;

using RabbitMQ.Client;

using DMS.Auth.Domain.Entities;
using DMS.Auth.Domain.Interfaces;

namespace DMS.Auth.Infrastructure.ExternalServices;

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
            user.Username,
            user.AgencyId,
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
        await channel.ExchangeDeclareAsync(exchange: "audit-exchange", type: "topic", durable: true);

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
