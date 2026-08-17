namespace FitProgress.Domain.Contracts.V1.Users.Requests;

public sealed record CreateUserRequest(string Name, string Email, string Password);
