using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class BusinessRuleService
{
    private readonly IBusinessRuleRepository _repository;
    private readonly IErrorCatalog _errors;

    public BusinessRuleService(IBusinessRuleRepository repository, IErrorCatalog errors)
    {
        _repository = repository;
        _errors = errors;
    }

    public async Task<Result<List<BusinessRuleDto>>> GetAllAsync()
    {
        var rules = await _repository.GetAllAsync();
        if (rules == null)
        {
            return _errors.Fail<List<BusinessRuleDto>>(ErrorCodes.AUTH.RulesNotFound);
        }
        var dtos = rules.Select(BusinessRuleDto.FromEntity).ToList();
        return Result<List<BusinessRuleDto>>.Ok(dtos);
    }

    public async Task<Result<BusinessRuleDto>> GetByIdAsync(int id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule == null)
        {
            return _errors.Fail<BusinessRuleDto>(ErrorCodes.AUTH.RuleNotFound);
        }
        return Result<BusinessRuleDto>.Ok(BusinessRuleDto.FromEntity(rule));
    }

    public async Task<Result<BusinessRuleDto>> AddAsync(BusinessRuleDto ruleDto)
    {
        if (!Guid.TryParse(ruleDto.RoleId, out var roleId))
            return _errors.Fail<BusinessRuleDto>(ErrorCodes.AUTH.RoleIdNotValid);

        var rule = new BusinessRule
        {
            DepartmentId = ruleDto.DepartmentId,
            KeycloakRoleId = roleId,
            HttpMethod = ruleDto.HttpMethod,
            PathPattern = ruleDto.PathPattern,
            Allowed = ruleDto.Allowed,
            CreatedAt = ruleDto.CreatedAt
        };

        await _repository.AddAsync(rule);

        return Result<BusinessRuleDto>.Ok(BusinessRuleDto.FromEntity(rule), "Business rule added successfully.");
    }

    public async Task<Result<BusinessRuleDto>> UpdateAsync(BusinessRuleDto ruleDto)
    {
        if (!Guid.TryParse(ruleDto.RoleId, out var roleId))
           return _errors.Fail<BusinessRuleDto>(ErrorCodes.AUTH.RoleIdNotValid);

        var existing = await _repository.GetByIdAsync(ruleDto.Id);
        if (existing == null)
        {
            return _errors.Fail<BusinessRuleDto>(ErrorCodes.AUTH.RuleNotFound);
        }

        existing.DepartmentId = ruleDto.DepartmentId;
        existing.KeycloakRoleId = roleId;
        existing.HttpMethod = ruleDto.HttpMethod;
        existing.PathPattern = ruleDto.PathPattern;
        existing.Allowed = ruleDto.Allowed;
        existing.ModifiedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);

        return Result<BusinessRuleDto>.Ok(BusinessRuleDto.FromEntity(existing), "Business rule updated successfully.");
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.RuleNotFound);
        }

        await _repository.DeleteAsync(rule);

        return Result<bool>.Ok(true, "Business rule deleted successfully.");
    }
}
