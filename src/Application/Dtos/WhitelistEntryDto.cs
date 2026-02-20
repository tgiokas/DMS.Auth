using Authentication.Domain.Entities;

namespace Authentication.Application.Dtos;

public class WhitelistEntryDto
{
    public int? Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static WhitelistEntryDto FromEntity(EmailWhitelist entity)
    {
        return new WhitelistEntryDto
        {
            Id = entity.Id,
            Type = entity.Type.ToString(),
            Value = entity.Value,
            CreatedAt = entity.CreatedAt
        };
    }
}