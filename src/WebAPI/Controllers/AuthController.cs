using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationService _authenticationService;

    public AuthController(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var tokenResponse = await _authenticationService.AuthenticateUserAsync(request.Username, request.Password);
        if (tokenResponse == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        return Ok(tokenResponse);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var tokenResponse = await _authenticationService.RefreshTokenAsync(request.RefreshToken);
        if (tokenResponse == null)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        return Ok(tokenResponse);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var result = await _authenticationService.LogoutAsync(request.RefreshToken);
        if (!result)
        {
            return BadRequest(new { message = "Failed to logout" });
        }

        return Ok(new { message = "Logout successful" });
    }
}
