namespace DMS.Auth.Application.Dtos;
public class KeycloakUser
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public bool IsMfaEnabled { get; set; }
    public string AgencyId { get; set; }
}