namespace Authentication.Application.Interfaces;

public interface IAuthorizationCache
{
    Task<bool?> GetCachedDecisionAsync(string token, string resource, string scope);
    Task SetCachedDecisionAsync(string token, string resource, string scope, bool decision, TimeSpan duration);
    Task ClearAsync();
}
