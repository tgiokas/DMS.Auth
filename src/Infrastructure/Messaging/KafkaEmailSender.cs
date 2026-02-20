using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.Messaging;

public class KafkaEmailSender : IEmailSender
{
    private readonly IMessagePublisher _kafkaPublisher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaEmailSender> _logger;
    private readonly string _emailChannel;

    public KafkaEmailSender(IMessagePublisher kafkaPublisher, IConfiguration configuration, ILogger<KafkaEmailSender> logger)
    {
        _kafkaPublisher = kafkaPublisher;
        _configuration = configuration;
        _logger = logger;

        _emailChannel = _configuration["AUTH_KAFKA_TOPIC"] ?? throw new ArgumentNullException(nameof(configuration), "AUTH_KAFKA_TOPIC is empty."); ;
    }

    public async Task<bool> SendEmailAsync(NotificationEmailDto notification)
    {       
        var messageId = Guid.NewGuid().ToString("N");
        var envelope = new KafkaMessage<NotificationEmailDto>
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
                route: _emailChannel, 
                key: notification.Recipient,
                payload: envelope,
                headers: headers
            );
            _logger.LogInformation("Email published to Kafka for {Email}", notification.Recipient);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish email to Kafka for {Email}", notification.Recipient);
            return false;
        }
    }
}
