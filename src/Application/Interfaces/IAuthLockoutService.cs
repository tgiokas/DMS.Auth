namespace Authentication.Application.Interfaces;

public interface IAuthLockoutService
{
    Task<bool> IsLockedAsync(string loginKey);
    Task RegisterLoginFailureAsync(string loginKey);
    Task RegisterLoginSuccessAsync(string loginKey);
}