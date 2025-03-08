using System.ComponentModel.DataAnnotations;

namespace DMS.Auth.Domain.Entities;

public class User
{
    [Key]
    public Guid Id { get; private set; }

    [Required]
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string AgencyId { get; private set; }
    // identifies which agency (realm) this user belongs to

    // Additional domain properties
    public bool IsMfaEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core requires a parameterless constructor
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

    // Add your new UpdateProfile method
    public void UpdateProfile(string newEmail)
    {
        // Optional: Add domain validation logic 
        // e.g., check if the email is well-formed, not empty, etc.
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
