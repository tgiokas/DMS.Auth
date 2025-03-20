using DMS.Auth.Application.Dtos;

namespace DMS.Auth.Application.Interfaces;

public interface IKeycloakClient
{   
    Task<TokenDto?> GetUserAccessTokenAsync(string username, string password);
    Task<TokenDto?> GetServiceAccessTokenAsync();
    Task<TokenDto> RefreshTokenAsync(string refreshToken);
    Task<string?> AuthenticateUserWithAuthorizationCodeAsync(string authorizationCode);
    Task<TokenTempDto?> GetTempTokenAsync(string username, string password);
    Task<MfaEnrollmentResponse?> GetMfaAuthCode(string tempToken);
    Task<TokenDto?> VerifyMfaAuthCode(MfaVerificationRequest request);

    Task<List<KeycloakUser>> GetUsersAsync();
    Task<string> GetUserIdByUsernameAsync(string username);
    Task<bool> CreateUserAsync(string username, string email, string password);
    Task<bool> UpdateUserAsync(UpdateUserDto request);
    Task<bool> DeleteUserAsync(string username);

    Task<List<KeycloakRole>> GetUserRolesAsync(string username);
    Task<bool> CreateRoleAsync(string roleName, string roleDescr, string realm);
    Task<bool> AssignRoleAsync(string username, string roleId);
    
    Task<bool> EnableMfaAsync(string username);
    Task<bool> LogoutAsync(string refreshToken);

    Task<string> GsisLoginUrl();
    Task<TokenDto?> GsisCallback(string code);
}