using Authentication.Domain.Entities;
using Authentication.Domain.Enums;

namespace Authentication.Domain.Interfaces;

public interface IEmailWhitelistRepository
{
    Task<List<EmailWhitelist>> GetAllAsync();   
    Task<EmailWhitelist?> GetByIdAsync(int id);
    Task<bool> IsWhitelistedAsync(string email);
    Task AddAsync(EmailWhitelist email);
    Task DeleteAsync(EmailWhitelist email);    
}