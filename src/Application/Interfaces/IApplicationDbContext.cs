﻿using DMS.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DMS.Auth.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
