using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Confluent.Kafka;

using Authentication.Application.Interfaces;

namespace Authentication.Infrastructure.Messaging;

public sealed class KafkaPublisher : IMessagePublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaPublisher> _logger;

    public KafkaPublisher(IConfiguration config, ILogger<KafkaPublisher> logger)
    {
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            // Bootstrap servers
            BootstrapServers = config["KAFKA_BOOTSTRAP_SERVERS"]
                ?? throw new ArgumentNullException(nameof(config), "KAFKA_BOOTSTRAP_SERVERS is not set."),

            // Durability / Acknowledgement
            Acks = Enum.Parse<Acks>(
                config["AUTH_KAFKA_ACKS"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_ACKS is not set.")),
            
            // Base delay before reconnecting to a broker
            ReconnectBackoffMs = int.Parse(
                config["AUTH_KAFKA_RECONNECT_BACKOFF_MS"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_RECONNECT_BACKOFF_MS is not set.")),

            // Maximum delay when exponential backoff applies
            ReconnectBackoffMaxMs = int.Parse(
                config["AUTH_KAFKA_RECONNECT_BACKOFF_MAX_MS"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_RECONNECT_BACKOFF_MAX_MS is not set.")),
            
            // Time allowed to establish initial TCP connection
            SocketConnectionSetupTimeoutMs = int.Parse(
                config["AUTH_KAFKA_SOCKET_CONNECTION_SETUP_TIMEOUT_MS"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_SOCKET_CONNECTION_SETUP_TIMEOUT_MS is not set.")),

            // How long to wait for socket operations before failing
            SocketTimeoutMs = int.Parse(
                config["AUTH_KAFKA_SOCKET_TIMEOUT_MS"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_SOCKET_TIMEOUT_MS is not set.")),
            
            // How many times the .NET client retries failed sends
            MessageSendMaxRetries = int.Parse(
                config["AUTH_KAFKA_MESSAGE_SEND_MAX_RETRIES"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_MESSAGE_SEND_MAX_RETRIES is not set.")),

            // Wait between retries to avoid hammering the broker
            RetryBackoffMs = int.Parse(
                config["AUTH_KAFKA_RETRY_BACKOFF_MS"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_RETRY_BACKOFF_MS is not set.")),
            
            // Max time broker has to respond to produce request
            RequestTimeoutMs = int.Parse(
                config["AUTH_KAFKA_REQUEST_TIMEOUT_MS"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_REQUEST_TIMEOUT_MS is not set.")),

            // Max time before message is considered failed (client side)
            MessageTimeoutMs = int.Parse(
                config["AUTH_KAFKA_MESSAGE_TIMEOUT_MS"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_MESSAGE_TIMEOUT_MS is not set.")),

            // Enabling idempotent producers
            EnableIdempotence = bool.Parse(
                config["AUTH_KAFKA_ENABLE_IDEMPOTENCE"]
                ?? throw new ArgumentNullException(nameof(config), "AUTH_KAFKA_ENABLE_IDEMPOTENCE is not set."))
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishJsonAsync<T>(
        string route,
        string key,
        T payload,
        IEnumerable<KeyValuePair<string, string>>? headers = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);

            var msg = new Message<string, string>
            {
                Key = key ?? string.Empty,
                Value = json,
                Headers = new Headers()
            };

            if (headers is not null)
            {
                foreach (var h in headers)
                    msg.Headers!.Add(h.Key, System.Text.Encoding.UTF8.GetBytes(h.Value));
            }

            var result = await _producer.ProduceAsync(route, msg, cancellationToken);
           
            _logger.LogDebug("Produced to {TP} (offset {Offset})", result.TopicPartition, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka produce error: {Reason}", ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        try { _producer.Flush(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) { _logger.LogWarning(ex, "Error flushing Kafka producer during dispose"); }
        finally { _producer.Dispose(); }
    }
}
