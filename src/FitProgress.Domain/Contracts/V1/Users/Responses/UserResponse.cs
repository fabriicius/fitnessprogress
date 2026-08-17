namespace FitProgress.Domain.Contracts.V1.Users.Responses;

public sealed record UserResponse(Guid Id, string Name, string Email, DateTimeOffset CreatedAt);
