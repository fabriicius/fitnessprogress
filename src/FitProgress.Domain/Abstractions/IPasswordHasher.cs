using FitProgress.Domain.ValueObjects;

namespace FitProgress.Domain.Abstractions;

public interface IPasswordHasher
{
    PasswordHash Hash(Password password);
}
