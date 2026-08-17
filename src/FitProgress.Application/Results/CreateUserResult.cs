using FitProgress.Domain.Contracts.V1.Users.Responses;

namespace FitProgress.Application.Results;

public abstract record CreateUserResult
{
    public sealed record Success(UserResponse User) : CreateUserResult;

    public sealed record ValidationFailed(IReadOnlyList<ValidationError> Errors) : CreateUserResult;

    public sealed record EmailAlreadyInUse : CreateUserResult;
}
