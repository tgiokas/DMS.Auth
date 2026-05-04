namespace Authentication.Application.Dtos;

public class RolePermissionUpdateDto
{
    public required int Id { get; set; }
    public required string RoleId { get; set; } = string.Empty;
    public string? HttpMethod { get; set; }
    public string? ActionId { get; set; }    
    public List<string>? EndPoints { get; set; }
    public List<string>? Urls { get; set; }
}