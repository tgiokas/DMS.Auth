using Microsoft.Extensions.Configuration;

namespace Authentication.Application.Configuration;

public class KafkaSettings
{
    // Bootstrap servers
    public string BootstrapServers { get; set; } = string.Empty;    

    // Base delay before reconnecting to a broker
    public int ReconnectBackoffMs { get; set; }

    // Maximum delay when exponential backoff applies
    public int ReconnectBackoffMaxMs { get; set; }

    // Time allowed to establish initial TCP connection
    public int SocketConnectionSetupTimeoutMs { get; set; }

    // How long to wait for socket operations before failing
    public int SocketTimeoutMs { get; set; }


    // Topic for the Producer
    public string Topic { get; set; } = string.Empty;

    // Wait between retries to avoid hammering the broker
    public int RetryBackoffMs { get; set; }

    // Max time broker has to respond to produce request
    public int RequestTimeoutMs { get; set; }

    // Max time before message is considered failed (client side)
    public int MessageTimeoutMs { get; set; }

    public static KafkaSettings BindFromConfiguration(IConfiguration configuration)
    {
        return new KafkaSettings
        {
            // Broker connection settings
            BootstrapServers = configuration["KAFKA_BOOTSTRAP_SERVERS"]
                ?? throw new ArgumentNullException(nameof(configuration), "KAFKA_BOOTSTRAP_SERVERS is not set."),
            ReconnectBackoffMs = ParseInt(configuration, "AUTH_KAFKA_RECONNECT_BACKOFF_MS"),
            ReconnectBackoffMaxMs = ParseInt(configuration, "AUTH_KAFKA_RECONNECT_BACKOFF_MAX_MS"),
            SocketConnectionSetupTimeoutMs = ParseInt(configuration, "AUTH_KAFKA_SOCKET_CONNECTION_SETUP_TIMEOUT_MS"),
            SocketTimeoutMs = ParseInt(configuration, "AUTH_KAFKA_SOCKET_TIMEOUT_MS"),

            // Producer settings
            Topic = configuration["AUTH_KAFKA_TOPIC"]
                ?? throw new ArgumentNullException(nameof(configuration), "AUTH_KAFKA_TOPIC is not set."),
            RetryBackoffMs = ParseInt(configuration, "AUTH_KAFKA_RETRY_BACKOFF_MS"),
            RequestTimeoutMs = ParseInt(configuration, "AUTH_KAFKA_REQUEST_TIMEOUT_MS"),
            MessageTimeoutMs = ParseInt(configuration, "AUTH_KAFKA_MESSAGE_TIMEOUT_MS"),                 
        };
    }

    private static int ParseInt(IConfiguration config, string key)
    {
        var raw = config[key]
            ?? throw new ArgumentNullException(nameof(config), $"{key} is not set.");
        if (!int.TryParse(raw, out var value))
            throw new ArgumentException($"{key} is not a valid integer.", nameof(config));
        return value;
    }
}
