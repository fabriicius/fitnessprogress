using FitProgress.Domain.Abstractions;
using FitProgress.Domain.Models.Users;
using FitProgress.Domain.ValueObjects;
using IdentityPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<FitProgress.Domain.Models.Users.User>;

namespace FitProgress.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly IdentityPasswordHasher _identityPasswordHasher = new();

    public PasswordHash Hash(Password password)
    {
        var hashedValue = _identityPasswordHasher.HashPassword(user: null!, password.Value);

        return PasswordHash.FromHashedValue(hashedValue);
    }
}
