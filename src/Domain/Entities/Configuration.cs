using Authentication.Domain.Enums;

namespace Authentication.Domain.Entities;

public class Configuration
{
    public int Id { get; set; }
    public MfaType MfaType { get; set; } = MfaType.None;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}