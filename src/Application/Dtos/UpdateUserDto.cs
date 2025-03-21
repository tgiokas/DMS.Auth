﻿namespace DMS.Auth.Application.Dtos;

public class UpdateUserDto
{
    public string Username { get; set; }
    public string NewUsername { get; set; } // Optional
    public string Email { get; set; }
    public string NewEmail { get; set; } // Optional
    public bool IsEnabled { get; set; }
}