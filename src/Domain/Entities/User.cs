using System.ComponentModel.DataAnnotations;

namespace DMS.Auth.Domain.Entities;

public class User
{
    [Key]
    public Guid Id { get; private set; }

    [Required]
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string? AgencyId { get; private set; }
    public bool IsMfaEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { }

    public User(string username, string email, string agencyId)
    {
        Id = Guid.NewGuid();
        Username = username;
        Email = email;
        AgencyId = agencyId;
        IsMfaEnabled = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string newEmail)
    {       
        if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains("@"))
        {
            throw new InvalidOperationException("Invalid email address.");
        }

        Email = newEmail;
    }

    public void EnableMfa()
    {
        IsMfaEnabled = true;
        // Possibly add domain events, e.g. MfaEnabledEvent
    }

    public void DisableMfa()
    {
        IsMfaEnabled = false;
    }
}
