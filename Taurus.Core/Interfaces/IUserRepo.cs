using Taurus.Core.Common;
using Taurus.Core.Entities;

namespace Taurus.Core.Interfaces;

public interface IUserRepo
{
    Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetByUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    // Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default); // paginated query probably?
    Task AddAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserEntity user, CancellationToken cancellationToken = default);
}
