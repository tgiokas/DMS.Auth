using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Authentication.Applicationn.ApiClient;

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

    protected async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request)
    {
        string requestBody = request.Content != null ? await request.Content.ReadAsStringAsync() : string.Empty;
        var sw = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorMessageTemplate, "Outgoing", request.Method,
                request.RequestUri, requestBody, HttpStatusCode.ServiceUnavailable, "");
            throw;
        }

        sw.Stop();
        string responseBody = await response.Content.ReadAsStringAsync();
        int statusCode = (int)response.StatusCode;
        LogLevel logLevel = statusCode > 499 ? LogLevel.Error : LogLevel.Information;

        _logger.Log(logLevel, LogMessageTemplate, "Outgoing", request.Method,
            request.RequestUri, requestBody, statusCode, responseBody, (long)sw.ElapsedMilliseconds);

        return response;
    }
}
