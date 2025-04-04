﻿using System.ComponentModel.DataAnnotations;

namespace DMS.Auth.Domain.Entities;

public class User
{
    [Key]
    public Guid Id { get; private set; }
    [Required]
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;   
    public bool IsMfaEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

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
    }

    public void DisableMfa()
    {
        IsMfaEnabled = false;
    }
}
