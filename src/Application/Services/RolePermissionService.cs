using Authentication.Application.Dtos;
using Authentication.Application.Errors;
using Authentication.Application.Interfaces;
using Authentication.Domain.Entities;
using Authentication.Domain.Interfaces;

namespace Authentication.Application.Services;

public class RolePermissionService
{
    private readonly IKeycloakClientRole _keycloakClientRole;
    private readonly IRolePermissionRepo _repository;
    private readonly IErrorCatalog _errors;

    public RolePermissionService(IKeycloakClientRole keycloakClientRole, IRolePermissionRepo repository, IErrorCatalog errors)
    {
        _keycloakClientRole = keycloakClientRole;
        _repository = repository;
        _errors = errors;
    }

    public async Task<Result<List<RolePermissionDto>>> GetAllAsync()
    {
        var rules = await _repository.GetAllAsync();
        if (rules == null)
        {
            return _errors.Fail<List<RolePermissionDto>>(ErrorCodes.AUTH.RulesNotFound);
        }
        var dtos = rules.Select(RolePermissionDto.FromEntity).ToList();
        return Result<List<RolePermissionDto>>.Ok(dtos);
    }

    public async Task<Result<RolePermissionDto>> GetByIdAsync(int id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule == null)
        {
            return _errors.Fail<RolePermissionDto>(ErrorCodes.AUTH.RuleNotFound);
        }
        return Result<RolePermissionDto>.Ok(RolePermissionDto.FromEntity(rule));
    }

    public async Task<Result<List<RolePermissionDto>>> GetByRoleIdAsync(string roleId)
    {
        if (!Guid.TryParse(roleId, out var guidRoleId))
            return _errors.Fail<List<RolePermissionDto>>(ErrorCodes.AUTH.RoleIdNotValid);

        var role = await _keycloakClientRole.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            return _errors.Fail<List<RolePermissionDto>>(ErrorCodes.AUTH.RoleNotFound);
        }

        var rules = await _repository.GetByRoleIdAsync(guidRoleId);
        if (rules == null || rules.Count == 0)
        {
            return _errors.Fail<List<RolePermissionDto>>(ErrorCodes.AUTH.RulesNotFound);
        }
        var dtos = rules.Select(RolePermissionDto.FromEntity).ToList();
        return Result<List<RolePermissionDto>>.Ok(dtos);
    }

    public async Task<Result<List<RolePermissionDto>>> AddAsync(List<RolePermissionCreateDto> ruleDtos)
    {
        var createdDtos = new List<RolePermissionDto>();

        foreach (var ruleDto in ruleDtos)
        {
            if (!Guid.TryParse(ruleDto.RoleId, out var guidRoleId))
                return _errors.Fail<List<RolePermissionDto>>(ErrorCodes.AUTH.RoleIdNotValid);

            var role = await _keycloakClientRole.GetRoleByIdAsync(ruleDto.RoleId);
            if (role == null)
            {
                return _errors.Fail<List<RolePermissionDto>>(ErrorCodes.AUTH.RoleNotFound);
            }

            var rule = new RolePermission
            {
                KeycloakRoleId = guidRoleId,
                HttpMethod = ruleDto.HttpMethod,
                ActionId = ruleDto.ActionId,
                EndPoints = ruleDto.EndPoints,
                Urls = ruleDto.Urls,
                Allowed = true,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(rule);
            createdDtos.Add(RolePermissionDto.FromEntity(rule));
        }

        return Result<List<RolePermissionDto>>.Ok(createdDtos, "Role permissions added successfully.");
    }

    public async Task<Result<RolePermissionDto>> UpdateAsync(RolePermissionUpdateDto ruleDto)
    {
        if (!Guid.TryParse(ruleDto.RoleId, out var roleId))
            return _errors.Fail<RolePermissionDto>(ErrorCodes.AUTH.RoleIdNotValid);

        var role = await _keycloakClientRole.GetRoleByIdAsync(ruleDto.RoleId);
        if (role == null)
        {
            return _errors.Fail<RolePermissionDto>(ErrorCodes.AUTH.RoleNotFound);
        }

        var existing = await _repository.GetByIdAsync(ruleDto.Id);
        if (existing == null)
        {
            return _errors.Fail<RolePermissionDto>(ErrorCodes.AUTH.RuleNotFound);
        }

        existing.KeycloakRoleId = roleId;
        existing.HttpMethod = ruleDto.HttpMethod ?? existing.HttpMethod;
        existing.ActionId = ruleDto.ActionId ?? existing.ActionId;
        existing.EndPoints = ruleDto.EndPoints ?? existing.EndPoints;
        existing.Urls = ruleDto.Urls ?? existing.Urls;
        existing.Allowed = false;
        existing.ModifiedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(existing);

        return Result<RolePermissionDto>.Ok(RolePermissionDto.FromEntity(existing), "Role permissions updated successfully.");
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var rule = await _repository.GetByIdAsync(id);
        if (rule == null)
        {
            return _errors.Fail<bool>(ErrorCodes.AUTH.RuleNotFound);
        }

        await _repository.DeleteAsync(rule);

        return Result<bool>.Ok(true, "Role permissions deleted successfully.");
    }

    public async Task<Result<bool>> DeleteAsync(List<RolePermissionIdDto> ids)
    {
        foreach (var id in ids)
        {
            var rule = await _repository.GetByIdAsync(id.Id);
            if (rule == null)
            {
                return _errors.Fail<bool>(ErrorCodes.AUTH.RuleNotFound);
            }

            await _repository.DeleteAsync(rule);
        }

        return Result<bool>.Ok(true, "Role permissions deleted successfully.");
    }

}
