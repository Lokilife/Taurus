using Taurus.Core.Interfaces;

namespace Taurus.Infrastructure.Persistence.Repositories;

public sealed class UserRepo : BaseRepo, IUserRepo
{
    public UserRepo(TaurusDbContext dbContext) : base(dbContext) { }

    public Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _db.Users.FindAsync(new Guid[] { id }, cancellationToken).AsTask();
    }

    public Task<UserEntity?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername, cancellationToken);;
    }

    public Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<UserEntity?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    public Task AddAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        return _db.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        _db.Users.Update(user);
        return _db.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        _db.Users.Remove(user);
        return _db.SaveChangesAsync(cancellationToken);
    }
}
