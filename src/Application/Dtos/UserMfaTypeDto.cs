namespace Authentication.Application.Dtos;

public class UserMfaTypeDto
{
    public required string Username { get; set; }
    public required string MfaType { get; set; }
}
