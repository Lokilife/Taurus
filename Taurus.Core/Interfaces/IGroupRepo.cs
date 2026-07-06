using Taurus.Core.Entities;

namespace Taurus.Core.Interfaces;

public interface IGroupRepo
{
    Task<GroupEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GroupEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddAsync(GroupEntity group, CancellationToken cancellationToken = default);
    Task UpdateAsync(GroupEntity group, CancellationToken cancellationToken = default);
    Task DeleteAsync(GroupEntity group, CancellationToken cancellationToken = default);
}
