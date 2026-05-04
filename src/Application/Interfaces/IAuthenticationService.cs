using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IAuthenticationService
{
    Task<Result<LoginResponseDto>?> LoginUserAsync(string username, string password, bool rememberMe = false);
    Task<Result<RefreshResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<Result<bool>> LogoutAsync(string refreshToken);
    Task<Result<LoginResponseDto>> OAuth2CallbackAsync(string code);    
}
