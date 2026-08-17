using FitProgress.Application.IService.Users;
using FitProgress.Application.Results;
using FitProgress.Domain.Abstractions;
using FitProgress.Domain.Contracts.V1.Users.Requests;
using FitProgress.Domain.Contracts.V1.Users.Responses;
using FitProgress.Domain.Models.Users;
using FitProgress.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FitProgress.Application.Services.Users;

public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ILogger<UserService> logger) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ILogger<UserService> _logger = logger;

    public async Task<CreateUserResult> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        var nameIsValid = PersonName.TryCreate(request.Name, out var name, out var nameError);
        if (!nameIsValid)
        {
            errors.Add(new ValidationError("name", nameError!));
        }

        var emailIsValid = Email.TryCreate(request.Email, out var email, out var emailError);
        if (!emailIsValid)
        {
            errors.Add(new ValidationError("email", emailError!));
        }

        var passwordIsValid = Password.TryCreate(request.Password, out var password, out var passwordError);
        if (!passwordIsValid)
        {
            errors.Add(new ValidationError("password", passwordError!));
        }

        if (errors.Count > 0)
        {
            return new CreateUserResult.ValidationFailed(errors);
        }

        try
        {
            var emailAlreadyExists = await _userRepository.ExistsByEmailAsync(email!, cancellationToken);
            if (emailAlreadyExists)
            {
                return new CreateUserResult.EmailAlreadyInUse();
            }

            var passwordHash = _passwordHasher.Hash(password!);
            var user = User.Create(name!, email!, passwordHash);

            var added = await _userRepository.AddAsync(user, cancellationToken);

            if (!added)
            {
                return new CreateUserResult.EmailAlreadyInUse();
            }

            var response = new UserResponse(
                user.Id,
                user.Name.Value,
                user.Email.Value,
                user.CreatedAt);

            return new CreateUserResult.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar usuário.");

            throw;
        }
    }
}
