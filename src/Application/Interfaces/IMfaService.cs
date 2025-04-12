using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IMfaService
{    
    TotpSetupDto GenerateTotpCode(string username, string issuer = "Auth");
    Task<bool> RegisterTotpAsync(string username, string code, string setupToken);
    Task<LoginResult> VerifyLoginTotpAsync(string setupToken, string code);    
}
