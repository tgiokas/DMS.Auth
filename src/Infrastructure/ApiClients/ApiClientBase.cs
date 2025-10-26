using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace Authentication.Infrastructure.ApiClients;

public abstract class ApiClientBase
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;

    const string LogMessageTemplate =
        "HTTP {Direction} {RequestMethod} {RequestPath} {RequestPayload} responded {HttpStatusCode} {ResponsePayload} in {Elapsed:0.0000} ms";

    const string ErrorMessageTemplate =
        "ERROR {Direction} {RequestMethod} {RequestPath} {RequestPayload} responded {HttpStatusCode} {ResponsePayload}";

    protected ApiClientBase(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        string requestBody = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : string.Empty;
        var sw = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorMessageTemplate, "Outgoing", request.Method,
                request.RequestUri, requestBody, HttpStatusCode.ServiceUnavailable, "");
            
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Service is temporarily unavailable.")
            };
        }

        sw.Stop();
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        int statusCode = (int)response.StatusCode;
        LogLevel logLevel = statusCode > 499 ? LogLevel.Error : LogLevel.Information;

        _logger.Log(logLevel, LogMessageTemplate, "Outgoing", request.Method,
            request.RequestUri, requestBody, statusCode, responseBody, (long)sw.ElapsedMilliseconds);

        return response;
    }

    protected async Task<HttpResponseMessage> SendRequestAsync2(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        string requestBody = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : string.Empty;
        var sw = Stopwatch.StartNew();

        // Define a retry policy
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult<HttpResponseMessage>(response => (int)response.StatusCode >= 500)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (outcome, timeSpan, retryCount, context) =>
            {
                if (outcome.Exception != null)
                {
                    _logger.LogWarning(ErrorMessageTemplate, "Outgoing Retry", request.Method, request.RequestUri, requestBody, HttpStatusCode.ServiceUnavailable, outcome.Exception.Message);
                }
                else
                {
                    _logger.LogWarning(LogMessageTemplate, "Outgoing Retry", request.Method, request.RequestUri, requestBody, (int)outcome.Result.StatusCode, "", (long)timeSpan.TotalMilliseconds);
                }
            });

        HttpResponseMessage response;
        try
        {
            // Execute the request with the retry policy
            response = await retryPolicy.ExecuteAsync(async () =>
            {
                // Clone the request before each retry
                var clonedRequest = await CloneHttpRequestMessageAsync(request);
                return await _httpClient.SendAsync(clonedRequest, cancellationToken);
            });
        }
        catch (Exception ex)
        {
            // Log the final failure using the ErrorMessageTemplate
            _logger.LogError(ex, ErrorMessageTemplate, "Outgoing", request.Method, request.RequestUri, requestBody, HttpStatusCode.ServiceUnavailable, ex.Message);
            
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("Service is temporarily unavailable.")
            };
        }

        sw.Stop();
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        int statusCode = (int)response.StatusCode;
        LogLevel logLevel = statusCode > 499 ? LogLevel.Error : LogLevel.Information;

        // Log the final response using the LogMessageTemplate
        _logger.Log(logLevel, LogMessageTemplate, "Outgoing", request.Method, request.RequestUri, requestBody, statusCode, responseBody, (long)sw.ElapsedMilliseconds);

        return response;
    }


    // Helper method to clone HttpRequestMessage
    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clonedRequest = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Clone the content if it exists
        if (request.Content != null)
        {
            var contentStream = await request.Content.ReadAsStreamAsync();
            var memoryStream = new MemoryStream();
            await contentStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // Reset the stream position for the cloned request
            clonedRequest.Content = new StreamContent(memoryStream);

            // Copy content headers
            if (request.Content.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    clonedRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        // Copy headers
        foreach (var header in request.Headers)
        {
            clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy options
        foreach (var property in request.Options)
        {
            clonedRequest.Options.TryAdd(property.Key, property.Value);
        }

        return clonedRequest;
    }
}
