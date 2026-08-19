namespace Authentication.Application.Interfaces;

public interface IEmailCache
{
    // Email account verification
    Task StoreTokenAsync(string token, string email, TimeSpan? ttl = null);
    Task<string?> GetEmailByTokenAsync(string token);
    Task RemoveTokenAsync(string token);

    // Email verification code (public /EmailVerify/send-code flow)
    Task StoreCodeAsync(string email, string code, TimeSpan? ttl = null);
    Task<string?> GetCodeAsync(string email);
    Task RemoveCodeAsync(string email);

    // MFA-during-login code — separate namespace so the public verification
    // flow above can't overwrite a code for a login that's still in progress.
    Task StoreMfaLoginCodeAsync(string email, string code, TimeSpan? ttl = null);
    Task<string?> GetMfaLoginCodeAsync(string email);
    Task RemoveMfaLoginCodeAsync(string email);

    TimeSpan MfaCodeDuration { get; }
}

