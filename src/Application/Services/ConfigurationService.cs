using System.Text.RegularExpressions;

using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;
using Authentication.Application.Errors;
using Authentication.Domain.Enums;
using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEmailWhitelistRepository _emailWhitelistRepository;
    private readonly IErrorCatalog _errors;

    public ConfigurationService(IConfigurationRepository configurationRepository, 
        IEmailWhitelistRepository emailWhitelistRepository,
        IErrorCatalog errors)
    {
        _configurationRepository = configurationRepository;
        _emailWhitelistRepository = emailWhitelistRepository;
        _errors = errors;
    }

    public async Task<Result<string>> GetMfaTypeAsync()
    {
        var mfaType = await _configurationRepository.GetMfaTypeAsync();
        return Result<string>.Ok(mfaType.ToString());
    }

    public async Task<Result<bool>> UpdateMfaTypeAsync(string mfaType)
    {
        if (!Enum.TryParse<MfaType>(mfaType, true, out var parsedMfaType))
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.InvalidMfaType);
        }

        var config = await _configurationRepository.GetConfigurationAsync();
        if (config == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.MfaTypeNotFound);
        }

        config.MfaType = parsedMfaType;
        config.ModifiedAt = DateTime.UtcNow;

        await _configurationRepository.UpdateAsync(config);
        return Result<bool>.Ok(true, "MFA type updated successfully.");
    }

    public async Task<Result<List<WhitelistEntryDto>>> GetAllWhitelistEntriesAsync()
    {
        var entries = await _emailWhitelistRepository.GetAllAsync();
        if (entries == null)
        {
            return _errors.Fail<List<WhitelistEntryDto>>(ErrorCodes.AUTH.WhitelistEntryNotFound);
        }
        
        var whitelistEntries = entries.Select(WhitelistEntryDto.FromEntity).ToList();
        return Result<List<WhitelistEntryDto>>.Ok(whitelistEntries);
    }

    public async Task<Result<EmailWhitelist>> AddWhitelistEntryAsync(string type, string value)
    {
        if (!Enum.TryParse<WhitelistType>(type, true, out var whitelistType))
        {
            return _errors.Fail<EmailWhitelist>(ErrorCodes.AUTH.InvalidWhitelistType);
        }

        if (!IsValidWhitelistValue(whitelistType, value))
        {
            return _errors.Fail<EmailWhitelist>(ErrorCodes.AUTH.InvalidWhitelistValue);
        }

        var normalizedValue = NormalizeValue(whitelistType, value);

        var exists = (await _emailWhitelistRepository.GetAllAsync())
            .Any(x => x.Type == whitelistType && x.Value == normalizedValue);

        if (exists)
        {
            return _errors.Fail<EmailWhitelist>(ErrorCodes.AUTH.WhitelistValueExists);
        }

        var entry = new EmailWhitelist
        {
            Type = whitelistType,
            Value = normalizedValue
        };

        await _emailWhitelistRepository.AddAsync(entry);

        return Result<EmailWhitelist>.Ok(entry, "Whitelist entry added successfully.");
    }

    public async Task<Result<List<WhitelistEntryDto>>> AddWhitelistEntriesAsync(List<WhitelistEntryDto> entries)
    {
        var allExisting = await _emailWhitelistRepository.GetAllAsync();
        var addedEntries = new List<WhitelistEntryDto>();
        var errors = new List<string>();

        foreach (var entry in entries)
        {
            if (!Enum.TryParse<WhitelistType>(entry.Type, true, out var whitelistType))
            {
                errors.Add($"Invalid type: {entry.Type}");
                continue;
            }

            if (!IsValidWhitelistValue(whitelistType, entry.Value))
            {
                errors.Add($"Invalid value for type {entry.Type}: {entry.Value}");
                continue;
            }

            var normalizedValue = NormalizeValue(whitelistType, entry.Value);

            if (allExisting.Any(x => x.Type == whitelistType && x.Value == normalizedValue) ||
                addedEntries.Any(x => x.Type.Equals(entry.Type, StringComparison.OrdinalIgnoreCase) 
                && x.Value.Equals(normalizedValue, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Duplicate entry: {entry.Type} - {entry.Value}");
                continue;
            }

            var newEntry = new EmailWhitelist
            {
                Type = whitelistType,
                Value = normalizedValue
            };

            await _emailWhitelistRepository.AddAsync(newEntry);
            addedEntries.Add(WhitelistEntryDto.FromEntity(newEntry));
        }

        if (addedEntries.Count == 0)
        {
            return _errors.Fail<List<WhitelistEntryDto>>(ErrorCodes.AUTH.InvalidWhitelistValue);
        }

        var message = "Whitelist entries added successfully.";
        if (errors.Count > 0)
            message += $" Some entries were skipped: {string.Join("; ", errors)}";

        return Result<List<WhitelistEntryDto>>.Ok(addedEntries, message);
    }

    public async Task<Result<bool>> DeleteWhitelistEntryAsync(int id)
    {
        var entry = await _emailWhitelistRepository.GetByIdAsync(id);
        if (entry == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.WhitelistEntryNotFound);
        }

        await _emailWhitelistRepository.DeleteAsync(entry);
        
        return Result<bool>.Ok(true, "Whitelist entry deleted successfully.");
    }

    public async Task<Result<bool>> DeleteWhitelistEntriesAsync(List<WhitelistEntryIdDto> ids)
    {
        var errors = new List<string>();
        var deletedCount = 0;

        foreach (var dto in ids)
        {
            var entry = await _emailWhitelistRepository.GetByIdAsync(dto.Id);
            if (entry == null)
            {
                errors.Add($"Whitelist entry with ID {dto.Id} not found.");
                continue;
            }

            await _emailWhitelistRepository.DeleteAsync(entry);
            deletedCount++;
        }

        if (deletedCount == 0)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.WhitelistEntryNotFound);
        }

        var message = $"Deleted {deletedCount} whitelist entr{(deletedCount == 1 ? "y" : "ies")}.";
        if (errors.Count > 0)
            message += $" Some entries were skipped: {string.Join("; ", errors)}";

        return Result<bool>.Ok(true, message);
    }


    private static bool IsValidWhitelistValue(WhitelistType type, string value)
    {
        value = value.Trim();

        switch (type)
        {
            case WhitelistType.Email:               
                return Regex.IsMatch(
                    value,
                    @"^[^@\s]+@[^@\s]+\.[a-zA-Z]{2,}$"
                );
            case WhitelistType.Domain:                
                return !value.Contains('@') &&
                       Regex.IsMatch(
                           value,
                           @"^([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}$"
                       );
            default:
                return false;
        }
    }

    private static string NormalizeValue(WhitelistType type, string value)
    {
        value = value.Trim().ToLowerInvariant();

        return type switch
        {
            WhitelistType.Email => value,
            WhitelistType.Domain => value.TrimStart('@'),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}