namespace Authentication.Application.Dtos;

public class RoleProfileDto
{
    public required string Id { get; set; }
    public required string RoleName { get; set; }   
    public string? Description { get; set; }  
}