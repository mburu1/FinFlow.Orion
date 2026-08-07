using System;
using System.Collections.Generic;
using System.Text;

using FinFlow.Orion.Domain.Entities.Identity;

namespace FinFlow.Orion.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(AppUser user, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
}
