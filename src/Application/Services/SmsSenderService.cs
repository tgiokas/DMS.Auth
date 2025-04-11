using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Authentication.Application.Interfaces;
using Authentication.Applicationn.ApiClient;
using Authentication.Application.Dtos;

namespace Authentication.Application.Services;

public class SmsSenderService : ApiClientBase, ISmsSender
{
    private readonly string _notificationServiceUrl;

    public SmsSenderService(HttpClient httpClient,
        IConfiguration config,
        ILogger<SmsSenderService> logger)
        : base(httpClient, logger)
    {
        _notificationServiceUrl = config["NotificationService:BaseUrl"]
            ?? throw new InvalidOperationException("NotificationService:BaseUrl not configured");
    }

    public async Task<bool> SendVerificationSmsAsync(string phoneNumber, string message)
    {
        var notification = new NotificationRequestDto
        {
            Recipient = phoneNumber,
            Subject = "New Message",
            Message = message,
            Channel = "sms"
        };

        var requestUrl = $"{_notificationServiceUrl}/api/notifications";

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = JsonContent.Create(notification)
        };

        var response = await SendRequestAsync(request);

        if (!response.IsSuccessStatusCode)
            return false;

        return true;
    }
}

