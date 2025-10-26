using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Application.Services;

public class SmsVerificationService : ISmsVerificationService
{
    private readonly ISmsSender _smsSender;
    private readonly ISmsCache _smsCache;
    private readonly IUserManagementService _userManagement;    

    public SmsVerificationService(
        ISmsSender smsSender,
        ISmsCache cache,
        IUserManagementService userManagementService)        
    {
        _smsSender = smsSender;
        _smsCache = cache;
        _userManagement = userManagementService;        
    }

    public async Task<Result<bool>> SendVerificationSmsAsync(string phoneNumber)
    {
        var code = GenerateCode();
        _smsCache.StoreCode(phoneNumber, code, TimeSpan.FromMinutes(5));
        var message = $"Your verification code is: {code}";

        try
        {
            var sent = await _smsSender.SendVerificationSmsAsync(phoneNumber, message);
            if (sent)
            {                
                return Result<bool>.Ok(data: true, message: "SMS verification published");
            }
            else
            {
                return Result<bool>.Fail("Error publishing SMS verification");
            }
        }
        catch (Exception)
        {
            return Result<bool>.Fail("Error publishing SMS verification");
        }
    }

    public async Task<Result<bool>> VerifySmsAsync(string phoneNumber, string code)
    {
        var cachedCode = _smsCache.GetCode(phoneNumber);
        var isValid = string.Equals(cachedCode, code, StringComparison.OrdinalIgnoreCase);

        if (isValid)
            _smsCache.RemoveCode(phoneNumber);

        await _userManagement.PhoneVerifiedAsync(phoneNumber);

        return Result<bool>.Ok(data: true, message: "Email verification Successfull");
    }

    public async Task<Result<bool>> SendMfaSmsAsync(string phoneNumber)
    {
        var code = GenerateCode();
        _smsCache.StoreCode(phoneNumber, code, TimeSpan.FromMinutes(5));
        var message = $"Your verification code is: {code}";

        try
        {
            var sent = await _smsSender.SendVerificationSmsAsync(phoneNumber, message);
            if (sent)
            {
                return Result<bool>.Ok(data: true, message: "Sms verification sent");
            }
            else
            {
                return Result<bool>.Fail("Sms verification failed to send");
            }
        }
        catch (Exception)
        {
            return Result<bool>.Fail("Error sending SMS verification");
        }
    }
    
    public bool VerifyMfaCode(string phoneNumber, string code)
    {
        var cachedCode = _smsCache.GetCode(phoneNumber);
        var isValid = string.Equals(cachedCode, code, StringComparison.OrdinalIgnoreCase);

        if (isValid)
            _smsCache.RemoveCode(phoneNumber);

        return isValid;
    }

    private string GenerateCode()
    {
        var random = new Random();
        return random.Next(100_000, 999_999).ToString();
    }
}
