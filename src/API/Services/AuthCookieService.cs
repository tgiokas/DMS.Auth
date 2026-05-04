using Authentication.Api.Constants;

namespace Authentication.Api.Services;

public static class AuthCookieService
{
    public const string RefreshTokenCookieName = "refresh_token";
    public const string RememberMeCookieName = "auth_remember";

    public static bool IsRememberMeFromCookie(HttpRequest request)
    {
        if (request.Cookies.TryGetValue(RememberMeCookieName, out var value))
        {
            return value == "1";
        }

        // Legacy sessions: only refresh_token cookie existed with fixed 8h expiry
        return false;
    }

    public static void AppendAuthCookies(HttpResponse response, HttpRequest request, string refreshToken, bool rememberMe)
    {
        var expires = rememberMe
            ? DateTimeOffset.UtcNow.AddDays(CookieConstants.RefreshTokenCookieRememberMeDays)
            : DateTimeOffset.UtcNow.AddHours(CookieConstants.RefreshTokenCookieExpirationHours);

        var baseOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expires
        };

        response.Cookies.Append(RefreshTokenCookieName, refreshToken, baseOptions);

        var rememberOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expires
        };

        response.Cookies.Append(RememberMeCookieName, rememberMe ? "1" : "0", rememberOptions);
    }

    public static void RefreshAuthCookies(HttpResponse response, HttpRequest request, string refreshToken)
    {
        AppendAuthCookies(response, request, refreshToken, IsRememberMeFromCookie(request));
    }

    public static void DeleteAuthCookies(HttpResponse response)
    {
        foreach (var name in new[] { RefreshTokenCookieName, RememberMeCookieName })
        {
            response.Cookies.Delete(name, new CookieOptions { Path = "/" });
        }
    }
}
