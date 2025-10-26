using Authentication.Application.Dtos;
namespace Authentication.Application.Interfaces;

public interface IKeycloakClientAuthorization
{
    Task<bool> IsAuthorizedAsync(string accessToken, string resource, string scope);
}
