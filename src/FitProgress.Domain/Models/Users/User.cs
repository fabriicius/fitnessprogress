using FitProgress.Domain.Exceptions;
using FitProgress.Domain.ValueObjects;

namespace FitProgress.Domain.Models.Users;

public sealed class User
{
    public Guid Id { get; }

    public PersonName Name { get; }

    public Email Email { get; }

    public PasswordHash PasswordHash { get; }

    public DateTimeOffset CreatedAt { get; }

    private User(
        Guid id,
        PersonName name,
        Email email,
        PasswordHash passwordHash,
        DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public static User Create(PersonName name, Email email, PasswordHash passwordHash)
    {
        if (name is null || email is null || passwordHash is null)
        {
            throw new DomainException(
                "Não é possível criar um usuário com nome, e-mail ou hash de senha nulos.");
        }

        return new User(
            Guid.NewGuid(),
            name,
            email,
            passwordHash,
            DateTimeOffset.UtcNow);
    }
}
