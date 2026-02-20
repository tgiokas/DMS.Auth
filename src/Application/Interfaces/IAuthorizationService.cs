namespace Authentication.Application.Interfaces;

public interface IAuthorizationService
{
    Task<bool> IsAuthorizedAsync(string role, string path, string method);
}
