using FitProgress.Application.Results;
using FitProgress.Application.Services.Users;
using FitProgress.Domain.Abstractions;
using FitProgress.Domain.Contracts.V1.Users.Requests;
using FitProgress.Domain.Models.Users;
using FitProgress.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitProgress.UnitTests.Application.Services.Users;

public class UserServiceTests
{
    [Fact]
    public async Task CreateUserAsync_DadosValidosEEmailInedito_DeveRetornarSuccess()
    {
        var repository = new FakeUserRepository(existsByEmail: false, addResult: true);
        var hasher = new FakePasswordHasher();
        var service = new UserService(repository, hasher, NullLogger<UserService>.Instance);

        var request = new CreateUserRequest("Maria Silva", "maria@example.com", "Senha123");

        var result = await service.CreateUserAsync(request, CancellationToken.None);

        var success = Assert.IsType<CreateUserResult.Success>(result);
        Assert.Equal("Maria Silva", success.User.Name);
        Assert.Equal("maria@example.com", success.User.Email);
        Assert.NotNull(repository.LastAddedUser);
        Assert.Equal("hash-de-Senha123", repository.LastAddedUser!.PasswordHash.Value);
        Assert.NotEqual("Senha123", repository.LastAddedUser.PasswordHash.Value);
    }

    [Fact]
    public async Task CreateUserAsync_NomeEmailESenhaInvalidosSimultaneamente_DeveRetornarTodasAsViolacoes()
    {
        var repository = new FakeUserRepository(existsByEmail: false, addResult: true);
        var hasher = new FakePasswordHasher();
        var service = new UserService(repository, hasher, NullLogger<UserService>.Instance);

        var request = new CreateUserRequest(string.Empty, "nao-e-email", "123");

        var result = await service.CreateUserAsync(request, CancellationToken.None);

        var validationFailed = Assert.IsType<CreateUserResult.ValidationFailed>(result);
        Assert.Equal(3, validationFailed.Errors.Count);
        Assert.Contains(validationFailed.Errors, e => e.Field == "name");
        Assert.Contains(validationFailed.Errors, e => e.Field == "email");
        Assert.Contains(validationFailed.Errors, e => e.Field == "password");
        Assert.False(repository.ExistsByEmailAsyncCalled);
        Assert.False(repository.AddAsyncCalled);
    }

    [Fact]
    public async Task CreateUserAsync_EmailJaExistente_DeveRetornarEmailAlreadyInUseSemChamarAddAsync()
    {
        var repository = new FakeUserRepository(existsByEmail: true, addResult: true);
        var hasher = new FakePasswordHasher();
        var service = new UserService(repository, hasher, NullLogger<UserService>.Instance);

        var request = new CreateUserRequest("Maria Silva", "maria@example.com", "Senha123");

        var result = await service.CreateUserAsync(request, CancellationToken.None);

        Assert.IsType<CreateUserResult.EmailAlreadyInUse>(result);
        Assert.True(repository.ExistsByEmailAsyncCalled);
        Assert.False(repository.AddAsyncCalled);
    }

    [Fact]
    public async Task CreateUserAsync_CorridaDeConcorrenciaNoAddAsync_DeveRetornarEmailAlreadyInUse()
    {
        var repository = new FakeUserRepository(existsByEmail: false, addResult: false);
        var hasher = new FakePasswordHasher();
        var service = new UserService(repository, hasher, NullLogger<UserService>.Instance);

        var request = new CreateUserRequest("Maria Silva", "maria@example.com", "Senha123");

        var result = await service.CreateUserAsync(request, CancellationToken.None);

        Assert.IsType<CreateUserResult.EmailAlreadyInUse>(result);
        Assert.True(repository.ExistsByEmailAsyncCalled);
        Assert.True(repository.AddAsyncCalled);
    }

    private sealed class FakeUserRepository(bool existsByEmail, bool addResult) : IUserRepository
    {
        public User? LastAddedUser { get; private set; }

        public bool ExistsByEmailAsyncCalled { get; private set; }

        public bool AddAsyncCalled { get; private set; }

        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
        {
            ExistsByEmailAsyncCalled = true;
            return Task.FromResult(existsByEmail);
        }

        public Task<bool> AddAsync(User user, CancellationToken cancellationToken)
        {
            AddAsyncCalled = true;
            LastAddedUser = user;
            return Task.FromResult(addResult);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public PasswordHash Hash(Password password) =>
            PasswordHash.FromHashedValue($"hash-de-{password.Value}");
    }
}
