using DMS.Auth.Application.Dtos;
using DMS.Auth.Application.Interfaces;
using DMS.Auth.Domain.Entities;
using DMS.Auth.Domain.Interfaces;

namespace DMS.Auth.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IKeycloakClient _keycloakClient;
    private readonly IAuditEventPublisher _auditPublisher;

    public AuthenticationService(
        IUserRepository userRepository,
        IKeycloakClient keycloakClient,
        IAuditEventPublisher auditPublisher)
    {
        _userRepository = userRepository;
        _keycloakClient = keycloakClient;
        _auditPublisher = auditPublisher;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        // 1. Create user in Keycloak
        var keycloakUserId = await _keycloakClient.CreateUserAsync(
            realm: request.AgencyId,
            username: request.Username,
            email: request.Email);

        // 2. Create and persist the user in our local DB
        var user = new User(request.Username, request.Email, request.AgencyId);
        await _userRepository.AddAsync(user);

        // 3. Publish an Audit event
        await _auditPublisher.PublishUserCreatedAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsMfaEnabled = user.IsMfaEnabled,
            AgencyId = user.AgencyId
        };
    }

    // 1. Update an existing user
    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        // Domain-level update
        user.UpdateProfile(request.NewEmail);
        await _userRepository.UpdateAsync(user);

        // Also update in Keycloak if you keep the same ID or store a separate Keycloak user ID:
        // Example (you might store keycloakUserId in your domain or a separate field):
        var keycloakUserId = user.Id.ToString();
        await _keycloakClient.UpdateUserEmailAsync(user.AgencyId, keycloakUserId, request.NewEmail);

        // Optionally publish an "UserUpdated" audit event
        // e.g. await _auditPublisher.PublishUserUpdatedAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsMfaEnabled = user.IsMfaEnabled,
            AgencyId = user.AgencyId
        };
    }

    // 2. Delete an existing user
    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return; // or throw an exception

        // For a "physical" delete in local DB:
        await _userRepository.DeleteAsync(user);

        // Also remove in Keycloak
        var keycloakUserId = user.Id.ToString();
        await _keycloakClient.DeleteUserAsync(user.AgencyId, keycloakUserId);

        // Optionally publish an event "UserDeleted"
        // e.g. await _auditPublisher.PublishUserDeletedAsync(user);
    }

    // 3. Assign a role
    public async Task AssignRoleAsync(Guid userId, string roleName)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) throw new Exception("User not found");

        // If you store roles locally, you might:
        // 1) Check if role exists in local DB
        // 2) Link user to that role in a bridging table
        // 3) Save changes

        // Also do it in Keycloak:
        var keycloakUserId = user.Id.ToString();
        await _keycloakClient.AssignRoleAsync(user.AgencyId, keycloakUserId, roleName);

        // Possibly publish "UserRoleAssigned" event
    }
    

    public async Task EnableMfaAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        // 1. Enable domain-level MFA
        user.EnableMfa();
        await _userRepository.UpdateAsync(user);

        // 2. Also reflect it in Keycloak
        await _keycloakClient.EnableMfaAsync(
            realm: user.AgencyId,
            // If you store Keycloak ID separately, use that.
            // For demonstration, using user.Id.ToString() is not ideal.
            keycloakUserId: user.Id.ToString());

        // 3. Publish the event
        await _auditPublisher.PublishMfaEnabledAsync(user);
    }
}

