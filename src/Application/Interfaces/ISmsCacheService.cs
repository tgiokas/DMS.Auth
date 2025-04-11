namespace Authentication.Application.Interfaces;

public interface ISmsCacheService
{
    void StoreCode(string phonenumber, string code, TimeSpan? ttl = null);
    string? GetCode(string phonenumber);
    void RemoveCode(string phonenumber);
}