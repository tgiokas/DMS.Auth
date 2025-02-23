namespace DMS.Auth.Application.Dtos;

/// <summary>
/// DTO for Keycloak authentication response.
/// </summary>
public class TokenResponse
{
    public string Access_token { get; set; }
    public string Refresh_token { get; set; }
    public string Token_type { get; set; }
    public int Expires_in { get; set; }
}

