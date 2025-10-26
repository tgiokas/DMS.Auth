namespace Authentication.Application.Dtos;

public class RoleUpdateDto
{ 
    public required string RoleName { get; set; }
    public string? NewRoleName { get; set; }
    public string? Description { get; set; }  
}