using Microsoft.AspNetCore.Mvc;

using Authentication.Application.Services;
using Authentication.Application.Dtos;

namespace Authentication.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BusinessRuleController : ControllerBase
{
    private readonly BusinessRuleService _businessRuleService;

    public BusinessRuleController(BusinessRuleService service)
    {
        _businessRuleService = service;
    }

    [HttpPost("getall")]
    public async Task<IActionResult> GetBusinessRulesAsync()
    {
        var result = await _businessRuleService.GetAllAsync();
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("getbyid")]
    public async Task<IActionResult> GetById(BusinessRuleDto businessRuleDto)
    {
        var result = await _businessRuleService.GetByIdAsync(businessRuleDto.Id);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(BusinessRuleDto businessRuleDto)
    {
        var result = await _businessRuleService.AddAsync(businessRuleDto);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(BusinessRuleDto businessRuleDto)
    {
        var result = await _businessRuleService.UpdateAsync(businessRuleDto);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(BusinessRuleIdDto idDto)
    {
        var result = await _businessRuleService.DeleteAsync(idDto.Id);
        if (!result.Success)
        {
            return Accepted(result);
        }

        return Ok(result);
    }
}
