namespace Authentication.Application.Dtos;

public record UserContext(
    string UserId,
    IReadOnlyDictionary<Department, IReadOnlyList<Guid>> DepartmentRoles);
