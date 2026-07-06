using Taurus.Core.Entities;

namespace Taurus.Core.Interfaces;

public interface ICredentialRepo
{
    Task<CredentialEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CredentialEntity>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(CredentialEntity credential, CancellationToken cancellationToken = default);
    Task UpdateAsync(CredentialEntity credential, CancellationToken cancellationToken = default);
    Task DeleteAsync(CredentialEntity credential, CancellationToken cancellationToken = default);
}
