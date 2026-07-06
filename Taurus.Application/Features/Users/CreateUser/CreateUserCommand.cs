namespace Taurus.Application.Features.Users.CreateUser;

public record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    string? DisplayName = null,
    string? GivenName = null,
    string? FamilyName = null,
    string? PhoneNumber = null);
