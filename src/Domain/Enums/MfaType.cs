namespace Authentication.Domain.Enums;

public enum MfaType
{
    None = 0,
    Totp = 1,
    Email = 2,
    Sms = 3
}
