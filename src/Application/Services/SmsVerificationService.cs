using Microsoft.Extensions.Logging;
using Authentication.Application.Interfaces;

namespace Authentication.Application.Services;

public class SmsVerificationService : ISmsVerificationService
{
    private readonly ISmsSender _smsSender;
    private readonly ISmsCacheService _cache;
    private readonly IUserManagementService _userManagement;
    private readonly ILogger<SmsVerificationService> _logger;

    public SmsVerificationService(ISmsSender smsSender, 
        ISmsCacheService cache,         
        IUserManagementService userManagementService,
        ILogger<SmsVerificationService> logger)
    {
        _smsSender = smsSender;
        _cache = cache;
        _userManagement = userManagementService;
        _logger = logger;
    }

    public async Task SendVerificationSmsAsync(string phoneNumber)
    {
        var code = GenerateCode(); 
        _cache.StoreCode(phoneNumber, code, TimeSpan.FromMinutes(5));

        var message = $"Your verification code is: {code}";
        await _smsSender.SendVerificationSmsAsync(phoneNumber, message);

        _logger.LogInformation("SMS verification code sent to {PhoneNumber}", phoneNumber);
    }

    public async Task<bool> VerifySmsAsync(string phoneNumber, string code)
    {
        var cachedCode = _cache.GetCode(phoneNumber);
        var isValid = string.Equals(cachedCode, code, StringComparison.OrdinalIgnoreCase);

        if (!isValid)       
            return false;

        _cache.RemoveCode(phoneNumber);

        await _userManagement.MarkPhoneAsVerifiedAsync(phoneNumber);
        return true;
    }

    private string GenerateCode()
    {
        var random = new Random();
        return random.Next(100_000, 999_999).ToString();
    }
}
