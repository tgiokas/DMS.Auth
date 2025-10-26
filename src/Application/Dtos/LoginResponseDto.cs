using System.Text.Json.Serialization;

namespace Authentication.Application.Dtos;

public class LoginResponseDto
{
    public string? AccessToken { get; set; }
    public int? ExpiresIn { get; set; }
    [JsonIgnore]
    public string? RefreshToken { get; set; }
    public bool MfaEnabled { get; set; } = false;
    public string MfaMethod { get; set; } = string.Empty;
    public string? MfaSetUpToken { get; set; }
}