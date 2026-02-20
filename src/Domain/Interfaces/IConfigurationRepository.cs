using Authentication.Domain.Entities;
using Authentication.Domain.Enums;

namespace Authentication.Domain.Interfaces;

public interface IConfigurationRepository
{
    Task<Configuration?> GetConfigurationAsync();   
    Task<MfaType> GetMfaTypeAsync();
    Task UpdateAsync(Configuration configuration);
}