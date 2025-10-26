using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IBusinessRuleRepository _ruleRepo;

    public AuthorizationService(IBusinessRuleRepository ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    public async Task<bool> IsAuthorizedAsync(UserContext user, string path, string method)
    {
        var deptRolePairs = user.DepartmentRoles
            .SelectMany(kvp => kvp.Value.Select(roleGuid => new { DepartmentId = kvp.Key.Id, RoleGuid = roleGuid }));

        foreach (var pair in deptRolePairs)
        {
            var rules = await _ruleRepo.GetByDepartmentRoleAndMethodAsync(pair.DepartmentId, pair.RoleGuid, method);

            var match = rules.FirstOrDefault(r =>
                GlobMatch(path, r.PathPattern));

            if (match is { Allowed: true }) return true;
            if (match is { Allowed: false }) return false;
        }

        return false;
    }

    private bool GlobMatch(string path, string pattern)
    {
        return pattern.EndsWith("*")
            ? path.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)
            : path.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
