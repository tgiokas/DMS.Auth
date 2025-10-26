using Authentication.Domain.Entities;

namespace Authentication.Application.Dtos;
public class BusinessRuleDto
{
    public int Id { get; set; }
    public required int DepartmentId { get; set; } 
    public required string RoleId { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string PathPattern { get; set; } = string.Empty;
    public bool Allowed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static BusinessRuleDto FromEntity(BusinessRule rule)
    {
        return new BusinessRuleDto
        {
            Id = rule.Id,
            DepartmentId = rule.DepartmentId,
            RoleId = rule.KeycloakRoleId.ToString(),
            HttpMethod = rule.HttpMethod,
            PathPattern = rule.PathPattern,
            Allowed = rule.Allowed,
            CreatedAt = rule.CreatedAt
        };
    }
}