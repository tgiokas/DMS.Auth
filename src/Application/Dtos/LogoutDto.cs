﻿namespace Authentication.Application.Dtos;

public class LogoutDto
{
    public required string RefreshToken { get; set; }
}