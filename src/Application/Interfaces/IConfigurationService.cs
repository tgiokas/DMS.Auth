using Authentication.Domain.Enums;
using Authentication.Application.Dtos;
using Authentication.Domain.Entities;

namespace Authentication.Application.Interfaces;

public interface IConfigurationService
{
    Task<Result<string>> GetMfaTypeAsync();
    Task<Result<bool>> UpdateMfaTypeAsync(string mfaType);

    Task<Result<List<WhitelistEntryDto>>> GetAllWhitelistEntriesAsync();
    Task<Result<EmailWhitelist>> AddWhitelistEntryAsync(string type, string value);
    Task<Result<List<WhitelistEntryDto>>> AddWhitelistEntriesAsync(List<WhitelistEntryDto> entries);
    Task<Result<bool>> DeleteWhitelistEntryAsync(int id);
    Task<Result<bool>> DeleteWhitelistEntriesAsync(List<WhitelistEntryIdDto> ids);
}