using DMS.Auth.Domain.Entities;

namespace DMS.Auth.Domain.Interfaces;

// For publishing events to the Audit Microservice
public interface IAuditEventPublisher
{
    Task PublishUserCreatedAsync(User user);
    Task PublishMfaEnabledAsync(User user);
    // ...
}
