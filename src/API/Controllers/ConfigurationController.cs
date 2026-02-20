using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Interfaces;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IConfigurationService _configurationService;

    public ConfigurationController(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    [HttpPost("mfa/type")]
    public async Task<IActionResult> GetMfaType()
    {
        var result = await _configurationService.GetMfaTypeAsync();
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("mfa/update")]
    public async Task<IActionResult> UpdateMfaType(MfaTypeGlobalUpdateDto request)
    {
        var result = await _configurationService.UpdateMfaTypeAsync(request.MfaType);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("whitelist/getall")]
    public async Task<IActionResult> GetWhitelistEntries()
    {
        var result = await _configurationService.GetAllWhitelistEntriesAsync();
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("whitelist/add")]
    public async Task<IActionResult> AddWhitelistEntry(WhitelistEntryDto request)
    {
        var result = await _configurationService.AddWhitelistEntryAsync(request.Type, request.Value);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("whitelists/add")]
    public async Task<IActionResult> AddWhitelistEntries(List<WhitelistEntryDto> request)
    {
        var result = await _configurationService.AddWhitelistEntriesAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("whitelist/delete")]
    public async Task<IActionResult> DeleteWhitelistEntry(WhitelistEntryIdDto request)
    {
        var result = await _configurationService.DeleteWhitelistEntryAsync(request.Id);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("whitelists/delete")]
    public async Task<IActionResult> DeleteWhitelistEntries(List<WhitelistEntryIdDto> request)
    {
        var result = await _configurationService.DeleteWhitelistEntriesAsync(request);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }
}