using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;

namespace Authentication.Infrastructure.Repositories;

public class RolePermissionRepo : IRolePermissionRepo
{
    private readonly ApplicationDbContext _dbContext;

    public RolePermissionRepo(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<RolePermission>> GetAllAsync()
    {
        return await _dbContext.RolePermissions.ToListAsync();
    }

    public async Task<RolePermission?> GetByIdAsync(int id)
    {
        return await _dbContext.RolePermissions
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<RolePermission>> GetByRoleIdAsync(Guid roleId)
    {
        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(r => r.KeycloakRoleId == roleId)
            .ToListAsync();
    }

    public async Task<bool> IsEndpointAuthorizedAsync(Guid roleId, string httpMethod, string path)
    {
        var normalizedMethod = httpMethod.ToUpperInvariant();
        var normalizedPath = path.Split('?')[0].TrimEnd('/');

        var candidates = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(r => r.KeycloakRoleId == roleId &&
                        r.HttpMethod.ToUpper() == normalizedMethod &&
                        r.Allowed)
            .ToListAsync();

        var isAuthorized = candidates.Any(r =>
        r.EndPoints.Any(ep =>
        {
            // Trim pattern
            var pattern = ep.TrimEnd('/');

            // Build base pattern without placeholder
            var patternBase = Regex.Replace(pattern, "{[^}]+}", "__PLACEHOLDER__");
            patternBase = patternBase.Replace("*", ".*");
                        
            // Numeric
            var numericPattern = "^" + patternBase.Replace("__PLACEHOLDER__", "[0-9]+") + "/?$";

            // UUID
            var uuidPattern = "^" + patternBase.Replace("__PLACEHOLDER__",
                "[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}") + "/?$";

            // Try numeric and then UUID
            return Regex.IsMatch(normalizedPath, numericPattern, RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(normalizedPath, uuidPattern, RegexOptions.IgnoreCase);
        }));

        return isAuthorized;
    }

    public async Task<bool> IsEndpointAuthorizedOptimizedAsync(Guid roleId, string httpMethod, string path)
    {
        const string sql = @"
        SELECT NOT EXISTS (
                SELECT 1
                FROM ""RolePermissions""
                WHERE ""KeycloakRoleId"" = @p0
                  AND UPPER(""HttpMethod"") = UPPER(@p1)
                  AND NOT ""Allowed""
                  AND EXISTS (
                      SELECT 1
                      FROM jsonb_array_elements_text(""EndPoints"") AS ep
                      WHERE @p2 ILIKE REPLACE(ep, '*', '%')
                  )
            )";

        return await _dbContext.Database
            .SqlQueryRaw<bool>(sql, roleId, httpMethod, path)
            .FirstAsync();
    }    

    public async Task AddAsync(RolePermission rule)
    {
        await _dbContext.RolePermissions.AddAsync(rule);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(RolePermission rule)
    {
        _dbContext.RolePermissions.Update(rule);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(RolePermission rule)
    {
        _dbContext.RolePermissions.Remove(rule);
        await _dbContext.SaveChangesAsync();
    }
}