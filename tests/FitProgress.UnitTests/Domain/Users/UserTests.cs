using FitProgress.Domain.Exceptions;
using FitProgress.Domain.Models.Users;
using FitProgress.Domain.ValueObjects;

namespace FitProgress.UnitTests.Domain.Users;

public class UserTests
{
    [Fact]
    public void Create_ComValueObjectsValidos_DeveCriarComSucesso()
    {
        PersonName.TryCreate("Maria Silva", out var name, out _);
        Email.TryCreate("maria@example.com", out var email, out _);
        var passwordHash = PasswordHash.FromHashedValue("hash-fake");

        var user = User.Create(name!, email!, passwordHash);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(name, user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.True(user.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_ComNomeNulo_DeveLancarDomainException()
    {
        Email.TryCreate("maria@example.com", out var email, out _);
        var passwordHash = PasswordHash.FromHashedValue("hash-fake");

        Assert.Throws<DomainException>(() => User.Create(null!, email!, passwordHash));
    }

    [Fact]
    public void Create_ComEmailNulo_DeveLancarDomainException()
    {
        PersonName.TryCreate("Maria Silva", out var name, out _);
        var passwordHash = PasswordHash.FromHashedValue("hash-fake");

        Assert.Throws<DomainException>(() => User.Create(name!, null!, passwordHash));
    }

    [Fact]
    public void Create_ComPasswordHashNulo_DeveLancarDomainException()
    {
        PersonName.TryCreate("Maria Silva", out var name, out _);
        Email.TryCreate("maria@example.com", out var email, out _);

        Assert.Throws<DomainException>(() => User.Create(name!, email!, null!));
    }
}
