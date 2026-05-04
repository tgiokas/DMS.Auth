namespace Authentication.Api.Constants;

public static class CookieConstants
{
    public const int AccessTokenCookieExpirationMins = 5;
    public const int RefreshTokenCookieExpirationHours = 8;
    /// <summary>When "Remember me" is checked, refresh and flag cookies use this lifetime.</summary>
    public const int RefreshTokenCookieRememberMeDays = 30;
}