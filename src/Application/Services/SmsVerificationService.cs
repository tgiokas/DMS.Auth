using System.Security.Cryptography;
using System.Text;

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
        var isValid = FixedTimeEquals(cachedCode, code);

        if (isValid)
            _smsCache.RemoveCode(phoneNumber);

        await PhoneVerifiedAsync(phoneNumber);

        return Result<bool>.Ok(data: true, message: "Email verification Successfull");
    }

    public async Task<Result<bool>> SendMfaSmsAsync(string phoneNumber)
    {
        var code = GenerateCode();
        _smsCache.StoreMfaLoginCode(phoneNumber, code, TimeSpan.FromMinutes(5));
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
        var cachedCode = _smsCache.GetMfaLoginCode(phoneNumber);
        var isValid = FixedTimeEquals(cachedCode, code);

        if (isValid)
            _smsCache.RemoveMfaLoginCode(phoneNumber);

        return isValid;
    }

    private async Task<Result<bool>> PhoneVerifiedAsync(string phoneNumber)
    {
        //var dbUser = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
        //if (dbUser == null)
        //{
        //    return _errors.Fail<bool>(ErrorCodes.AUTH.UserNotFoundInDB);
        //}

        //dbUser.PhoneVerified = true;
        //await _userRepository.UpdateAsync(dbUser);

        return Result<bool>.Ok(data: true, message: "Phone Verified.");
    }

    private static string GenerateCode()
    {
        return RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
    }

    private static bool FixedTimeEquals(string? a, string? b)
    {
        if (a is null || b is null)
        {
            return false;
        }

        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
