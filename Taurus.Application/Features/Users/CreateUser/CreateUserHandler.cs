using Taurus.Core.Entities;
using Taurus.Core.Interfaces;

namespace Taurus.Application.Features.Users.CreateUser;

public class CreateUserHandler
{
    private readonly IUserRepo _userRepo;

    public CreateUserHandler(IUserRepo userRepository)
    {
        _userRepo = userRepository;
    }

    public async Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        var validator = new CreateUserValidator();
        var validationResult = await validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
            return new CreateUserResult(Guid.Empty, false, string.Join("; ", validationResult.Errors));

        var normalizedUsername = command.Username.ToLowerInvariant();
        if (await _userRepo.GetByUsernameAsync(normalizedUsername, ct) != null)
            return new CreateUserResult(Guid.Empty, false, "Username already exists");

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = command.Username,
            NormalizedUsername = normalizedUsername,
            Email = command.Email,
            DisplayName = command.DisplayName ?? command.Username,
            GivenName = command.GivenName,
            FamilyName = command.FamilyName,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // TODO: add password handling
        // TODO: unit of work
        await _userRepo.AddAsync(user, ct);

        return new CreateUserResult(user.Id, true);
    }
}
