using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.Messaging;

public class KafkaEmailSender : IEmailSender
{
    private readonly IMessagePublisher _kafkaPublisher;
    private readonly ILogger<KafkaEmailSender> _logger;

    public KafkaEmailSender(IMessagePublisher kafkaPublisher, ILogger<KafkaEmailSender> logger)
    {
        _kafkaPublisher = kafkaPublisher;
        _logger = logger;
    }

    public async Task<bool> SendVerificationEmailAsync(string email, string subject, string message)
    {
        var notification = new NotificationDto
        {
            Recipient = email,
            Subject = subject,
            Message = message,
            Channel = "email"
        };

        var messageId = Guid.NewGuid().ToString("N");
        var envelope = new KafkaMessage<NotificationDto>
        {
            Id = messageId,
            Content = notification,
            Timestamp = DateTime.UtcNow
        };

        var headers = new[]
        {
            new KeyValuePair<string, string>("content-type", "application/json"),
            new KeyValuePair<string, string>("x-channel", "email")
        };

        try
        {
            await _kafkaPublisher.PublishJsonAsync(
                route: "email",
                key: email,
                payload: envelope,
                headers: headers
            );
            _logger.LogInformation("Email published to Kafka for {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish email to Kafka for {Email}", email);
            return false;
        }
    }
}
