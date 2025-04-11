using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Authentication.Application.Interfaces;
using Authentication.Applicationn.ApiClient;
using Authentication.Application.Dtos;

namespace Authentication.Application.Services;

public class EmailSenderService : ApiClientBase, IEmailSender
{
    private readonly string _notificationServiceUrl;

    public EmailSenderService(HttpClient httpClient,
        IConfiguration config,
        ILogger<EmailSenderService> logger)
        : base(httpClient, logger)
    {
        _notificationServiceUrl = config["NotificationService:BaseUrl"]
            ?? throw new InvalidOperationException("NotificationService:BaseUrl not configured");
    }

    public async Task<bool> SendVerificationEmailAsync(string recipient, string subject, string message)
    {
        var notification = new NotificationRequestDto
        {
            Recipient = recipient,
            Subject = subject,
            Message = message,
            Channel = "email"
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

