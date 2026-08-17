using FitProgress.Domain.Models.Users;
using FitProgress.Domain.ValueObjects;

namespace FitProgress.Domain.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<bool> AddAsync(User user, CancellationToken cancellationToken);
}
