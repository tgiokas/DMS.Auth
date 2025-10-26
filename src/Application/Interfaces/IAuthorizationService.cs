using Authentication.Application.Dtos;

namespace Authentication.Application.Interfaces;

public interface IAuthorizationService
{
    Task<bool> IsAuthorizedAsync(UserContext user, string path, string method);
}
