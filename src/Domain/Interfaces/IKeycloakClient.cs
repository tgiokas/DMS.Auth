namespace DMS.Auth.Domain.Interfaces
{
    public interface IKeycloakClient
    {
        // Creates user in Keycloak for a given agency (realm).
        Task<string> CreateUserAsync(string realm, string username, string email);

        // Updates the user’s Keycloak data (e.g., enable MFA).
        Task EnableMfaAsync(string realm, string keycloakUserId);

        Task UpdateUserEmailAsync(string realm, string keycloakUserId, string newEmail);
        
        Task DeleteUserAsync(string realm, string keycloakUserId);
        
        Task AssignRoleAsync(string realm, string keycloakUserId, string roleName);
    }

}
