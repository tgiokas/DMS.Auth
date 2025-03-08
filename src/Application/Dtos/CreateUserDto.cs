using System;
namespace DMS.Auth.Application.Dtos;

public class CreateUserDto
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public bool IsMfaEnabled { get; set; }
    public string AgencyId { get; set; }

}
