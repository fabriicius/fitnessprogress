using FitProgress.Application.Results;
using FitProgress.Domain.Contracts.V1.Users.Requests;

namespace FitProgress.Application.IService.Users;

public interface IUserService
{
    Task<CreateUserResult> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
}
