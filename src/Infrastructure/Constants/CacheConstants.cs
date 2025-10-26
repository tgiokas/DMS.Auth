namespace Authentication.Infrastructure.Constants;

public static class CacheConstants
{
    public const int TotpCacheTtlMins = 10;
    public const int PasswordResetCacheTtlMins = 10;
    public const int EmailCacheTtlMins = 10;
    public const int SafetyBufferSec = 30;
    public const int ClientUUIDCacheHours = 24;
}