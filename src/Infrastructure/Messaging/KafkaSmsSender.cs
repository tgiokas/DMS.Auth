using Microsoft.Extensions.Logging;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.Messaging;

public class KafkaSmsSender : ISmsSender
{
    private readonly IMessagePublisher _kafkaPublisher;
    private readonly ILogger<KafkaSmsSender> _logger;

    public KafkaSmsSender(IMessagePublisher kafkaPublisher, ILogger<KafkaSmsSender> logger)
    {
        _kafkaPublisher = kafkaPublisher;
        _logger = logger;
    }

    public async Task<bool> SendVerificationSmsAsync(string phoneNumber, string message)
    {
        var notification = new NotificationDto
        {
            Recipient = phoneNumber,
            Message = message,
            Channel = "sms"
        };

        var headers = new[]
        {
            new KeyValuePair<string, string>("content-type", "application/json"),
            new KeyValuePair<string, string>("x-channel", "sms")
        };

        try
        {
            await _kafkaPublisher.PublishJsonAsync(
                route: "sms",
                key: phoneNumber,
                payload: notification,
                headers: headers
            );
            _logger.LogInformation("SMS published to Kafka for {PhoneNumber}", phoneNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish SMS to Kafka for {PhoneNumber}", phoneNumber);
            return false;
        }
    }
}
