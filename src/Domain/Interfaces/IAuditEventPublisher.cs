using DMS.Auth.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Auth.Domain.Interfaces
{
    // For publishing events to the Audit Microservice
    public interface IAuditEventPublisher
    {
        Task PublishUserCreatedAsync(User user);
        Task PublishMfaEnabledAsync(User user);
        // ...
    }
}
