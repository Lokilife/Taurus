namespace Taurus.Application.Features.Users.CreateUser;

public record CreateUserResult(
    Guid UserId,
    bool Success,
    string? ErrorMessage = null);
