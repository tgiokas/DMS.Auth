using Microsoft.Extensions.Caching.Distributed;

using Authentication.Application.Interfaces;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IKeycloakClientRole _keycloakClientRole;
    private readonly IRolePermissionRepo _rolePermissionRepo;
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(10);

    public AuthorizationService(
        IKeycloakClientRole keycloakClientRole,
        IRolePermissionRepo rolePermissionRepo,
        IDistributedCache cache)
    {
        _keycloakClientRole = keycloakClientRole;
        _rolePermissionRepo = rolePermissionRepo;
        _cache = cache;
    }

    public async Task<bool> IsAuthorizedAsync(string role, string path, string method)
    {        
        var keycloakRole = await _keycloakClientRole.GetRoleByNameAsync(role);
        if (keycloakRole == null)
            return false;

        var roleId = Guid.Parse(keycloakRole.Id);
        var normalizedMethod = method.ToUpperInvariant();
        var normalizedPath = path.ToLowerInvariant();

        // Construct cache key
        var cacheKey = $"authz:{roleId}:{normalizedMethod}:{normalizedPath}";

        // Try to read cacheKey from Distributed Cache
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return bool.Parse(cached);

        // Query DB
        var isAuthorized = await _rolePermissionRepo.IsEndpointAuthorizedAsync(roleId, normalizedMethod, path);

        // Cache authorization result 
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _defaultTtl
        };

        await _cache.SetStringAsync(cacheKey, isAuthorized.ToString(), options);

        return isAuthorized;
    }
}
