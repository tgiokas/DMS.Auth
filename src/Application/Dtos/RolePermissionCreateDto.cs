namespace Authentication.Application.Dtos;

public class RolePermissionCreateDto
{   
    public required string RoleId { get; set; } = string.Empty;
    public required string HttpMethod { get; set; }
    public required string ActionId { get; set; }    
    public required List<string> EndPoints { get; set; }
    public required List<string> Urls { get; set; }        
}