using Microsoft.EntityFrameworkCore;

using Authentication.Domain.Entities;
using Authentication.Domain.Enums;
using Authentication.Domain.Interfaces;
using Authentication.Infrastructure.Database;

public class EmailWhitelistRepository : IEmailWhitelistRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailWhitelistRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }    

    public async Task<List<EmailWhitelist>> GetAllAsync()
    {
        return await _dbContext.EmailWhitelists.ToListAsync();
    }

    public async Task<EmailWhitelist?> GetByIdAsync(int id)
    {
        return await _dbContext.EmailWhitelists.FindAsync([id]);
    }

    public async Task<bool> IsWhitelistedAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);
        var domain = ExtractDomain(normalizedEmail);

        return await _dbContext.EmailWhitelists
            .AsNoTracking()
            .AnyAsync(x =>
                (x.Type == WhitelistType.Email && x.Value == normalizedEmail) ||
                (x.Type == WhitelistType.Domain && x.Value == domain));
    }

    public async Task AddAsync(EmailWhitelist entry)
    {
        var existing = await _dbContext.EmailWhitelists.AnyAsync(x =>
            x.Type == entry.Type &&
            x.Value == entry.Value);
        
        if (existing == false)
        {
            _dbContext.EmailWhitelists.Add(entry);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(EmailWhitelist entry)
    {      
        _dbContext.EmailWhitelists.Remove(entry);
        await _dbContext.SaveChangesAsync();
    }

    private static string NormalizeEmail(string email)
      => email.Trim().ToLowerInvariant();

    private static string ExtractDomain(string normalizedEmail)
    {
        var at = normalizedEmail.LastIndexOf('@');
        if (at <= 0 || at == normalizedEmail.Length - 1)
            throw new ArgumentException("Invalid email format.", nameof(normalizedEmail));

        return normalizedEmail[(at + 1)..];
    }
}
