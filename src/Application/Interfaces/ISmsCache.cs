namespace Authentication.Application.Interfaces;

public interface ISmsCache
{
    void StoreCode(string phonenumber, string code, TimeSpan? ttl = null);
    string? GetCode(string phonenumber);
    void RemoveCode(string phonenumber);

    // MFA-during-login code — separate namespace so the standalone SMS
    // verification flow above can't overwrite a code for a login in progress.
    void StoreMfaLoginCode(string phonenumber, string code, TimeSpan? ttl = null);
    string? GetMfaLoginCode(string phonenumber);
    void RemoveMfaLoginCode(string phonenumber);
}