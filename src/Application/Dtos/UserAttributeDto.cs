namespace Authentication.Application.Dtos;

public class UserAttributeDto
{
    public required string UserId { get; set; }
    public required string Key { get; set; }
    public string Value { get; set; }=string.Empty;
}
