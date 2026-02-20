using Authentication.Domain.Entities;

namespace Authentication.Application.Dtos;
public class RolePermissionDto
{
    public int Id { get; set; }
    public required string RoleId { get; set; } = string.Empty;
    public string? HttpMethod { get; set; }
    public string? ActionId { get; set; }
    public bool? Allowed { get; set; }
    public List<string>? EndPoints { get; set; }
    public List<string>? Urls { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static RolePermissionDto FromEntity(RolePermission rule)
    {
        return new RolePermissionDto
        {
            Id = rule.Id,
            RoleId = rule.KeycloakRoleId.ToString(),
            HttpMethod = rule.HttpMethod,
            ActionId = rule.ActionId,
            Allowed = rule.Allowed,
            EndPoints = rule.EndPoints,
            Urls = rule.Urls,
            CreatedAt = rule.CreatedAt,
            ModifiedAt = rule.ModifiedAt
        };
    }
}