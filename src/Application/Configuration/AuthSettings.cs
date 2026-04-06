using Microsoft.Extensions.Configuration;

namespace Authentication.Application.Configuration;

/// General authentication service settings bound from environment variables.
/// Covers DB connection, frontend redirects, email whitelist, password reset,
/// email verification, and lockout configuration.
public class AuthSettings
{
    // Database
    public string DbConnection { get; set; } = string.Empty;

    // Frontend
    public string FrontendEntraIdRedirectUri { get; set; } = string.Empty;

    // Email Whitelist ("off" to disable, "domain" or "email" to enable)
    public string EmailsWhitelist { get; set; } = "off";

    // Password Reset
    public string PasswordResetUrl { get; set; } = string.Empty;

    // Email Verification
    public string VerificationUrl { get; set; } = string.Empty;

    // Lockout
    public int MaxLoginFailures { get; set; }
    public int FailureResetTimeMins { get; set; }
    public int LockDurationMins { get; set; }

    public static AuthSettings BindFromConfiguration(IConfiguration configuration)
    {
        return new AuthSettings
        {
            DbConnection = configuration["AUTH_DB_CONNECTION"]
                ?? throw new ArgumentNullException(nameof(configuration), "AUTH_DB_CONNECTION is not set."),

            FrontendEntraIdRedirectUri = configuration["FRONTEND_ENTRAID_REDIRECTURI"]
                ?? throw new ArgumentNullException(nameof(configuration), "FRONTEND_ENTRAID_REDIRECTURI is not set."),

            EmailsWhitelist = configuration["AUTH_EMAILS_WHITELIST"]
                ?? throw new ArgumentNullException(nameof(configuration), "AUTH_EMAILS_WHITELIST is not set."),

            PasswordResetUrl = configuration["PASSWORD_RESET_URL"]
                ?? throw new ArgumentNullException(nameof(configuration), "PASSWORD_RESET_URL is not set."),

            VerificationUrl = configuration["VERIFICATION_URL"]
                ?? throw new ArgumentNullException(nameof(configuration), "VERIFICATION_URL is not set."),

            MaxLoginFailures = int.Parse(
                configuration["AUTH_MAX_LOGIN_FAILURES"]
                ?? throw new ArgumentNullException(nameof(configuration), "AUTH_MAX_LOGIN_FAILURES is not set.")),

            FailureResetTimeMins = int.Parse(
                configuration["AUTH_FAILURE_RESET_TIME_MINS"]
                ?? throw new ArgumentNullException(nameof(configuration), "AUTH_FAILURE_RESET_TIME_MINS is not set.")),

            LockDurationMins = int.Parse(
                configuration["AUTH_LOCK_DURATION_MINS"]
                ?? throw new ArgumentNullException(nameof(configuration), "AUTH_LOCK_DURATION_MINS is not set."))
        };
    }
}