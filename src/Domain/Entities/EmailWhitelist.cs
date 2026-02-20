using Authentication.Domain.Enums;

namespace Authentication.Domain.Entities;

public class EmailWhitelist
{
    public int Id { get; set; }
    public WhitelistType Type { get; set; }

    // Stores either:
    // - normalized email: "user@cbs.gr"
    // - normalized domain: "cbs.gr"
    public string Value { get; set; } = string.Empty!;   
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}